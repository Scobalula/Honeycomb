using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Honeycomb.Formats.Fio;
using Honeycomb.Formats.Td;
using RedFox.GameExtraction;
using RedFox.Graphics3D;
using RedFox.Graphics3D.Groups;
using RedFox.Imaging.Processing;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Models;

public sealed class TplMaterialBuilder
{
    private const string	DiffuseSlot			= "DiffuseMap";
    private const string	NormalSlot			= "NormalMap";
    private const string	SpecularSlot		= "SpecularMap";
    private const string	EmissionSlot		= "EmissionMap";
    private const string	DetailNormalSlot	= "DetailNormalMap";
    private const string	TexturesDirectory	= "[textures]";
    private const float		AlphaTestReference	= 0.5f;
    private const float		DefaultRoughness	= 0.5f;
    private const float		HairSpecularScale	= 0.1f;

    private readonly MeshGroup						_group;
    private readonly AssetReadContext				_context;
    private readonly VirtualFileSystem				_fileSystem;
    private readonly Dictionary<string, Material>	_materials		= [];
    private readonly Dictionary<string, TdTexture?>	_descriptions	= [];

    public TplMaterialBuilder(MeshGroup group, AssetReadContext context)
    {
        _group		= group;
        _context	= context;
        _fileSystem	= context.GetRequiredService<AssetFileSystemService>().FileSystem;
    }

    public async Task<Material> GetMaterialAsync(FioPropertySection? section, CancellationToken cancellationToken)
    {
        FioPropertySection?	layer			= section?.GetSection("layer0");
        string				textureName		= layer?.GetString("texName") ?? string.Empty;
        string				mayaName		= section?.GetString("mayaMtl") ?? string.Empty;
        string				fallbackName	= string.IsNullOrEmpty(textureName) ? "default" : textureName;
        string				materialName	= string.IsNullOrEmpty(mayaName) ? fallbackName : mayaName;

        if (_materials.TryGetValue(materialName, out Material? material))
        {
            return material;
        }

        material					= _group.AddNode(new Material(materialName));
        _materials[materialName]	= material;

        if (layer is null || string.IsNullOrEmpty(textureName))
        {
            return material;
        }

        TdMaterial	description	= GetDescription(textureName)?.GetMaterial(layer.GetString("mtlName", string.Empty)) ?? TdMaterial.Default;
        Vector2		tiling		= new(layer.GetFloat("tilingU", 1.0f), layer.GetFloat("tilingV", 1.0f));

        if (description.Sfx is TdSfxShader sfx)
        {
            await BuildEffectAsync(material, sfx, string.IsNullOrEmpty(sfx.Texture) ? textureName : sfx.Texture, tiling, cancellationToken);
        }
        else
        {
            await BuildSurfaceAsync(material, description, description.Glt!, textureName, tiling, cancellationToken);
        }

        return material;
    }

    private async Task BuildEffectAsync(Material material, TdSfxShader sfx, string textureName, Vector2 tiling, CancellationToken cancellationToken)
    {
        material.Type			= sfx.BlendMode == "add" ? PbrMaterial.AdditiveTypeName : PbrMaterial.UnlitTransparentTypeName;
        material.DiffuseColor	= new Vector4(SrgbToLinear(sfx.Tint.AsVector3()) * GetEffectIntensity(sfx.EmissiveIntensity), sfx.Tint.W);

        material.SetAttribute(PbrMaterial.TextureTiling, tiling);

        if (await TryConnectTextureAsync(material, DiffuseSlot, textureName, cancellationToken))
        {
            material.DiffuseMapName = DiffuseSlot;
        }
    }

