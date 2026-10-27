using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Sani;

public static class SaniQuantizedTracks
{
    private const int FloatQuantizationSize = 8;

    public static void Read(FioReader reader, FioReader frames, bool streamed, bool hasScales, int frameCount, List<SaniChannel> channels)
    {
        int positionCount		= reader.Read<int>();
        int rotationCount		= reader.Read<int>();
        int scaleCount			= hasScales ? reader.Read<int>() : 0;
        int floatCount			= reader.Read<int>();
        int fixedPositionCount	= reader.Read<int>();
        int fixedRotationCount	= reader.Read<int>();
        int fixedScaleCount		= hasScales ? reader.Read<int>() : 0;
        int fixedFloatCount		= reader.Read<int>();

        if (streamed)
        {
            reader.Read<int>();
        }

        ushort[] positionChannels	= reader.ReadArray<ushort>(positionCount);
        ushort[] rotationChannels	= reader.ReadArray<ushort>(rotationCount);
        ushort[] scaleChannels		= reader.ReadArray<ushort>(scaleCount);

        reader.Position += floatCount * sizeof(ushort);

        ushort[] fixedPositionChannels	= reader.ReadArray<ushort>(fixedPositionCount);
        ushort[] fixedRotationChannels	= reader.ReadArray<ushort>(fixedRotationCount);
        ushort[] fixedScaleChannels		= reader.ReadArray<ushort>(fixedScaleCount);

        reader.Position += fixedFloatCount * sizeof(ushort);

        SaniQuantization fixedPositionQuantization	= reader.Read<SaniQuantization>();
        SaniQuantization fixedRotationQuantization	= reader.Read<SaniQuantization>();
        SaniQuantization fixedScaleQuantization		= hasScales ? reader.Read<SaniQuantization>() : default;

        reader.Position += FloatQuantizationSize;

        SaniQuantization[] positionQuantizations	= reader.ReadArray<SaniQuantization>(positionCount);
        SaniQuantization[] rotationQuantizations	= reader.ReadArray<SaniQuantization>(rotationCount);
        SaniQuantization[] scaleQuantizations		= reader.ReadArray<SaniQuantization>(scaleCount);

        reader.Position += floatCount * FloatQuantizationSize;

        SaniUInt16Vector[] fixedPositions	= reader.ReadArray<SaniUInt16Vector>(fixedPositionCount);
        SaniUInt16Vector[] fixedRotations	= reader.ReadArray<SaniUInt16Vector>(fixedRotationCount);
        SaniUInt16Vector[] fixedScales		= reader.ReadArray<SaniUInt16Vector>(fixedScaleCount);

        reader.Position += fixedFloatCount * sizeof(ushort);

        for (int i = 0; i < fixedPositionCount; i++)
        {
            channels[fixedPositionChannels[i]].Positions = [fixedPositionQuantization.Dequantize(fixedPositions[i])];
        }

        for (int i = 0; i < fixedRotationCount; i++)
        {
            channels[fixedRotationChannels[i]].Rotations = [fixedRotationQuantization.DequantizeRotation(fixedRotations[i])];
        }

        for (int i = 0; i < fixedScaleCount; i++)
        {
            channels[fixedScaleChannels[i]].Scales = [fixedScaleQuantization.Dequantize(fixedScales[i])];
        }

        uint[]				positionFrames	= frames.ReadArray<uint>(frameCount * positionCount);
        SaniUInt16Vector[]	rotationFrames	= frames.ReadArray<SaniUInt16Vector>(frameCount * rotationCount);
        SaniUInt16Vector[]	scaleFrames		= frames.ReadArray<SaniUInt16Vector>(frameCount * scaleCount);

        for (int i = 0; i < positionCount; i++)
        {
            channels[positionChannels[i]].Positions = [.. Enumerable.Range(0, frameCount).Select(frame => positionQuantizations[i].Dequantize(positionFrames[frame * positionCount + i]))];
        }

        for (int i = 0; i < rotationCount; i++)
        {
            channels[rotationChannels[i]].Rotations = [.. Enumerable.Range(0, frameCount).Select(frame => rotationQuantizations[i].DequantizeRotation(rotationFrames[frame * rotationCount + i]))];
        }

        for (int i = 0; i < scaleCount; i++)
        {
            channels[scaleChannels[i]].Scales = [.. Enumerable.Range(0, frameCount).Select(frame => scaleQuantizations[i].Dequantize(scaleFrames[frame * scaleCount + i]))];
        }
    }
}
