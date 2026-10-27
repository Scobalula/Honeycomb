using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Td;

public sealed class TdGltShader
{
    public string BlendMode { get; private set; } = "none";

    public string Combiner { get; private set; } = "default";

    public float MetalnessScale { get; private set; } = 1.0f;

    public float MetalnessBias { get; private set; }

    public float RoughnessScale { get; private set; } = 1.0f;

    public float RoughnessBias { get; private set; }

    public float Reflectance { get; private set; } = 0.5f;

    public float NormalScale { get; private set; } = 1.0f;

    public Vector4 AlbedoTint { get; private set; } = Vector4.One;

    public Vector4 EmissiveTint { get; private set; } = Vector4.One;

    public float EmissiveIntensity { get; private set; }

    public Vector4 GlassFilter { get; private set; } = Vector4.One;

    public float GlassDensity { get; private set; } = 1.0f;

    public string ScatteringProfile { get; private set; } = "disabled";

    public string TranslucencyMode { get; private set; } = "none";

    public float TranslucencyIntensity { get; private set; } = 0.5f;

    public bool Anisotropic { get; private set; }

    public float Anisotropy { get; private set; } = 0.5f;

    public float AnisotropicIntensity { get; private set; } = 1.0f;

    public float AnisotropicShift { get; private set; } = 0.2f;

    public string DetailTexture { get; private set; } = string.Empty;

    public float DetailDensity { get; private set; } = 1.0f;

    public float DetailScale { get; private set; } = 1.0f;

    public string DiffuseTexture { get; private set; } = string.Empty;

    public string NormalTexture { get; private set; } = string.Empty;

    public string SpecularTexture { get; private set; } = string.Empty;

    public string EmissiveTexture { get; private set; } = string.Empty;

    public static TdGltShader Read(FioPropertySection section, string materialPreset)
    {
        TdGltShader shader = new();

        switch (section.GetString("preset", materialPreset))
        {
            case "metal":
                shader.MetalnessScale	= 0.0f;
                shader.MetalnessBias	= 0.25f;
                shader.RoughnessScale	= 0.0f;
                shader.RoughnessBias	= 0.5f;
                break;
            case "stone":
                shader.MetalnessScale	= 0.0f;
                break;
        }

        shader.BlendMode				= section.GetString("blendMode", shader.BlendMode);
        shader.Combiner					= section.GetString("combiner", shader.Combiner);
        shader.MetalnessScale			= section.GetFloat("metalness.scale", shader.MetalnessScale);
        shader.MetalnessBias			= section.GetFloat("metalness.bias", shader.MetalnessBias);
        shader.RoughnessScale			= section.GetFloat("roughness.scale", shader.RoughnessScale);
        shader.RoughnessBias			= section.GetFloat("roughness.bias", shader.RoughnessBias);
        shader.Reflectance				= section.GetFloat("reflectance", shader.Reflectance);
        shader.NormalScale				= section.GetFloat("nmScale", shader.NormalScale);
        shader.AlbedoTint				= section.GetColor("albedo_tint", shader.AlbedoTint);
        shader.EmissiveTint				= section.GetColor("emissive.tint", shader.EmissiveTint);
        shader.EmissiveIntensity		= section.GetFloat("emissive.intensity", shader.EmissiveIntensity);
        shader.GlassFilter				= section.GetColor("glass.filter", shader.GlassFilter);
        shader.GlassDensity				= section.GetFloat("glass.density", shader.GlassDensity);
        shader.ScatteringProfile		= section.GetString("subsurface.scattering.profile", shader.ScatteringProfile);
        shader.TranslucencyMode			= section.GetString("subsurface.translucency.mode", shader.TranslucencyMode);
        shader.TranslucencyIntensity	= section.GetFloat("subsurface.translucency.intensity", shader.TranslucencyIntensity);
        shader.Anisotropic				= section.GetBool("anisotropic_spec.enabled", shader.Anisotropic);
        shader.Anisotropy				= section.GetFloat("anisotropic_spec.anisotropy", shader.Anisotropy);
        shader.AnisotropicIntensity		= section.GetFloat("anisotropic_spec.intensity", shader.AnisotropicIntensity);
        shader.AnisotropicShift			= section.GetFloat("anisotropic_spec.extra.shift", shader.AnisotropicShift);
        shader.DetailTexture			= section.GetString("tex.det", shader.DetailTexture);
        shader.DetailDensity			= section.GetFloat("detail.density", shader.DetailDensity);
        shader.DetailScale				= section.GetFloat("detail.scale", shader.DetailScale);
        shader.DiffuseTexture			= section.GetString("tex.diffOverride", shader.DiffuseTexture);
        shader.NormalTexture			= section.GetString("tex.NM", shader.NormalTexture);
        shader.SpecularTexture			= section.GetString("tex.spec", shader.SpecularTexture);
        shader.EmissiveTexture			= section.GetString("tex.emissiveTex", shader.EmissiveTexture);
        return shader;
    }
}
