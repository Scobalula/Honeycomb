namespace Honeycomb.Formats.Tpl;

public sealed class TplResource
{
    public string TplFile { get; private set; } = string.Empty;

    public string TplDataFile { get; private set; } = string.Empty;

    public static TplResource FromTpl(string tplFile)
    {
        return new TplResource
        {
            TplFile		= tplFile,
            TplDataFile	= tplFile + "_data",
        };
    }

    public static TplResource Parse(string text)
    {
        TplResource resource = new();

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
                case "tpl":
                    resource.TplFile = value;
                    break;
                case "tplData":
                    resource.TplDataFile = value;
                    break;
            }
        }

        return resource;
    }
}
