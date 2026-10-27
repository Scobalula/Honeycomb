namespace Honeycomb.Formats.Sound;

public sealed class SoundMediaBundleResource
{
    public string Name { get; private set; } = string.Empty;

    public string BundleFile { get; private set; } = string.Empty;

    public static SoundMediaBundleResource Parse(string text)
    {
        SoundMediaBundleResource resource = new();

        foreach (string rawLine in text.Split('\n'))
        {
            string	line	= rawLine.TrimEnd('\r');
            int		colon	= line.IndexOf(':');

            if (colon < 0)
            {
                continue;
            }

            string key		= line[..colon];
            string value	= line[(colon + 1)..].Trim().Trim('\'');

            switch (key)
            {
                case "name":
                    resource.Name = value;
                    break;
                case "blob_platform_x64":
                    resource.BundleFile = value;
                    break;
            }
        }

        return resource;
    }
}