    private async Task BuildSurfaceAsync(Material material, TdMaterial description, TdGltShader glt, string textureName, Vector2 tiling, CancellationToken cancellationToken)
    {
        string	diffuseName		= string.IsNullOrEmpty(glt.DiffuseTexture) ? textureName : glt.DiffuseTexture;
        string	normalName		= string.IsNullOrEmpty(glt.NormalTexture) ? $"{textureName}_nm" : glt.NormalTexture;
        string	specularName	= string.IsNullOrEmpty(glt.SpecularTexture) ? $"{textureName}_spec" : glt.SpecularTexture;
        string	emissionName	= string.IsNullOrEmpty(glt.EmissiveTexture) ? $"{textureName}_em" : glt.EmissiveTexture;
        string	diffuseUsage	= GetDescription(diffuseName)?.Usage ?? string.Empty;
        bool	blended			= glt.BlendMode != "none";
        bool	alphaTested		= diffuseUsage.Contains("MAK");
        int		alphaMode		= GetAlphaMode(diffuseUsage, blended);
        int		specularMode	= GetSpecularMode(GetDescription(specularName)?.Usage);
        int		emissionMode	= GetEmissionMode(GetDescription(emissionName)?.Usage);

        bool	glass			= glt.Combiner == "glass" || description.Distort;
        string	type			= GetSurfaceType(glt, glass, blended);

        material.Type			= type;
        material.DiffuseColor	= glass ? new Vector4(SrgbToLinear(glt.AlbedoTint.AsVector3() * glt.GlassFilter.AsVector3()), Math.Clamp(glt.GlassDensity * 0.5f, 0.05f, 1.0f)) : new Vector4(SrgbToLinear(glt.AlbedoTint.AsVector3()), 1.0f);
        material.EmissiveColor	= new Vector4(glt.EmissiveTint.AsVector3() * GetEmissiveIntensity(glt.EmissiveIntensity), 1.0f);
        material.AlphaCutoff	= alphaTested && !blended ? AlphaTestReference : null;
        material.Metallic		= Math.Clamp(glt.MetalnessBias, 0.0f, 1.0f);
        material.Roughness		= Math.Clamp(DefaultRoughness * glt.RoughnessScale + glt.RoughnessBias, 0.0f, 1.0f);

        material.SetAttribute(PbrMaterial.TextureTiling, tiling);
        material.SetAttribute(PbrMaterial.NormalStrength, glt.NormalScale);
        material.SetAttribute(PbrMaterial.DetailNormalTiling, new Vector2(glt.DetailDensity));
        material.SetAttribute(PbrMaterial.DetailNormalStrength, glt.DetailScale);
        material.SetAttribute(PbrMaterial.Reflectance, glt.Reflectance);
        material.SetAttribute(PbrMaterial.SubsurfaceStrength, type == PbrMaterial.TypeName && glt.ScatteringProfile != "disabled" ? 1.0f : 0.0f);
        material.SetAttribute(PbrMaterial.HairShift, glt.AnisotropicShift);
        material.SetAttribute(PbrMaterial.HairSpecularStrength, glt.AnisotropicIntensity * HairSpecularScale);

        if (await TryConnectTextureAsync(material, DiffuseSlot, diffuseName, cancellationToken))
        {
            material.DiffuseMapName = DiffuseSlot;

            if (alphaMode != 1)
            {
                material.SetChannelSource(PbrMaterial.DiffuseMap, new MaterialChannelSource(DiffuseSlot, ColorChannel.Red, ColorChannel.Green, ColorChannel.Blue, ColorChannel.One));
            }
        }

        if (await TryConnectTextureAsync(material, NormalSlot, normalName, cancellationToken))
        {
            material.NormalMapName = NormalSlot;
        }

        if (await TryConnectTextureAsync(material, SpecularSlot, specularName, cancellationToken))
        {
            ConnectSpecularChannels(material, glt, specularMode);
        }
        else if (alphaMode == 2)
        {
            material.SetChannelSource(PbrMaterial.RoughnessMap, new MaterialChannelSource(DiffuseSlot, ColorChannel.Alpha) { Scale = new Vector4(glt.RoughnessScale), Bias = new Vector4(glt.RoughnessBias) });
        }
        else if (alphaMode == 3)
        {
            material.SetChannelSource(PbrMaterial.RoughnessMap, new MaterialChannelSource(DiffuseSlot, ColorChannel.Alpha) { Scale = new Vector4(-glt.RoughnessScale), Bias = new Vector4(glt.RoughnessScale + glt.RoughnessBias) });
        }

        if (await TryConnectTextureAsync(material, EmissionSlot, emissionName, cancellationToken))
        {
            if (emissionMode == 2)
            {
                material.AmbientOcclusionMapName = EmissionSlot;
                material.SetChannelSource(PbrMaterial.OcclusionMap, new MaterialChannelSource(EmissionSlot, ColorChannel.Red));
            }
            else
            {
                material.EmissiveMapName = EmissionSlot;
            }

            if (emissionMode == 3)
            {
                material.SetChannelSource(PbrMaterial.OcclusionMap, new MaterialChannelSource(EmissionSlot, ColorChannel.Alpha));
            }
        }

        if (!string.IsNullOrEmpty(glt.DetailTexture))
        {
            await TryConnectTextureAsync(material, DetailNormalSlot, glt.DetailTexture, cancellationToken);
        }
    }

