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
        var teamRatings = CalculateTeamRatings(teams, ranks).ToList();
        var c = CalculateTeamSqrtSigma(teamRatings);
        var sumQ = CalculateSumQ(teamRatings, c, ranks).ToList();
        var rankOccurrences = teamRatings.CountRankOccurrences().ToList();
        
        var rankGroups = teamRatings
            .Select((tr, i) => new { tr.Rank, Index = i })
            .GroupBy(x => x.Rank)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Index).ToList());
        
        var result = new List<ITeam>();
        foreach (var (teamIdi, teamI) in teamRatings.Index())
        {
            var omega = 0D;
            var delta = 0D;

            // Adjust Mu with margin if scores are present
            var adjustedMuI = teamI.Mu;
            var scoreI = scores?.ElementAtOrDefault(teamIdi);
            if (scoreI.HasValue)
            {
                var marginAdjustment = 0D;
                var comparisonCount = 0;

                foreach (var (teamIdj, teamJ) in teamRatings.Index())
                {
                    var scoreJ = scores?.ElementAtOrDefault(teamIdj);
                    if (teamIdi == teamIdj || !scoreJ.HasValue)
                    {
                        continue;
                    }
                    
                    var scoreDiff = Math.Abs(scoreI.Value - scoreJ.Value);
                    if (scoreDiff <= 0)
                    {
                        continue;
                    }

                    var marginFactor = scoreDiff > Margin && Margin > 0
                        ? Math.Log(1 + scoreDiff / Margin)
                        : 1D;
                    
                    var skillDiff = teamI.Mu - teamJ.Mu;
                    var direction = scoreI.Value > scoreJ.Value ? 1D : -1D;

                    marginAdjustment += skillDiff * (marginFactor - 1) * direction;
                    comparisonCount++;
                }
                
                adjustedMuI += comparisonCount > 0 ? marginAdjustment / comparisonCount : 0;
            }

            var iMuOverC = Math.Exp(adjustedMuI / c);
            
            // Calculate Omega and Delta
            foreach (var (teamIdq, teamQ) in teamRatings.Index())
            {
                var iMuOverCeOverSumQ = iMuOverC / sumQ[teamIdq];

                if (teamQ.Rank > teamI.Rank)
                {
                    continue;
                }
                
                delta += iMuOverCeOverSumQ * (1 - iMuOverCeOverSumQ) / rankOccurrences[teamIdq];
                if (teamQ.Rank == teamI.Rank)
                {
                    omega += (1 - iMuOverCeOverSumQ) / rankOccurrences[teamIdq];
                }
                else
                {
                    omega -= iMuOverCeOverSumQ / rankOccurrences[teamIdq];
                }
            }

            omega *= teamI.SigmaSq / c;
            delta *= teamI.SigmaSq / Math.Pow(c, 2);
            delta *= Gamma(
                c, 
                teamRatings.Count, 
                teamI.Mu, 
                teamI.SigmaSq, 
                teamI.Players,
                teamI.Rank, 
                weights?.ElementAtOrDefault(teamIdi)
            );

            // Update player ratings
            var modifiedTeam = new List<IRating>();
            foreach (var (playerIdj, playerJ) in teamI.Players.Index())
            {
                var weight = weights?.ElementAtOrDefault(teamIdi)?.ElementAtOrDefault(playerIdj) ?? 1D;
                var mu = playerJ.Mu;
                var sigma = playerJ.Sigma;

                if (omega >= 0)
                {
                    mu += (sigma * sigma / teamI.SigmaSq) * omega * weight;
                    sigma *= Math.Sqrt(
                        Math.Max(
                            1 - (sigma * sigma / teamI.SigmaSq) * delta * weight,
                            Kappa
                        )
                    );
                }
                else
                {
                    mu += (sigma * sigma / teamI.SigmaSq) * omega / weight;
                    sigma *= Math.Sqrt(
                        Math.Max(
                            1 - (sigma * sigma / teamI.SigmaSq) * delta / weight,
                            Kappa
                        )
                    );
                }

                var modifiedPlayer = teams.ElementAt(teamIdi).Players.ElementAt(playerIdj);
                modifiedPlayer.Mu = mu;
                modifiedPlayer.Sigma = sigma;
                
                modifiedTeam.Add(modifiedPlayer);
            }
            
            result.Add(new Team { Players = modifiedTeam });
        }

        // Average mu changes for teams that tied
        foreach (var teamIndices in rankGroups.Values.Where(g => g.Count > 1))
        {
            var avgMuChange = teamIndices.Average(i => 
                result.ElementAt(i).Players.First().Mu - teams.ElementAt(i).Players.First().Mu
            );

            foreach (var teamIndex in teamIndices)
            {
                foreach (var (playerIndex, player) in result.ElementAt(teamIndex).Players.Index())
                {
                    player.Mu = teams.ElementAt(teamIndex).Players.ElementAt(playerIndex).Mu + avgMuChange;
                }
            }
        }
        
        return result;
    }
}