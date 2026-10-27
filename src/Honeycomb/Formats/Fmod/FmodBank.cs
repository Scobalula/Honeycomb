using System.Buffers.Binary;

namespace Honeycomb.Formats.Fmod;

public sealed class FmodBank
{
    private const int RiffHeaderSize	= 12;
    private const int ChunkHeaderSize	= 8;

    public byte[]? SoundBank { get; private set; }

    public static FmodBank Read(byte[] data)
    {
        FmodBank	bank		= new();
        int			position	= RiffHeaderSize;

        while (position + ChunkHeaderSize <= data.Length)
        {
            ReadOnlySpan<byte>	id		= data.AsSpan(position, 4);
            int					size	= BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position + 4));
            int					start	= position + ChunkHeaderSize;

            if (id.SequenceEqual("SND "u8))
            {
                int offset = data.AsSpan(start, size).IndexOf("FSB5"u8);

                bank.SoundBank = data.AsSpan(start + offset, size - offset).ToArray();
                break;
            }

            position = start + size + (size & 1);
        }

        return bank;
    }
}
