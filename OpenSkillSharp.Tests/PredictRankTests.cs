using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Tests;

public class PredictRankTests
{
    private const double Tolerance = 0.0000001;
    
    private static readonly PlackettLuce Model = new();
    private static readonly ITeam TeamA = new Team { Players = [Model.Rating(34, 0.25), Model.Rating(24, 0.5)] };

    [Fact]
    public void PredictRank_ProducesTotalProbabilityOf1()
    {
        List<(int rank, double probability)> probabilities = Model.PredictRank(
            [
                TeamA,
                new Team { Players = [Model.Rating(32, 0.25), Model.Rating(22, 0.5)] },
                new Team { Players = [Model.Rating(30, 0.25), Model.Rating(20, 0.5)] }
            ]
        );

        Assert.Equal(1, probabilities.Sum(p => p.probability), Tolerance);
    }

    [Fact]
    public void PredictRank_GivenIdenticalTeams_ProducesTotalProbabilityOf1()
    {
        List<(int rank, double probability)> probabilities = Model.PredictRank([TeamA, TeamA, TeamA]);

        Assert.Equal(1, probabilities.Sum(p => p.probability), Tolerance);
    }
}