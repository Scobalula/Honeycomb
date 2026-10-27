using System.Numerics;

namespace Honeycomb.Formats.Fio;

public sealed class FioPropertySection
{
    public Dictionary<string, object> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static FioPropertySection Read(FioReader reader)
    {
        FioPropertySection	section	= new();
        int					count	= reader.Read<int>();

        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();

            section.Values[key] = ReadValue(reader);
        }

        return section;
    }

    public static FioPropertySection Parse(string text)
    {
        return new FioTextReader(text).ReadSection();
    }

    public string? GetString(string path) => GetValue(path) as string;

    public FioPropertySection? GetSection(string path) => GetValue(path) as FioPropertySection;

    public string GetString(string path, string fallback) => GetValue(path) as string ?? fallback;

    public bool GetBool(string path, bool fallback) => GetValue(path) is bool value ? value : fallback;

    public float GetFloat(string path, float fallback)
    {
        return GetValue(path) switch
        {
            float value	=> value,
            int value	=> value,
            _			=> fallback,
        };
    }

    public Vector4 GetColor(string path, Vector4 fallback)
    {
        if (GetValue(path) is not object[] { Length: 4 } argb || argb.Any(value => value is not int))
        {
            return fallback;
        }

        return new Vector4((int)argb[1], (int)argb[2], (int)argb[3], (int)argb[0]) / 255.0f;
    }

    private object? GetValue(string path)
    {
        FioPropertySection	section	= this;
        string[]			keys	= path.Split('.');

        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (section.Values.GetValueOrDefault(keys[i]) is not FioPropertySection child)
            {
                return null;
            }

            section = child;
        }

        return section.Values.GetValueOrDefault(keys[^1]);
    }

    private static object ReadValue(FioReader reader)
    {
        int type = reader.Read<int>();

        return type switch
        {
            1	=> reader.Read<int>(),
            2	=> reader.Read<float>(),
            3	=> reader.Read<byte>() != 0,
            4	=> reader.ReadString(),
            5	=> reader.ReadString(),
            6	=> ReadArray(reader),
            7	=> Read(reader),
            _	=> throw new InvalidDataException($"Unknown property type {type} at 0x{reader.Position - 4:X}."),
        };
    }

    private static object[] ReadArray(FioReader reader)
    {
        object[] values = new object[reader.Read<int>()];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = ReadValue(reader);
        }

        return values;
    }
}
