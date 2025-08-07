using MathNet.Numerics.Distributions;

namespace OpenSkillSharp.Util;

/// <summary>
/// Utility for calculating values of a normal distribution.
/// </summary>
public static class Statistics
{
    private static readonly Normal Normal = new(0, 1);

    /// <summary>
    /// Normal cumulative distribution.
    /// </summary>
    public static double PhiMajor(double x)
    {
        return Normal.CumulativeDistribution(x);
    }

    /// <summary>
    /// Normal inverse cumulative distribution.
    /// </summary>
    public static double InversePhiMajor(double x)
    {
        return Normal.InverseCumulativeDistribution(x);
    }

    /// <summary>
    /// Normal probability density.
    /// </summary>
    public static double PhiMinor(double x)
    {
        return Normal.Density(x);
    }
}