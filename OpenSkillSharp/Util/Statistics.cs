using System.Diagnostics.CodeAnalysis;

using MathNet.Numerics.Distributions;

namespace OpenSkillSharp.Util;

/// <summary>
/// Utility for calculating values of a normal distribution.
/// </summary>
public static class Statistics
{
    private static readonly Normal Normal = new(0, 1);
    private const double Epsilon = 1e-10;

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

    public static double V(double x, double t)
    {
        double xt = x - t;
        double denominator = PhiMajor(xt);

        return denominator < Epsilon
            ? -xt
            : PhiMinor(xt) / denominator;
    }

    public static double W(double x, double t)
    {
        double xt = x - t;
        double denominator = PhiMajor(xt);

        if (denominator < Epsilon)
        {
            return x < 0 ? 1 : 0;
        }

        return V(x, t) * (V(x, t) + xt);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static double VT(double x, double t)
    {
        double xx = Math.Abs(x);
        double b = PhiMajor(t - xx) - PhiMajor(-t - xx);

        if (b < Epsilon)
        {
            return x < 0
                ? -x - t
                : -x + t;
        }

        double a = PhiMinor(-t - xx) - PhiMinor(t - xx);
        return (x < 0 ? -a : a) / b;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static double WT(double x, double t)
    {
        double xx = Math.Abs(x);
        double b = PhiMajor(t - xx) - PhiMajor(-t - xx);

        return b < double.Epsilon
            ? 1
            : ((((t - xx) * PhiMinor(t - xx)) + ((t + xx) * PhiMinor(-t - xx))) / b) + (VT(x, t) * VT(x, t));
    }
}