using System.Numerics;

namespace Honeycomb.Formats.Sani;

public sealed class SaniChannel
{
    public string Name { get; }

    public Vector3[] Positions { get; set; } = [];

    public Quaternion[] Rotations { get; set; } = [];

    public Vector3[] Scales { get; set; } = [];

    public SaniChannel(string name)
    {
        Name = name;
    }
}
