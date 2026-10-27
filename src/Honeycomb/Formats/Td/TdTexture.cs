using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Td;

public sealed class TdTexture
{
    public string Usage { get; private set; } = string.Empty;

    public Dictionary<string, TdMaterial> Materials { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static TdTexture Parse(string text)
    {
        FioPropertySection	section	= FioPropertySection.Parse(text);
        TdTexture			texture	= new() { Usage = section.GetString("usage", string.Empty) };

        if (section.GetSection("materials") is FioPropertySection materials)
        {
            foreach ((string name, object value) in materials.Values)
            {
                if (value is FioPropertySection material)
                {
                    texture.Materials[name] = TdMaterial.Read(material);
                }
            }
        }

        return texture;
    }

    public TdMaterial GetMaterial(string name)
    {
        return Materials.GetValueOrDefault(name) ?? Materials.Values.FirstOrDefault() ?? TdMaterial.Default;
    }
}
