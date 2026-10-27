using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Pct;

public sealed class PctResource
{
    private const ushort DataChunk			= 0x01;
    private const ushort FormatChunk		= 0xF2;
    private const ushort MipCountChunk		= 0xF9;
    private const ushort DimensionsChunk	= 0x102;
    private const ushort MipLevelsChunk		= 0x107;

    public PctFormat Format { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public int Depth { get; private set; }

    public int FaceCount { get; private set; }

    public int FaceSize { get; private set; }

    public int ImageOffset { get; private set; }

    public string PctFile { get; private set; } = string.Empty;

    public List<PctMipLevel> MipLevels { get; } = [];

    public List<string> MipFiles { get; } = [];

    public static PctResource Parse(string text)
    {
        PctResource	resource	= new();
        string		section		= string.Empty;

        foreach (string rawLine in text.Split('\n'))
        {
            string	line		= rawLine.TrimEnd('\r');
            string	content		= line.TrimStart();
            int		indent		= line.Length - content.Length;
            bool	isListItem	= content.StartsWith("- ", StringComparison.Ordinal);

            if (isListItem)
            {
                content = content[2..];
            }

            if (indent == 0 && isListItem)
            {
                if (section == "mipMaps")
                {
                    resource.MipFiles.Add(content);
                }

                continue;
            }

            int colon = content.IndexOf(':');

            if (colon < 0)
            {
                continue;
            }

            string key		= content[..colon];
            string value	= content[(colon + 1)..].Trim();

            if (indent == 0)
            {
                section = key;
                resource.ReadField(key, value);
            }
            else if (section == "header")
            {
                resource.ReadHeaderField(key, value, indent);
            }
        }

        return resource;
    }

    public static PctResource Read(byte[] header, string pctFile)
    {
        PctResource	resource	= new() { PctFile = pctFile };
        FioReader	reader		= new(header, 0);
        FioChunk	chunk		= reader.ReadChunk();
        int			mipCount	= 0;

        reader.Position = chunk.End;

        while ((chunk = reader.ReadChunk()).Id != DataChunk)
        {
            switch (chunk.Id)
            {
                case DimensionsChunk:
                    resource.Width		= reader.Read<int>();
                    resource.Height		= reader.Read<int>();
                    resource.Depth		= reader.Read<int>();
                    resource.FaceCount	= reader.Read<int>();
                    break;
                case FormatChunk:
                    resource.Format = (PctFormat)reader.Read<int>();
                    break;
                case MipCountChunk:
                    mipCount = reader.Read<int>();
                    break;
                case MipLevelsChunk:
                    for (int i = 0; i < mipCount; i++)
                    {
                        resource.MipLevels.Add(new PctMipLevel(reader.Read<int>(), reader.Read<int>()));
                    }
                    break;
            }

            reader.Position = chunk.End;
        }

        resource.ImageOffset	= chunk.End;
        resource.FaceSize		= resource.MipLevels[^1].Offset + resource.MipLevels[^1].Size;
        return resource;
    }

    private void ReadField(string key, string value)
    {
        switch (key)
        {
            case "imageOffset":
                ImageOffset = int.Parse(value);
                break;
            case "pct":
                PctFile = value.Trim('\'');
                break;
        }
    }

    private void ReadHeaderField(string key, string value, int indent)
    {
        switch (key)
        {
            case "format":
                Format = (PctFormat)int.Parse(value);
                break;
            case "sx":
                Width = int.Parse(value);
                break;
            case "sy":
                Height = int.Parse(value);
                break;
            case "sz":
                Depth = int.Parse(value);
                break;
            case "nFaces":
                FaceCount = int.Parse(value);
                break;
            case "faceSize":
                FaceSize = int.Parse(value);
                break;
            case "offset":
                MipLevels.Add(new PctMipLevel(int.Parse(value), 0));
                break;
            case "size" when indent > 2:
                MipLevels[^1] = MipLevels[^1] with { Size = int.Parse(value) };
                break;
        }
    }
}
