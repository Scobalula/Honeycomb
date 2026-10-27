using System.Numerics;

namespace Honeycomb.Formats.Sani;

public readonly record struct SaniQuaternion(float W, float X, float Y, float Z)
{
    public Quaternion ToQuaternion() => new(X, Y, Z, W);
}
