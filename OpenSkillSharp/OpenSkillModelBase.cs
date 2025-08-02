using OpenSkillSharp.Domain.Model;
using OpenSkillSharp.Domain.Rating;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp;

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
    
    public IEnumerable<double> CalculateSumQ(
        IList<ITeamRating> teamRatings, 
        double c, 
        IList<double>? scores = null
    )
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
    
    protected static double DefaultGamma(
        double c, 
        double k, 
        double mu, 
        double sigmaSq, 
        IEnumerable<IRating> team, 
        double qRank, 
        IEnumerable<double>? weights
    ) => Math.Sqrt(sigmaSq) / c;

    protected abstract IEnumerable<ITeam> Compute(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null
    );
}