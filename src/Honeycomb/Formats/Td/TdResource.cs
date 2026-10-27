namespace Honeycomb.Formats.Td;

public sealed class TdResource
{
    public string TdFile { get; private set; } = string.Empty;

    public static TdResource Parse(string text)
    {
        TdResource resource = new();

        foreach (string rawLine in text.Split('\n'))
        {
            string	line	= rawLine.TrimEnd('\r');
            int		colon	= line.IndexOf(':');

            if (colon >= 0 && line[..colon] == "td")
            {
                resource.TdFile = line[(colon + 1)..].Trim().Trim('\'');
            }
        }

        return resource;
    }
}
