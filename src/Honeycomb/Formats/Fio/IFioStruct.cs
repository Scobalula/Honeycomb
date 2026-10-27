namespace Honeycomb.Formats.Fio;

public interface IFioStruct
{
    void ReadField(FioReader reader, int index);
}
