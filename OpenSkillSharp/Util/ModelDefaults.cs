using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Util;

/// <summary>
/// Contains default parameter values shared by all Weng-Lin models.
/// </summary>
public static class ModelDefaults
{
    /// <summary>
    /// 25.0
    /// </summary>
    public const double Mu = 25D;

    /// <summary>
    /// ~ 8.33333
    /// </summary>
    public const double Sigma = 25D / 3;

    /// <summary>
    /// ~ 4.16666
    /// </summary>
    public const double Beta = 25D / 6;

    /// <summary>
    /// 1e-4
    /// </summary>
    public const double Kappa = 0.0001;

    /// <summary>
    /// ~ 0.08333
    /// </summary>
    public const double Tau = 25D / 300;

    /// <summary>
    /// sqrt(sigmaSq) / c
    /// </summary>
    public static readonly GammaFactory Gamma = GammaFormula;

    /// <summary>
    /// (Min: 1.0, Max: 2.0)
    /// </summary>
    public static readonly (double Min, double Max) WeightBounds = (1.0, 2.0);

    /// <summary>
    /// Default gamma function for all Weng-Lin models.
    /// </summary>
    private static double GammaFormula(
        double c,
        double k,
        double mu,
        double sigmaSq,
        IEnumerable<IRating> team,
        double qRank,
        IEnumerable<double>? weights
    )
    {
        return Math.Sqrt(sigmaSq) / c;
    }
}