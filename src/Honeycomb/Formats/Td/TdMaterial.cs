using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Td;

public sealed class TdMaterial
{
    public static TdMaterial Default { get; } = Read(new FioPropertySection());

    public string Preset { get; private set; } = "default";

    public TdGltShader? Glt { get; private set; }

    public TdSfxShader? Sfx { get; private set; }

    public bool Distort { get; private set; }

    public static TdMaterial Read(FioPropertySection section)
    {
        TdMaterial			material	= new() { Preset = section.GetString("preset", "default") };
        FioPropertySection?	glt			= section.GetSection("shaders.glt");
        FioPropertySection?	sfx			= section.GetSection("shaders.sfx");

        material.Distort = material.Preset == "glass" || section.GetSection("shaders.distort") is not null;

        if (glt is null && (sfx is not null || material.Preset is "sfx" or "portal"))
        {
            material.Sfx = TdSfxShader.Read(sfx ?? new FioPropertySection());
        }
        else
        {
            material.Glt = TdGltShader.Read(glt ?? new FioPropertySection(), material.Preset);
        }

        return material;
    }
}
