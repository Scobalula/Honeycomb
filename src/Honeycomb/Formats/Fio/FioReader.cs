using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Honeycomb.Formats.Fio;

public sealed class FioReader
{
    private const int VersionFlag		= 0x20000;
    private const int PrioritizedFlag	= 0x10000;

    private readonly byte[] _data;

    public int Position { get; set; }

    public FioReader(byte[] data, int position)
    {
        _data		= data;
        Position	= position;
    }

    public T Read<T>() where T : unmanaged
    {
        T value = MemoryMarshal.Read<T>(_data.AsSpan(Position));

        Position += Unsafe.SizeOf<T>();
        return value;
    }

    public byte[] ReadBytes(int count)
    {
        byte[] value = _data.AsSpan(Position, count).ToArray();

        Position += count;
        return value;
    }

    public string ReadString()
    {
        int		length	= Read<int>();
        string	value	= Encoding.UTF8.GetString(_data, Position, length);

        Position += length;
        return value;
    }

    public string ReadShortString()
    {
        int		length	= Read<ushort>();
        string	value	= Encoding.UTF8.GetString(_data, Position, length);

        Position += length;
        return value;
    }

    public bool[] ReadFlags()
    {
        int		count	= Read<ushort>();
        byte[]	bits	= ReadBytes((count + 7) / 8);
        bool[]	flags	= new bool[count];

        for (int i = 0; i < count; i++)
        {
            flags[i] = IsSet(bits, i);
        }

        return flags;
    }

    public FioChunk ReadChunk()
    {
        ushort	id	= Read<ushort>();
        int		end	= Read<int>();

        return new FioChunk(id, end);
    }

    public void SkipChunks()
    {
        FioChunk chunk;

        do
        {
            chunk		= ReadChunk();
            Position	= chunk.End;
        }
        while (!chunk.IsTerminator);
    }

    public void SkipSpline()
    {
        FioChunk chunk = ReadChunk();

        while (chunk.Id != 1)
        {
            Position	= chunk.End;
            chunk		= ReadChunk();
        }
    }

    public int ReadStruct(IFioStruct value)
    {
        (int fieldCount, int version) = ReadStructHeader();

        byte[] mask = ReadBytes((fieldCount + 7) / 8);

        for (int field = 0; field < fieldCount; field++)
        {
            if (IsSet(mask, field))
            {
                value.ReadField(this, field);
            }
        }

        return version;
    }

    public int ReadStruct(IFioStruct value, uint signature)
    {
        uint actual = Read<uint>();

        if (actual != signature)
        {
            throw new InvalidDataException($"Expected signature 0x{signature:X8} but found 0x{actual:X8} at 0x{Position - 4:X}.");
        }

        return ReadStruct(value);
    }

    public List<T> ReadStructVector<T>() where T : IFioStruct, new()
    {
        int		count	= Read<int>();
        List<T>	values	= new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(new T());
        }

        (int fieldCount, _) = ReadStructHeader();

        for (int field = 0; field < fieldCount; field++)
        {
            byte	mode	= Read<byte>();
            byte[]	mask	= mode > 1 ? ReadBytes((count + 7) / 8) : [];

            for (int i = 0; i < count; i++)
            {
                if (mode == 1 || (mode > 1 && IsSet(mask, i)))
                {
                    values[i].ReadField(this, field);
                }
            }
        }

        return values;
    }

    public List<T?> ReadVector<T>(Func<FioReader, T> read)
    {
        int			count	= Read<int>();
        byte		mode	= Read<byte>();
        byte[]		mask	= mode > 1 ? ReadBytes((count + 7) / 8) : [];
        List<T?>	values	= new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(mode == 1 || (mode > 1 && IsSet(mask, i)) ? read(this) : default);
        }

        return values;
    }

    public T[] ReadArray<T>(int count) where T : unmanaged
    {
        T[] values = MemoryMarshal.Cast<byte, T>(_data.AsSpan(Position, count * Unsafe.SizeOf<T>())).ToArray();

        Position += count * Unsafe.SizeOf<T>();
        return values;
    }

    public T[] ReadPrimitiveVector<T>() where T : unmanaged
    {
        int	count	= Read<int>();
        T[]	values	= MemoryMarshal.Cast<byte, T>(_data.AsSpan(Position, count * Unsafe.SizeOf<T>())).ToArray();

        Position += count * Unsafe.SizeOf<T>();
        return values;
    }

    private (int FieldCount, int Version) ReadStructHeader()
    {
        int header	= Read<int>();
        int version	= (header & VersionFlag) != 0 ? Read<ushort>() : 0;

        if ((header & PrioritizedFlag) != 0)
        {
            throw new InvalidDataException($"Prioritized fio fields are not supported at 0x{Position:X}.");
        }

        return (header & 0xFFFF, version);
    }

    private static bool IsSet(byte[] bits, int index) => (bits[index >> 3] & (1 << (index & 7))) != 0;
}
