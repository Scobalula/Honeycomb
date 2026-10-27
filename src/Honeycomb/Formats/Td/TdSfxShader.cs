using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Td;

public sealed class TdSfxShader
{
    public string BlendMode { get; private set; } = "add";

    public string Texture { get; private set; } = string.Empty;

    public Vector4 Tint { get; private set; } = Vector4.One;

    public float EmissiveIntensity { get; private set; } = 1.0f;

    public static TdSfxShader Read(FioPropertySection section)
    {
        TdSfxShader shader = new();

        shader.BlendMode			= section.GetString("blendMode", shader.BlendMode);
        shader.Texture				= section.GetString("tex", shader.Texture);
        shader.Tint					= section.GetColor("tint", shader.Tint);
        shader.EmissiveIntensity	= section.GetFloat("emissive.intensity", shader.EmissiveIntensity);
        return shader;
    }
}
