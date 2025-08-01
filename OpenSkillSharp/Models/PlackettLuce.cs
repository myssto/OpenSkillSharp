using OpenSkillSharp.Domain.Model;
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
public class PlackettLuce : IOpenSkillModel
{
    public double Mu { get; set; } = 25D;

    public double Sigma { get; set; } = 25D / 3;

    public double Beta { get; set; } = 25D / 6;
    
    public double BetaSq => Math.Pow(Beta, 2);
    
    public double Kappa { get; set; } = 0.0001;
    
    public GammaFactory Gamma { get; set; } = DefaultGamma;
    
    public double Tau { get; set; } = 25D / 300;

    public double Margin { get; set; }
    
    public bool LimitSigma { get; set; }
    
    public bool Balance { get; set; }

    public IRating Rating(double? mu = null, double? sigma = null) => new Rating.Rating
    {
        Mu = mu ?? Mu,
        Sigma = sigma ?? Sigma,
    };

    public IEnumerable<ITeam> Rate(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null,
        double? tau = null
    )
    {
        if (ranks is not null)
        {
            if (!ranks.IsEqualLengthTo(teams))
            {
                throw new ArgumentException($"Arguments '{nameof(ranks)}' and '{nameof(teams)}' must be of equal length.");
            }

            if (scores is not null)
            {
                throw new ArgumentException(
                    $"Cannot except both '{nameof(ranks)}' and '{nameof(scores)}' at the same time."
                );
            }
        }

        if (scores is not null && !scores.IsEqualLengthTo(teams))
        {
            throw new ArgumentException($"Arguments '{nameof(scores)}' and '{nameof(teams)}' must be of equal length.");
        }

        if (weights is not null)
        {
            if (!weights.IsEqualLengthTo(teams))
            {
                throw new ArgumentException($"Arguments '{nameof(weights)}' and '{nameof(teams)}' must be of equal length.");
            }

            for (var i = 0; i < weights.Count; i++)
            {
                if (!weights[i].IsEqualLengthTo(teams[i].Players))
                {
                    throw new ArgumentException($"Size of team weights at index {i} does not match the size of the team.");
                }
            }
        }
        
        // Create a deep copy of the given teams
        var originalTeams = teams;
        teams = originalTeams.Select(t => t.Clone()).ToList();

        // Correct sigma
        tau ??= Tau;
        var tauSq = Math.Pow(tau.Value, 2);
        foreach (var player in teams.SelectMany(t => t.Players))
        {
            player.Sigma = Math.Sqrt(player.Sigma * player.Sigma + tauSq);
        }

        // Convert score to ranks
        if (ranks is null && scores is not null)
        {
            ranks = teams.CalculateRankings(scores.Select(s => -s).ToList()).ToList();
        }
        
        // Normalize weights
        weights = weights?.Select(w => w.Normalize(1, 2)).ToList();

        IList<double>? tenet = null;
        if (ranks is not null)
        {
            var (orderedTeams, orderedRanks) = ranks.Unwind(teams);

            if (weights is not null)
            {
                (weights, _) = ranks.Unwind(weights);
            }

            tenet = orderedRanks;
            teams = orderedTeams;
            ranks = ranks.OrderBy(r => r).ToList();
        }

        IList<ITeam> finalResult;
        if (ranks is not null && tenet is not null)
        {
            (finalResult, _) = tenet.Unwind(Compute(teams, ranks, scores, weights).ToList());
        }
        else
        {
            finalResult = Compute(teams, weights: weights).ToList();
        }
        
        if (LimitSigma)
        {
            foreach (var (teamIdx, team) in finalResult.Index())
            {
                foreach (var (playerIdx, player) in team.Players.Index())
                {
                    player.Sigma = Math.Min(
                        player.Sigma, 
                        originalTeams.ElementAt(teamIdx).Players.ElementAt(playerIdx).Sigma
                    );
                }
            }
        }
        
        return finalResult;
    }

    private IEnumerable<ITeam> Compute(
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

    public IEnumerable<ITeamRating> CalculateTeamRatings(
        IList<ITeam> teams,
        IList<double>? ranks = null
    )
    {
        ranks ??= teams.CalculateRankings().ToList();

        return teams.Select((team, teamIdx) =>
        {
            var maxOrdinal = team.Players.Max(p => p.Ordinal);
            var sumMu = 0D;
            var sumSigmaSq = 0D;

            foreach (var player in team.Players)
            {
                var balanceWeight = Balance
                    ? 1 + (maxOrdinal - player.Ordinal) / (maxOrdinal + Kappa)
                    : 1D;
                
                sumMu += player.Mu * balanceWeight;
                sumSigmaSq += Math.Pow(player.Sigma * balanceWeight, 2);
            }

            return new TeamRating
            {
                Players = team.Players,
                Mu = sumMu,
                SigmaSq = sumSigmaSq,
                Rank = (int)ranks[teamIdx]
            };
        });
    }

    public double CalculateTeamSqrtSigma(IList<ITeamRating> teamRatings) =>
        Math.Sqrt(teamRatings.Select(t => t.SigmaSq + BetaSq).Sum());
    
    public IEnumerable<double> CalculateSumQ(IList<ITeamRating> teamRatings, double c, IList<double>? scores = null)
    {
        var sumQ = new Dictionary<int, double>();
        
        foreach (var (teamIdx, teamX) in teamRatings.Index())
        {
            var adjustedMu = teamX.Mu;

            if (scores is not null && scores.Count == teamRatings.Count)
            {
                var marginAdjustment = 0D;
                var comparisonCount = 0;

                foreach (var (teamIdy, teamY) in teamRatings.Index())
                {
                    if (teamIdx == teamIdy)
                    {
                        continue;
                    }

                    var scoreDiff = Math.Abs(scores.ElementAt(teamIdx) - scores.ElementAt(teamIdy));
                    if (scoreDiff <= 0)
                    {
                        continue;
                    }

                    var marginFactor = scoreDiff > Margin && Margin > 0
                        ? Math.Log(1 + scoreDiff / Margin)
                        : 1D;
                    
                    var skillDiff = teamX.Mu - teamY.Mu;
                    var direction = scores.ElementAt(teamIdx) > scores.ElementAt(teamIdy) 
                        ? 1D 
                        : -1D;

                    marginAdjustment += skillDiff * (marginFactor - 1) * direction;
                    comparisonCount++;
                }
                
                adjustedMu += comparisonCount > 0 ? marginAdjustment / comparisonCount : 0;
            }
            
            var summed = Math.Exp(adjustedMu / c);
            foreach (var (teamIdy, teamY) in teamRatings.Index())
            {
                if (teamX.Rank >= teamY.Rank)
                {
                    sumQ[teamIdy] = sumQ.TryGetValue(teamIdy, out var current) 
                        ? current + summed 
                        : summed;
                }
            }
        }
        
        return sumQ.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
    }

    /// <summary>
    /// Default gamma function for Plackett-Luce.
    /// </summary>
    private static double DefaultGamma(
        double c, 
        double k, 
        double mu, 
        double sigmaSq, 
        IEnumerable<IRating> team, 
        double qRank, 
        IEnumerable<double>? weights
    ) => Math.Sqrt(sigmaSq) / c;
}