namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeAnimation
{
    public float SampleFrequency { get; set; }

    public int FrameCount { get; set; } = 1;

    public MorphemeAnimationChannel[] Channels { get; set; } = [];

    public MorphemeAnimationChannel? Trajectory { get; set; }
}
