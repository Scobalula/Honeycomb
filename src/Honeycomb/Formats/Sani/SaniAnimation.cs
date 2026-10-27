using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Sani;

public sealed class SaniAnimation
{
    public const uint Signature = 0x414E5445;

    private const ushort MetaChunk				= 0xF1;
    private const ushort AnimationChunk			= 0xF2;
    private const ushort TrajectoryChunk		= 0xF4;
    private const ushort EventsChunk			= 0xF5;
    private const ushort StreamedAnimationChunk	= 0xF6;
    private const ushort QuantizedCompression	= 2;
    private const ushort ScaledVersion			= 3;

    private const int EventTrackGuidSize = 16;

    private int _floatChannelCount;

    public ushort Version { get; private set; }

    public float Duration { get; private set; }

    public float Framerate { get; private set; }

    public int FrameCount { get; private set; }

    public List<SaniChannel> Channels { get; } = [];

    public List<SaniEvent> Events { get; } = [];

    public Vector3[] TrajectoryPositions { get; private set; } = [];

    public Quaternion[] TrajectoryRotations { get; private set; } = [];

    public static SaniAnimation Read(byte[] data, byte[]? streamingData)
    {
        SaniAnimation	animation	= new();
        FioReader		reader		= new(data, 0);
        uint			signature	= reader.Read<uint>();

        if (signature != Signature)
        {
            throw new InvalidDataException($"Expected signature 0x{Signature:X8} but found 0x{signature:X8}.");
        }

        animation.Version = reader.Read<ushort>();

        if (animation.Version is not (1 or 2 or ScaledVersion))
        {
            throw new NotSupportedException($"Unsupported animation version {animation.Version}.");
        }

        while (reader.Position < data.Length)
        {
            FioChunk chunk = reader.ReadChunk();

            switch (chunk.Id)
            {
                case MetaChunk:
                    animation.ReadMeta(reader);
                    break;
                case AnimationChunk:
                    animation.ReadTracks(reader, reader, false);
                    break;
                case StreamedAnimationChunk:
                    animation.ReadTracks(reader, OpenStreamedFrames(streamingData), true);
                    break;
                case TrajectoryChunk:
                    animation.ReadTrajectory(reader);
                    break;
                case EventsChunk:
                    animation.ReadEvents(reader);
                    break;
            }

            reader.Position = chunk.End;
        }

        return animation;
    }

    private void ReadMeta(FioReader reader)
    {
        Duration	= reader.Read<float>();
        Framerate	= reader.Read<float>();
        FrameCount	= reader.Read<int>();

        int channelCount = reader.Read<int>();

        _floatChannelCount = reader.Read<int>();

        for (int i = 0; i < channelCount; i++)
        {
            Channels.Add(new SaniChannel(reader.ReadString()));
        }
    }

    private void ReadTracks(FioReader reader, FioReader frames, bool streamed)
    {
        if (ReadCompression(reader) == QuantizedCompression)
        {
            SaniQuantizedTracks.Read(reader, frames, streamed, Version == ScaledVersion, FrameCount, Channels);
        }
        else
        {
            SaniOptimizedTracks.Read(reader, frames, streamed, Version == ScaledVersion, FrameCount, _floatChannelCount, Channels);
        }
    }

    private void ReadTrajectory(FioReader reader)
    {
        if (ReadCompression(reader) == QuantizedCompression)
        {
            SaniQuantization positionQuantization = reader.Read<SaniQuantization>();
            SaniQuantization rotationQuantization = reader.Read<SaniQuantization>();

            TrajectoryPositions = [.. reader.ReadArray<SaniUInt16Vector>(FrameCount).Select(value => positionQuantization.Dequantize(value))];
            TrajectoryRotations = [.. reader.ReadArray<SaniUInt16Vector>(FrameCount).Select(value => rotationQuantization.DequantizeRotation(value))];
        }
        else
        {
            TrajectoryPositions = [.. reader.ReadArray<Vector4>(FrameCount).Select(value => value.AsVector3())];
            TrajectoryRotations = [.. reader.ReadArray<SaniQuaternion>(FrameCount).Select(value => value.ToQuaternion())];
        }
    }

    private void ReadEvents(FioReader reader)
    {
        int trackCount = reader.Read<int>();

        for (int i = 0; i < trackCount; i++)
        {
            string name = reader.ReadString();

            reader.Position += EventTrackGuidSize;
            reader.Read<bool>();
            reader.Read<int>();

            int eventCount = reader.Read<int>();

            for (int j = 0; j < eventCount; j++)
            {
                float start = reader.Read<float>();

                reader.Position += sizeof(float) + sizeof(uint);

                int parameterCount = reader.Read<int>();

                reader.Position += parameterCount * sizeof(int);

                Events.Add(new SaniEvent(name, start * Duration));
            }
        }
    }

    private ushort ReadCompression(FioReader reader) => Version >= 2 ? reader.Read<ushort>() : (ushort)1;

    private static FioReader OpenStreamedFrames(byte[]? streamingData)
    {
        if (streamingData is null)
        {
            throw new InvalidDataException("Animation frames are streamed from a .sani_data file that was not provided.");
        }

        FioReader	reader	= new(streamingData, 0);
        FioChunk	chunk	= reader.ReadChunk();

        while (chunk.Id != AnimationChunk)
        {
            reader.Position	= chunk.End;
            chunk			= reader.ReadChunk();
        }

        return reader;
    }
}
