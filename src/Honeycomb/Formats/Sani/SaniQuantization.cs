using System.Numerics;

namespace Honeycomb.Formats.Sani;

public readonly record struct SaniQuantization(Vector3 Offset, Vector3 Scale)
{
    public Vector3 Dequantize(SaniUInt16Vector value) => new Vector3(value.X, value.Y, value.Z) * Scale + Offset;

    public Vector3 Dequantize(uint packed) => new Vector3(packed >> 21, (packed >> 10) & 0x7FF, packed & 0x3FF) * Scale + Offset;

    public Quaternion DequantizeRotation(SaniUInt16Vector value)
    {
        Vector3	vector	= Dequantize(value);
        float	length	= vector.LengthSquared();
        Vector3	axis	= vector * (2.0f / (1.0f + length));

        return new Quaternion(axis, (1.0f - length) / (1.0f + length));
    }
}
