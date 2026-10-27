using RedFox.GameExtraction;

namespace Honeycomb;

/// <summary>
/// Provides the setting definitions, defaults, and shared configuration mapping for the Honeycomb frontends.
/// </summary>
public static class HoneycombSettings
{
    /// <summary>
    /// Gets the settings exposed by the Honeycomb frontends.
    /// </summary>
    public static IReadOnlyList<GameExtractionSetting> Definitions { get; } =
    [
        new GameExtractionSetting
        {
            Name = "OutputDirectory",
            Group = GameExtractionSettingGroup.Export,
            Label = "Output directory",
            Type = GameExtractionSettingType.DirectoryPath,
            DefaultValue = GameExtractionSettings.GetDefaultOutputDirectory("Honeycomb"),
        },
        new GameExtractionSetting
        {
            Name = "Overwrite",
            Group = GameExtractionSettingGroup.Export,
            Label = "Overwrite existing files",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = false,
        },
        new GameExtractionSetting
        {
            Name = "PreserveDirectoryStructure",
            Group = GameExtractionSettingGroup.Export,
            Label = "Preserve directory structure",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = true,
        },
        new GameExtractionSetting
        {
            Name = "ReadRawAssets",
            Group = GameExtractionSettingGroup.Export,
            Label = "Read and export raw assets",
            Type = GameExtractionSettingType.Boolean,
            Description = "Read assets as-is from the source. This setting also affects the previewer.",
            DefaultValue = true,
        },
        GameExtractionLogging.VerboseSetting,
        new GameExtractionSetting
        {
            Name = "ModelFormats",
            Group = GameExtractionSettingGroup.Model,
            Label = "Model export formats",
            Description = "Comma-separated list of model extensions.",
            Type = GameExtractionSettingType.TextArray,
            DefaultValue = ".semodel,.cast,.fbx",
        },
        new GameExtractionSetting
        {
            Name = "ExportModelImages",
            Group = GameExtractionSettingGroup.Model,
            Label = "Export model images",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = true,
        },
        new GameExtractionSetting
        {
            Name = "RelativeModelImages",
            Group = GameExtractionSettingGroup.Model,
            Label = "Export model images to the model's folder.",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = false,
        },
        new GameExtractionSetting
        {
            Name = "ExportHelperGeometry",
            Group = GameExtractionSettingGroup.Model,
            Label = "Export collision and helper geometry",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = false,
        },
        new GameExtractionSetting
        {
            Name = "SkipExistingModels",
            Group = GameExtractionSettingGroup.Model,
            Label = "Skip already exported models.",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = true,
        },
        new GameExtractionSetting
        {
            Name = "AnimationFormats",
            Group = GameExtractionSettingGroup.Animation,
            Label = "Animation export formats",
            Description = "Comma-separated list of animation extensions.",
            Type = GameExtractionSettingType.TextArray,
            DefaultValue = ".seanim,.cast",
        },
        new GameExtractionSetting
        {
            Name = "SkipExistingAnimations",
            Group = GameExtractionSettingGroup.Animation,
            Label = "Skip already exported animations.",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = true,
        },
        new GameExtractionSetting
        {
            Name = "AudioFormats",
            Group = GameExtractionSettingGroup.Sound,
            Label = "Audio export formats",
            Description = "Comma-separated list of audio extensions.",
            Type = GameExtractionSettingType.TextArray,
            DefaultValue = ".wav",
        },
        new GameExtractionSetting
        {
            Name = "SkipExistingAudio",
            Group = GameExtractionSettingGroup.Sound,
            Label = "Skip audio that already exists",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = true,
        },
        new GameExtractionSetting
        {
            Name = "ImageFormats",
            Group = GameExtractionSettingGroup.Image,
            Label = "Image export formats",
            Description = "Comma-separated list of image extensions.",
            Type = GameExtractionSettingType.TextArray,
            DefaultValue = ".png",
        },
        new GameExtractionSetting
        {
            Name = "SkipExistingImages",
            Group = GameExtractionSettingGroup.Image,
            Label = "Skip images that already exist",
            Type = GameExtractionSettingType.Boolean,
            DefaultValue = true,
        },
    ];

    /// <summary>
    /// Creates the default persisted setting values from the setting definitions.
    /// </summary>
    /// <returns>The default settings.</returns>
    public static GameExtractionSettings CreateDefaults()
    {
        GameExtractionSettings settings = new();

        foreach (GameExtractionSetting definition in Definitions)
        {
            settings.SetSettingValue(definition, definition.DefaultValue?.ToString());
        }

        return settings;
    }
}
