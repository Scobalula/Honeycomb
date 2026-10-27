using System.Numerics;

namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeAnimationChannel
{
    public Vector3[] Positions { get; set; } = [];

    public Quaternion[] Rotations { get; set; } = [];
}
