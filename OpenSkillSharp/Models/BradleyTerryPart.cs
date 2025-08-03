using OpenSkillSharp.Domain.Rating;
using OpenSkillSharp.Rating;

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
        var teamRatings = CalculateTeamRatings(teams, ranks).ToList();
        var nTeams = teamRatings.Count;

        var result = new List<ITeam>();
        foreach (var (teamIdi, teamI) in teamRatings.Index())
        {
            var omegaSum = 0D;
            var deltaSum = 0D;
            var comparisonCount = 0;

            var start = Math.Max(0, teamIdi - WindowSize);
            var end = Math.Min(nTeams, teamIdi + WindowSize + 1);

            for (var teamIdq = start; teamIdq < end; teamIdq++)
            {
                if (teamIdq == teamIdi)
                {
                    continue;
                }

                var teamQ = teamRatings.ElementAt(teamIdq);
                var teamQScore = scores?.ElementAtOrDefault(teamIdq);
                var teamIScore = scores?.ElementAtOrDefault(teamIdi);
                var marginFactor = 1D;

                if (teamQScore.HasValue && teamIScore.HasValue)
                {
                    var scoreDiff = Math.Abs(teamQScore.Value - teamIScore.Value);
                    if (scoreDiff > 0 && teamQ.Rank < teamI.Rank && Margin > 0 && scoreDiff > Margin)
                    {
                        marginFactor = Math.Log(1 + scoreDiff / Margin);
                    }
                }
                
                var cIq = Math.Sqrt(teamI.SigmaSq + teamQ.SigmaSq + 2 * BetaSq);
                var pIq = 1 / (1 + Math.Exp((teamQ.Mu - teamI.Mu) * marginFactor / cIq));
                var sigmaToCIq = teamI.SigmaSq / cIq;

                var s = 0D;
                if (teamQ.Rank > teamI.Rank)
                {
                    s = 1D;
                }
                else if (teamQ.Rank == teamI.Rank)
                {
                    s = 0.5;
                }

                omegaSum += sigmaToCIq * (s - pIq);
                var gamma = Gamma(
                    cIq,
                    nTeams,
                    teamI.Mu,
                    teamI.SigmaSq,
                    teamI.Players,
                    teamI.Rank,
                    weights?.ElementAt(teamIdi)
                );
                deltaSum += gamma * sigmaToCIq / cIq * pIq * (1 - pIq);
                comparisonCount++;
            }

            var omega = comparisonCount > 0 
                ? omegaSum / comparisonCount 
                : 0D;
            var delta = comparisonCount > 0
                ? deltaSum / comparisonCount
                : 0D;

            var modifiedTeam = new List<IRating>();
            foreach (var (playerIdj, playerJ) in teamI.Players.Index())
            {
                var weight = weights?.ElementAtOrDefault(teamIdi)?.ElementAtOrDefault(playerIdj) ?? 1D;
                var mu = playerJ.Mu;
                var sigma = playerJ.Sigma;

                if (omega >= 0)
                {
                    mu += sigma * sigma / teamI.SigmaSq * omega * weight;
                    sigma *= Math.Sqrt(
                        Math.Max(
                            1 - (sigma * sigma / teamI.SigmaSq) * delta * weight,
                            Kappa
                        )
                    );
                }
                else
                {
                    mu += sigma * sigma / teamI.SigmaSq * omega / weight;
                    sigma *= Math.Sqrt(
                        Math.Max(
                            1 - sigma * sigma / teamI.SigmaSq * delta / weight,
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

        return result;
    }
}