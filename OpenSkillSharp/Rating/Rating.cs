using OpenSkillSharp.Util;

namespace OpenSkillSharp.Rating;

public class Rating : IRating
{
    public double Mu { get; set; } = ModelDefaults.Mu;

    public double Sigma { get; set; } = ModelDefaults.Sigma;

    public double Ordinal => GetOrdinal();

    public IRating Clone()
    {
        return new Rating { Mu = Mu, Sigma = Sigma };
    }

    public double GetOrdinal(double z = 3, double alpha = 1, double target = 0)
    {
        return alpha * (Mu - (z * Sigma) + (target / alpha));
    }
}