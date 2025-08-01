using MathNet.Numerics.Distributions;

namespace OpenSkillSharp.Util;

/// <summary>
/// Utility for calculating values of a normal distribution.
/// </summary>
public static class Statistics
{
    private static readonly Normal Normal = new(mean: 0, stddev: 1);

    /// <summary>
    /// Normal cumulative distribution.
    /// </summary>
    public static double PhiMajor(double x) =>
        Normal.CumulativeDistribution(x);
    
    /// <summary>
    /// Normal inverse cumulative distribution.
    /// </summary>
    public static double InversePhiMajor(double x) =>
        Normal.InverseCumulativeDistribution(x);

    /// <summary>
    /// Normal probability density.
    /// </summary>
    public static double PhiMinor(double x) =>
        Normal.Density(x);
}