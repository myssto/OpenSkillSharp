using OpenSkillSharp.Domain.Rating;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp.Models;

/// <summary>
/// The Bradley-Terry Part model maintains the single scalar value representation of player performance,
/// enables rating updates based on match outcomes, and utilizes a logistic regression approach for rating
/// estimation. By allowing for partial pairing situations, this model caters to scenarios where not all
/// players face each other directly and still provides accurate rating estimates.
/// </summary>
public class BradleyTerryPart : OpenSkillModelBase
{
    /// <summary>
    /// The sliding window size for partial pairing such that a larger window size tends to full pairing mode accuracy.
    /// </summary>
    public int WindowSize { get; set; } = 4;

    protected override IEnumerable<ITeam> Compute(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null
    )
    {
        List<ITeamRating> teamRatings = CalculateTeamRatings(teams, ranks).ToList();

        return teamRatings
            .Select((iTeam, iTeamIndex) =>
            {
                // Calculate omega and delta
                int comparisonCount = 0;
                (double omega, double delta) = teamRatings
                    .Index()
                    .Skip(Math.Max(0, iTeamIndex - WindowSize))
                    .Take(Math.Min(teamRatings.Count, iTeamIndex + WindowSize + 1))
                    .Where(q => q.Index != iTeamIndex)
                    .Aggregate((sumOmega: 0D, sumDelta: 0D), (acc, q) =>
                    {
                        (int qTeamIndex, ITeamRating qTeam) = q;

                        // Margin factor adjustment
                        double marginFactor = 1;
                        if (scores is not null)
                        {
                            double scoreDiff = Math.Abs(scores[qTeamIndex] - scores[iTeamIndex]);
                            if (scoreDiff > 0 && Margin > 0 && scoreDiff > Margin && qTeam.Rank < iTeam.Rank)
                            {
                                marginFactor = Math.Log(1 + (scoreDiff / Margin));
                            }
                        }

                        double ciq = Math.Sqrt(iTeam.SigmaSq + qTeam.SigmaSq + (2 * BetaSq));
                        double piq = 1 / (1 + Math.Exp((qTeam.Mu - iTeam.Mu) * marginFactor / ciq));
                        double sigmaToCiq = iTeam.SigmaSq / ciq;
                        double s = Common.Score(qTeam.Rank, iTeam.Rank);
                        double gamma = Gamma(
                            ciq,
                            teamRatings.Count,
                            iTeam.Mu,
                            iTeam.SigmaSq,
                            iTeam.Players,
                            iTeam.Rank,
                            weights?.ElementAt(iTeamIndex)
                        );

                        comparisonCount++;
                        return (
                            sumOmega: acc.sumOmega + (sigmaToCiq * (s - piq)),
                            sumDelta: acc.sumDelta + (gamma * sigmaToCiq / ciq * piq * (1 - piq))
                        );
                    });

                omega = comparisonCount > 0 ? omega / comparisonCount : omega;
                delta = comparisonCount > 0 ? delta / comparisonCount : delta;

                // Adjust player ratings
                List<IRating> modifiedTeam = iTeam.Players.Select((_, jPlayerIndex) =>
                {
                    IRating modifiedPlayer = teams[iTeamIndex].Players.ElementAt(jPlayerIndex);
                    double weight = weights?.ElementAtOrDefault(iTeamIndex)?.ElementAtOrDefault(jPlayerIndex) ?? 1D;
                    double weightScalar = omega >= 0
                        ? weight
                        : 1 / weight;

                    modifiedPlayer.Mu += modifiedPlayer.Sigma * modifiedPlayer.Sigma / iTeam.SigmaSq * omega *
                                         weightScalar;
                    modifiedPlayer.Sigma *= Math.Sqrt(Math.Max(
                        1 - (modifiedPlayer.Sigma * modifiedPlayer.Sigma / iTeam.SigmaSq * delta * weightScalar),
                        Kappa
                    ));

                    return modifiedPlayer;
                }).ToList();

                return new Team { Players = modifiedTeam };
            }).ToList();
    }
}