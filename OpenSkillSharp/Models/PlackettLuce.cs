using OpenSkillSharp.Domain.Rating;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp.Models;

/// <summary>
/// The Plackett-Luce model departs from singular scalar representations of player performance in simpler models.
/// There is a vector of abilities for each player that captures their performance across multiple dimensions.
/// The outcome of a match between multiple players depends on their abilities in each dimension. By introducing
/// this multidimensional aspect, the Plackett-Luce model provides a richer framework for ranking players
/// based on their abilities in various dimensions.
/// </summary>
public class PlackettLuce : OpenSkillModelBase
{
    protected override IEnumerable<ITeam> Compute(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null
    )
    {
        List<ITeamRating> teamRatings = CalculateTeamRatings(teams, ranks).ToList();
        double c = CalculateTeamSqrtSigma(teamRatings);
        List<double> sumQ = CalculateSumQ(teamRatings, c, ranks).ToList();
        List<int> rankOccurrences = teamRatings.CountRankOccurrences().ToList();
        List<double> adjustedMus = CalculateMarginAdjustedMu(teamRatings, scores).ToList();

        List<Team> result = teamRatings.Select((iTeam, iTeamIndex) =>
        {
            // Calculate omega and delta
            double iMuOverC = Math.Exp(adjustedMus[iTeamIndex] / c);
            (double omega, double delta) = teamRatings
                .Select((qTeam, qTeamIndex) => (qTeam, qTeamIndex))
                .Where(x => x.qTeam.Rank <= iTeam.Rank)
                .Aggregate((sumOmega: 0D, sumDelta: 0D), (acc, x) =>
                {
                    double iMuOverCeOverSumQ = iMuOverC / sumQ[x.qTeamIndex];

                    return (
                        sumOmega: acc.sumOmega + (
                            iTeamIndex == x.qTeamIndex
                                ? 1 - (iMuOverCeOverSumQ / rankOccurrences[x.qTeamIndex])
                                : -1 * iMuOverCeOverSumQ / rankOccurrences[x.qTeamIndex]
                        ),
                        sumDelta: acc.sumDelta +
                                  (iMuOverCeOverSumQ * (1 - iMuOverCeOverSumQ) / rankOccurrences[x.qTeamIndex])
                    );
                });

            omega *= iTeam.SigmaSq / c;
            delta *= iTeam.SigmaSq / Math.Pow(c, 2);
            delta *= Gamma(
                c,
                teamRatings.Count,
                iTeam.Mu,
                iTeam.SigmaSq,
                iTeam.Players,
                iTeam.Rank,
                weights?.ElementAtOrDefault(iTeamIndex)
            );

            // Adjust player ratings
            List<IRating> modifiedTeam = iTeam.Players.Select((_, jPlayerIndex) =>
            {
                IRating modifiedPlayer = teams[iTeamIndex].Players.ElementAt(jPlayerIndex);
                double weight = weights?.ElementAtOrDefault(iTeamIndex)?.ElementAtOrDefault(jPlayerIndex) ?? 1D;
                double scalar = omega >= 0
                    ? weight
                    : 1 / weight;

                modifiedPlayer.Mu += modifiedPlayer.Sigma * modifiedPlayer.Sigma / iTeam.SigmaSq * omega * scalar;
                modifiedPlayer.Sigma *= Math.Sqrt(Math.Max(
                    1 - (modifiedPlayer.Sigma * modifiedPlayer.Sigma / iTeam.SigmaSq * delta * scalar),
                    Kappa
                ));

                return modifiedPlayer;
            }).ToList();

            return new Team { Players = modifiedTeam };
        }).ToList();

        AdjustPlayerMuChangeForTie(teams, teamRatings, result);

        return result;
    }
}