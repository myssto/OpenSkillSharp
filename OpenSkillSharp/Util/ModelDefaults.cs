using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Util;

/// <summary>
/// Contains default parameter values shared by all Weng-Lin models.
/// </summary>
public static class ModelDefaults
{
    public const double Mu = 25D;

    public const double Sigma = 25D / 3;

    public const double Beta = 25D / 6;

    public const double Kappa = 0.0001;

    public static readonly GammaFactory Gamma = GammaFormula;

    public const double Tau = 25D / 300;

    public static readonly (double Min, double Max) WeightBounds = (1.0, 2.0);

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