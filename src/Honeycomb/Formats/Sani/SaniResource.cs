using System.Text.Json;
using System.Text.Json.Serialization;

namespace Honeycomb.Formats.Sani;

public sealed class SaniResource
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("animation")]
    public string AnimationFile { get; init; } = string.Empty;

    [JsonPropertyName("animData")]
    public string? AnimationDataFile { get; init; }

    public static SaniResource Parse(string text)
    {
        return JsonSerializer.Deserialize<SaniResource>(text) ?? throw new InvalidDataException("Animation resource is empty.");
    }
}
