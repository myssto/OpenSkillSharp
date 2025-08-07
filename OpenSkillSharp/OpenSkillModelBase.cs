using OpenSkillSharp.Domain.Model;
using OpenSkillSharp.Domain.Rating;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp;

/// <summary>
/// Base class for all OpenSkill model implementations.
/// </summary>
public abstract class OpenSkillModelBase : IOpenSkillModel
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
    
    public IEnumerable<double> PredictWin(IList<ITeam> teams)
    {
        var teamRatings = CalculateTeamRatings(teams).ToList();
        var n = teams.Count;
        var denominator = n * (n - 1) / 2;

        return teamRatings.Select((teamA, idx) => teamRatings
                .Where((_, idy) => idx != idy)
                .Sum(teamB => Statistics.PhiMajor(
                    (teamA.Mu - teamB.Mu) / Math.Sqrt(n * BetaSq + teamA.SigmaSq + teamB.SigmaSq)
                )) / denominator
        );
    }

    public double PredictDraw(IList<ITeam> teams)
    {
        var teamRatings = CalculateTeamRatings(teams).ToList();
        
        var playerCount = teamRatings.SelectMany(t => t.Players).Count();
        var drawProbability = 1D / playerCount;
        var drawMargin = Math.Sqrt(playerCount) * Beta * Statistics.InversePhiMajor((1 + drawProbability) / 2D);

        return teamRatings.SelectMany((teamA, i) =>
            teamRatings
                .Skip(i + 1)
                .Select(teamB =>
                {
                    var denominator = Math.Sqrt(playerCount * BetaSq + teamA.SigmaSq + teamB.SigmaSq);
                    return Statistics.PhiMajor((drawMargin - teamA.Mu + teamB.Mu) / denominator)
                           - Statistics.PhiMajor((teamB.Mu - teamA.Mu - drawMargin) / denominator);
                })
        ).Average();
    }

    /// <summary>
    /// Creates team ratings for a game.
    /// </summary>
    /// <param name="teams">A list of teams in a game.</param>
    /// <param name="ranks">
    /// An optional list of numbers representing a rank for each team of <paramref name="teams"/>.
    /// </param>
    /// <returns>A list of team ratings.</returns>
    public IEnumerable<ITeamRating> CalculateTeamRatings(
        IList<ITeam> teams,
        IList<double>? ranks = null
    )
    {
        ranks ??= teams.CalculateRankings().ToList();

        return teams.Select((team, index) =>
        {
            var maxOrdinal = team.Players.Max(p => p.Ordinal);
            var (sumMu, sumSigmaSq) = team.Players
                .Aggregate((mu: 0D, sigmaSq: 0D), (acc, player) =>
                {
                    var balanceWeight = Balance
                        ? 1 + (maxOrdinal - player.Ordinal) / (maxOrdinal + Kappa)
                        : 1D;

                    return (
                        mu: acc.mu + player.Mu * balanceWeight,
                        sigmaSq: acc.sigmaSq + Math.Pow(player.Sigma * balanceWeight, 2)
                    );
                });

            return new TeamRating
            {
                Players = team.Players,
                Mu = sumMu,
                SigmaSq = sumSigmaSq,
                Rank = (int)ranks[index]
            };
        });
    }
    
    /// <summary>
    /// Calculate the square root of the collective team sigma.
    /// </summary>
    /// <param name="teamRatings">A list of team ratings in a game.</param>
    /// <returns>A number representing the square root of the collective team sigma.</returns>
    public double CalculateTeamSqrtSigma(IList<ITeamRating> teamRatings) =>
        Math.Sqrt(teamRatings.Select(t => t.SigmaSq + BetaSq).Sum());
    
    /// <summary>
    /// Sum up all values of (mu / c)^e
    /// </summary>
    /// <param name="teamRatings">A list of team ratings in a game.</param>
    /// <param name="c">The square root of the collective team sigma.</param>
    /// <param name="scores">
    /// An optional list of numbers representing a score for each team of <paramref name="teamRatings"/> used
    /// in margin factor calculation.
    /// </param>
    /// <returns>A list of numbers representing the SumQ for each team</returns>
    public IEnumerable<double> CalculateSumQ(
        IList<ITeamRating> teamRatings, 
        double c, 
        IList<double>? scores = null
    )
    {
        // Calculate margin adjustment for team mu values if ranks are provided
        var adjustedMus = CalculateMarginAdjustedMu(teamRatings, scores).ToList();

        return teamRatings.Select(qTeam => teamRatings
            .Select((iTeam, iTeamIndex) => (iTeam, iTeamIndex))
            .Where(x => x.iTeam.Rank >= qTeam.Rank)
            .Select(x => Math.Exp(adjustedMus[x.iTeamIndex] / c)).Sum()
        );
    }

    public IEnumerable<double> CalculateMarginAdjustedMu(
        IList<ITeamRating> teamRatings,
        IList<double>? scores = null
    )
    {
        if (scores?.Count != teamRatings.Count)
        {
            return teamRatings.Select(t => t.Mu);
        }

        return teamRatings
            .Select((qTeam, qTeamIndex) =>
            {
                var qTeamScore = scores[qTeamIndex];
                var muAdjustment = teamRatings
                    .Where((_, iTeamIndex) =>
                        qTeamIndex != iTeamIndex
                        && Math.Abs(qTeamScore - scores[iTeamIndex]) > 0
                    )
                    .Select((iTeam, iTeamIndex) =>
                    {
                        var iTeamScore = scores[iTeamIndex];
                        var direction = qTeamScore > iTeamScore ? 1D : -1D;
                        var scoreDiff = Math.Abs(qTeamScore - iTeamScore);
                        var marginFactor = scoreDiff > Margin && Margin > 0
                            ? Math.Log(1 + scoreDiff / Margin)
                            : 1D;

                        return (qTeam.Mu - iTeam.Mu) * (marginFactor - 1) * direction;
                    })
                    .Average();

                return qTeam.Mu + muAdjustment;
            });
    }

    public static void AdjustPlayerMuChangeForTie(
        IList<ITeam> originalTeams,
        IList<ITeamRating> teamRatings,
        IEnumerable<ITeam> processedTeams
    )
    {
        var processedTeamsList = processedTeams.ToList();
        var rankGroups = teamRatings
            .Select((tr, i) => new { tr.Rank, Index = i })
            .GroupBy(x => x.Rank)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Index).ToList());
        
        foreach (var teamIndices in rankGroups.Values.Where(g => g.Count > 1))
        {
            var avgMuChange = teamIndices.Average(i => 
                processedTeamsList[i].Players.First().Mu - originalTeams[i].Players.First().Mu
            );
    
            foreach (var teamIndex in teamIndices)
            {
                foreach (var (playerIndex, player) in processedTeamsList[teamIndex].Players.Index())
                {
                    player.Mu = originalTeams[teamIndex].Players.ElementAt(playerIndex).Mu + avgMuChange;
                }
            }
        }
    }
    
    protected static double DefaultGamma(
        double c, 
        double k, 
        double mu, 
        double sigmaSq, 
        IEnumerable<IRating> team, 
        double qRank, 
        IEnumerable<double>? weights
    ) => Math.Sqrt(sigmaSq) / c;

    /// <summary>
    /// Computes the updated ratings for a list of teams in a game.
    /// </summary>
    /// <param name="teams">A list of teams.</param>
    /// <param name="ranks">An optional list representing rank positions for each team.</param>
    /// <param name="scores">An optional list representing scores achieved by each team.</param>
    /// <param name="weights">An optional matrix of weights applied during the computation.</param>
    /// <returns>A list of teams with updated ratings.</returns>
    protected abstract IEnumerable<ITeam> Compute(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null
    );
}