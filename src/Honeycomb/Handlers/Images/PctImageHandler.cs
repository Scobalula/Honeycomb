using System.Text;
using Honeycomb.Formats.Pct;
using RedFox.GameExtraction;
using RedFox.GameExtraction.AssetHandlers;
using RedFox.Graphics3D;
using RedFox.Imaging;
using RedFox.Imaging.IO;
using RedFox.Imaging.Primitives;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Images;

public sealed class PctImageHandler : TextureHandler
{
    private const string ResourceSuffix	= ".pct.resource";
    private const string HeaderSuffix	= ".pct_header";

    public override bool CanHandle(Asset asset, GameExtractionConfiguration configuration)
    {
        return asset.Name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase) || asset.Name.EndsWith(HeaderSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public override Task<AssetReadResult> ReadAsync(Asset asset, AssetReadContext context, CancellationToken cancellationToken)
    {
        AssetReadResult result = new()
        {
            Asset	= asset,
            Data	= new Texture(GetTextureName(asset.Name)) { UserData = asset, ImageLoader = this },
            Handler	= this,
        };

        return Task.FromResult(result);
    }

    public override Image Load(Texture texture, ImageTranslatorManager translatorManager, MipType mipType)
    {
        Asset				asset			= texture.UserData as Asset ?? throw new InvalidOperationException($"Texture '{texture.Name}' is not backed by an asset.");
        VirtualFile			resourceFile	= asset.DataSource as VirtualFile ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not backed by a file.");
        VirtualDirectory	directory		= resourceFile.Parent ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not mounted in a file system.");
        PctResource			resource		= ReadResource(resourceFile);
        ImageFormat			format			= GetImageFormat(resource.Format);
        int					firstMip		= GetFirstMip(resource, mipType);
        byte[]				data			= new byte[resource.FaceCount * resource.MipLevels.Skip(firstMip).Sum(mipLevel => mipLevel.Size)];
        int					offset			= 0;

        if (resource.MipFiles.Count > 0)
        {
            for (int mip = firstMip; mip < resource.MipLevels.Count; mip++)
            {
                using Stream stream = GetFile(directory, resource.MipFiles[mip]).Open();
                stream.ReadExactly(data, offset, resource.MipLevels[mip].Size);
                offset += resource.MipLevels[mip].Size;
            }
        }
        else
        {
            byte[] pct = GetFile(directory, resource.PctFile).ReadAllBytes();

            for (int face = 0; face < resource.FaceCount; face++)
            {
                foreach (PctMipLevel mipLevel in resource.MipLevels.Skip(firstMip))
                {
                    Array.Copy(pct, resource.ImageOffset + face * resource.FaceSize + mipLevel.Offset, data, offset, mipLevel.Size);
                    offset += mipLevel.Size;
                }
            }
        }

        return new Image(Math.Max(1, resource.Width >> firstMip), Math.Max(1, resource.Height >> firstMip), Math.Max(1, resource.Depth >> firstMip), resource.FaceCount, resource.MipLevels.Count - firstMip, format, false, data);
    }

    private static string GetTextureName(string name)
    {
        if (name.EndsWith(HeaderSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return name[..^HeaderSuffix.Length];
        }

        return name[..^ResourceSuffix.Length];
    }

    private static PctResource ReadResource(VirtualFile resourceFile)
    {
        if (resourceFile.Name.EndsWith(HeaderSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return PctResource.Read(resourceFile.ReadAllBytes(), resourceFile.Name[..^HeaderSuffix.Length] + ".pct");
        }

        return PctResource.Parse(Encoding.UTF8.GetString(resourceFile.ReadAllBytes()));
    }

    private static int GetFirstMip(PctResource resource, MipType mipType)
    {
        int maximumDimension = mipType switch
        {
            MipType.UltraLow	=> 256,
            MipType.Low			=> 512,
            MipType.Medium		=> 1024,
            _					=> int.MaxValue,
        };

        int largestDimension	= Math.Max(resource.Width, resource.Height);
        int firstMip			= 0;

        while (firstMip < resource.MipLevels.Count - 1 && largestDimension >> firstMip > maximumDimension)
        {
            firstMip++;
        }

        return firstMip;
    }

    private static ImageFormat GetImageFormat(PctFormat format) => format switch
    {
        PctFormat.Argb8888			=> ImageFormat.B8G8R8A8Unorm,
        PctFormat.A8				=> ImageFormat.A8Unorm,
        PctFormat.Ai88				=> ImageFormat.R8G8Unorm,
        PctFormat.Dxt1Opaque		=> ImageFormat.BC1Unorm,
        PctFormat.Dxt1Transparent	=> ImageFormat.BC1Unorm,
        PctFormat.Dxt5				=> ImageFormat.BC3Unorm,
        PctFormat.Xrgb8888			=> ImageFormat.B8G8R8X8Unorm,
        PctFormat.Dxn				=> ImageFormat.BC5Unorm,
        PctFormat.Dxt5A				=> ImageFormat.BC4Unorm,
        PctFormat.Rgba16161616F		=> ImageFormat.R16G16B16A16Float,
        PctFormat.Argb16161616S		=> ImageFormat.R16G16B16A16Snorm,
        PctFormat.Argb16161616U		=> ImageFormat.R16G16B16A16Unorm,
        PctFormat.R32F				=> ImageFormat.R32Float,
        PctFormat.R8U				=> ImageFormat.R8Unorm,
        PctFormat.Bc6U				=> ImageFormat.BC6HUF16,
        PctFormat.Bc6S				=> ImageFormat.BC6HSF16,
        PctFormat.Bc7				=> ImageFormat.BC7Unorm,
        PctFormat.Bc7A				=> ImageFormat.BC7Unorm,
        PctFormat.R16G16			=> ImageFormat.R16G16Unorm,
        PctFormat.R16				=> ImageFormat.R16Unorm,
        _							=> throw new NotSupportedException($"PCT format {format} is not supported."),
    };

    private static VirtualFile GetFile(VirtualDirectory directory, string name)
    {
        if (!directory.TryGetFile(name, out VirtualFile? file))
        {
            throw new FileNotFoundException($"'{directory.FullPath}/{name}' was not found, load the pak that contains it.", name);
        }

        return file;
    }
}
