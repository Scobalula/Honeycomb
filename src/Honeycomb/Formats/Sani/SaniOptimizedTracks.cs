using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Sani;

public static class SaniOptimizedTracks
{
    public static void Read(FioReader reader, FioReader frames, bool streamed, bool hasScales, int frameCount, int floatChannelCount, List<SaniChannel> channels)
    {
        int positionCount	= reader.Read<int>();
        int rotationCount	= reader.Read<int>();
        int scaleCount		= hasScales ? reader.Read<int>() : 0;
        int floatCount		= reader.Read<int>();

        if (streamed)
        {
            reader.Read<int>();
        }

        ushort[] positionChannels	= reader.ReadArray<ushort>(positionCount);
        ushort[] rotationChannels	= reader.ReadArray<ushort>(rotationCount);
        ushort[] scaleChannels		= reader.ReadArray<ushort>(scaleCount);

        reader.Position += floatCount * sizeof(ushort);

        Vector4[]			fixedPositions	= reader.ReadArray<Vector4>(channels.Count);
        SaniQuaternion[]	fixedRotations	= reader.ReadArray<SaniQuaternion>(channels.Count);
        Vector3[]			fixedScales		= reader.ReadArray<Vector3>(hasScales ? channels.Count : 0);
        int					paddedPositions	= (positionCount + 3) & ~3;
        int					paddedRotations	= (rotationCount + 3) & ~3;
        int					paddedScales	= (scaleCount + 3) & ~3;

        reader.Position += floatChannelCount * sizeof(float);

        for (int i = 0; i < channels.Count; i++)
        {
            channels[i].Positions = [fixedPositions[i].AsVector3()];
            channels[i].Rotations = [fixedRotations[i].ToQuaternion()];
        }

        for (int i = 0; i < fixedScales.Length; i++)
        {
            channels[i].Scales = [fixedScales[i]];
        }

        Vector4[]			positionFrames	= frames.ReadArray<Vector4>(frameCount * paddedPositions);
        SaniQuaternion[]	rotationFrames	= frames.ReadArray<SaniQuaternion>(frameCount * paddedRotations);
        Vector3[]			scaleFrames		= frames.ReadArray<Vector3>(frameCount * paddedScales);

        for (int i = 0; i < positionCount; i++)
        {
            channels[positionChannels[i]].Positions = [.. Enumerable.Range(0, frameCount).Select(frame => positionFrames[frame * paddedPositions + i].AsVector3())];
        }

        for (int i = 0; i < rotationCount; i++)
        {
            channels[rotationChannels[i]].Rotations = [.. Enumerable.Range(0, frameCount).Select(frame => rotationFrames[frame * paddedRotations + i].ToQuaternion())];
        }

        for (int i = 0; i < scaleCount; i++)
        {
            channels[scaleChannels[i]].Scales = [.. Enumerable.Range(0, frameCount).Select(frame => scaleFrames[frame * paddedScales + i])];
        }
    }
}
