namespace Honeycomb.Formats.Fio;

public readonly record struct FioChunk(ushort Id, int End)
{
    public const ushort TerminatorId = 0xFFFF;

    public bool IsTerminator => Id == TerminatorId;
}
