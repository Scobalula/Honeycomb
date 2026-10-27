namespace Honeycomb.Formats.Sound;

public sealed class SoundEventResource
{
    public string Name { get; private set; } = string.Empty;

    public List<string> Banks { get; } = [];

    public List<string> MediaBundles { get; } = [];

    public static SoundEventResource Parse(string text)
    {
        SoundEventResource	resource	= new();
        string				listKey		= string.Empty;

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            if (line.StartsWith("- "))
            {
                string item = line[2..].Trim().Trim('\'');

                switch (listKey)
                {
                    case "banks":
                        resource.Banks.Add(item);
                        break;
                    case "media_bundles":
                        resource.MediaBundles.Add(item);
                        break;
                }

                continue;
            }

            int colon = line.IndexOf(':');

            if (colon < 0)
            {
                continue;
            }

            string key		= line[..colon];
            string value	= line[(colon + 1)..].Trim().Trim('\'');

            listKey = key;

            switch (key)
            {
                case "name":
                    resource.Name = value;
                    break;
            }
        }

        return resource;
    }
}
