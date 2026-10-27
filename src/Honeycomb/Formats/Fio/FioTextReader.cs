using System.Globalization;

namespace Honeycomb.Formats.Fio;

public sealed class FioTextReader
{
    private readonly string	_text;
    private int				_position;

    public FioTextReader(string text)
    {
        _text = text;
    }

    public FioPropertySection ReadSection()
    {
        FioPropertySection section = new();

        while (SkipWhitespace() && _text[_position] != '}')
        {
            string key = _text[_position] == '"' ? ReadQuotedString() : ReadToken();

            if (key.Length == 0 || !SkipWhitespace() || _text[_position] != '=')
            {
                throw new InvalidDataException($"Expected '=' after '{key}' at {_position}.");
            }

            _position++;
            SkipWhitespace();
            section.Values[key] = ReadValue();
        }

        _position++;
        return section;
    }

    private object ReadValue()
    {
        switch (_text[_position])
        {
            case '{':
                _position++;
                return ReadSection();
            case '[':
                _position++;
                return ReadArray();
            case '"':
                return ReadQuotedString();
            default:
                return ParseAtom(ReadToken());
        }
    }

    private object[] ReadArray()
    {
        List<object> values = [];

        while (SkipWhitespace() && _text[_position] != ']')
        {
            if (_text[_position] == ',')
            {
                _position++;
                continue;
            }

            values.Add(ReadValue());
        }

        _position++;
        return values.ToArray();
    }

    private string ReadQuotedString()
    {
        int end = _text.IndexOf('"', _position + 1);

        if (end < 0)
        {
            throw new InvalidDataException($"Unterminated string at {_position}.");
        }

        string value = _text[(_position + 1)..end];

        _position = end + 1;
        return value;
    }

    private string ReadToken()
    {
        int start = _position;

        while (_position < _text.Length && !char.IsWhiteSpace(_text[_position]) && "={}[],\"".IndexOf(_text[_position]) < 0)
        {
            _position++;
        }

        return _text[start.._position];
    }

    private bool SkipWhitespace()
    {
        while (_position < _text.Length)
        {
            if (char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
            else if (_text.AsSpan(_position).StartsWith("//"))
            {
                int end = _text.IndexOf('\n', _position);

                _position = end < 0 ? _text.Length : end + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private static object ParseAtom(string token)
    {
        if (bool.TryParse(token, out bool boolean))
        {
            return boolean;
        }

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
        {
            return integer;
        }

        if (float.TryParse(token.TrimEnd('f'), NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
        {
            return number;
        }

        return token;
    }
}