    private async Task<bool> TryConnectTextureAsync(Material material, string slot, string textureName, CancellationToken cancellationToken)
    {
        if (!TryGetTextureFile(textureName, out VirtualFile? file) || file.Tag is not Asset textureAsset)
        {
            return false;
        }

        AssetReadResult	result	= await _context.ReadAsync(textureAsset, cancellationToken);
        Texture			texture	= material.AddNode(result.GetData<Texture>());

        material.Connect(slot, texture);
        return true;
    }

    private bool TryGetTextureFile(string textureName, [NotNullWhen(true)] out VirtualFile? file)
    {
        if (_fileSystem.TryGetFile($"pct/{textureName}.pct.resource", out file))
        {
            return true;
        }

        if (_fileSystem.TryGetDirectory(TexturesDirectory, out VirtualDirectory? textures))
        {
            foreach (VirtualDirectory directory in textures.EnumerateDirectories("*", SearchOption.AllDirectories).Prepend(textures))
            {
                if (directory.TryGetFile($"{textureName}.pct_header", out file))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ConnectSpecularChannels(Material material, TdGltShader glt, int specularMode)
    {
        material.SetChannelSource(PbrMaterial.RoughnessMap, new MaterialChannelSource(SpecularSlot, ColorChannel.Green) { Scale = new Vector4(glt.RoughnessScale), Bias = new Vector4(glt.RoughnessBias) });

        if (specularMode == 3)
        {
            material.SetChannelSource(PbrMaterial.TranslucencyMap, new MaterialChannelSource(SpecularSlot, ColorChannel.Red));
            material.SetAttribute(PbrMaterial.TranslucencyStrength, glt.TranslucencyIntensity);
        }
        else
        {
            material.SetChannelSource(PbrMaterial.MetallicMap, new MaterialChannelSource(SpecularSlot, ColorChannel.Red) { Scale = new Vector4(glt.MetalnessScale), Bias = new Vector4(glt.MetalnessBias) });
        }

        if (specularMode >= 2)
        {
            material.SetChannelSource(PbrMaterial.OcclusionMap, new MaterialChannelSource(SpecularSlot, ColorChannel.Blue));
        }
    }

    private TdTexture? GetDescription(string textureName)
    {
        if (_descriptions.TryGetValue(textureName, out TdTexture? description))
        {
            return description;
        }

        description = null;

        if (_fileSystem.TryGetFile($"td/{textureName}.td.resource", out VirtualFile? resourceFile) && resourceFile.Parent is VirtualDirectory directory)
        {
            TdResource resource = TdResource.Parse(Encoding.UTF8.GetString(resourceFile.ReadAllBytes()));

            if (directory.TryGetFile(resource.TdFile, out VirtualFile? file))
            {
                description = TdTexture.Parse(Encoding.UTF8.GetString(file.ReadAllBytes()));
            }
        }

        _descriptions[textureName] = description;
        return description;
    }

    private static string GetSurfaceType(TdGltShader glt, bool glass, bool blended)
    {
        if (glass)
        {
            return PbrMaterial.TransparentTypeName;
        }

        if (glt.Anisotropic)
        {
            return blended ? PbrMaterial.HairTransparentTypeName : PbrMaterial.HairTypeName;
        }

        return blended ? PbrMaterial.TransparentTypeName : PbrMaterial.TypeName;
    }

    private static int GetAlphaMode(string usage, bool blended)
    {
        if (usage.Contains("MAK"))
        {
            return 1;
        }

        if (usage.Contains("MRGH"))
        {
            return 2;
        }

        if (usage.Contains("MSP"))
        {
            return 3;
        }

        return blended ? 1 : 0;
    }

    private static int GetSpecularMode(string? usage)
    {
        return usage switch
        {
            "MSCRGHAO" or "MMRAOT"	=> 2,
            "MTRAO"					=> 3,
            _						=> 1,
        };
    }

    private static int GetEmissionMode(string? usage)
    {
        return usage switch
        {
            "MAO"		=> 2,
            "MEM+MAO"	=> 3,
            _			=> 1,
        };
    }

    private static float GetEmissiveIntensity(float intensity)
    {
        return MathF.Sqrt(Math.Max(intensity, 0.0f)) * 0.25f;
    }

    private static float GetEffectIntensity(float intensity)
    {
        return Math.Max(1.0f, MathF.Sqrt(Math.Max(intensity, 0.0f)) * 0.5f);
    }

    private static Vector3 SrgbToLinear(Vector3 color)
    {
        return new Vector3(SrgbToLinear(color.X), SrgbToLinear(color.Y), SrgbToLinear(color.Z));
    }

    private static float SrgbToLinear(float value)
    {
        return value <= 0.04045f ? value / 12.92f : MathF.Pow((value + 0.055f) / 1.055f, 2.4f);
    }
}
