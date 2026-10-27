using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Tests;

public class PredictWinTests
{
    private const double Tolerance = 0.0000001;

    private static readonly PlackettLuce Model = new();
    private static readonly IRating A1 = Model.Rating();
    private static readonly IRating A2 = Model.Rating(32.444, 5.123);
    private static readonly IRating B1 = Model.Rating(73.381, 1.421);
    private static readonly IRating B2 = Model.Rating(25.188, 6.211);
    private static readonly ITeam Team1 = new Team { Players = [A1, A2] };
    private static readonly ITeam Team2 = new Team { Players = [B1, B2] };

    [Theory]
    [InlineData(2, 0.5)]
    [InlineData(3, 0.333333333333)]
    [InlineData(4, 0.25)]
    [InlineData(5, 0.2)]
    public void PredictWin_FFA_NewPlayers(int nTeams, double expectedProbability)
    {
        List<ITeam> teams = Enumerable
            .Range(0, nTeams)
            .Select(ITeam (_) => new Team { Players = [A1] })
            .ToList();

        List<double> probabilities = Model.PredictWin(teams).ToList();

        Assert.All(probabilities, prob => Assert.Equal(expectedProbability, prob, Tolerance));
        Assert.Equal(1, probabilities.Sum(), Tolerance);
    }

    [Fact]
    public void PredictWin_2Teams()
    {
        List<double> probabilities = Model.PredictWin([Team1, Team2]).ToList();

        Assert.Equal(0.0008308945674377, probabilities[0], Tolerance);
        Assert.Equal(0.9991691054325622, probabilities[1], Tolerance);
        Assert.Equal(1, probabilities.Sum(), Tolerance);
    }

    [Fact]
    public void PredictWin_MultipleAsymmetricTeams()
    {
        List<double> probabilities = Model
            .PredictWin([Team1, Team2, new Team { Players = [A2] }, new Team { Players = [B2] }])
            .ToList();

        Assert.Equal(0.32579822053781543, probabilities[0], Tolerance);
        Assert.Equal(0.49965489287103865, probabilities[1], Tolerance);
        Assert.Equal(0.12829642754274315, probabilities[2], Tolerance);
        Assert.Equal(0.04625045904840272, probabilities[3], Tolerance);
        Assert.Equal(1, probabilities.Sum(), Tolerance);
    }

    [Fact]
    public void PredictWin_4PlayerFFA_VaryingSkill()
    {
        List<double> probabilities = Model.PredictWin(
                [
                    new Team { Players = [Model.Rating(1, 0.1)] },
                    new Team { Players = [Model.Rating(2, 0.1)] },
                    new Team { Players = [Model.Rating(3, 0.1)] },
                    new Team { Players = [Model.Rating(4, 0.1)] }
                ]
            )
            .ToList();

        Assert.Equal(0.20281164759988402, probabilities[0], Tolerance);
        Assert.Equal(0.2341964232088598, probabilities[1], Tolerance);
        Assert.Equal(0.2658035767911402, probabilities[2], Tolerance);
        Assert.Equal(0.297188352400116, probabilities[3], Tolerance);
        Assert.Equal(1, probabilities.Sum(), Tolerance);
    }

    [Fact]
    public void PredictWin_5PlayerFFA_WithImposter()
    {
        List<double> probabilities = Model.PredictWin(
                [
                    new Team { Players = [A1] },
                    new Team { Players = [A1] },
                    new Team { Players = [A1] },
                    new Team { Players = [A2] },
                    new Team { Players = [A1] }
                ]
            )
            .ToList();

        Assert.Equal(0.1790804191839367, probabilities[0], Tolerance);
        Assert.Equal(0.1790804191839367, probabilities[1], Tolerance);
        Assert.Equal(0.1790804191839367, probabilities[2], Tolerance);
        Assert.Equal(0.2836783412642534, probabilities[3], Tolerance);
        Assert.Equal(0.1790804191839367, probabilities[4], Tolerance);
        Assert.Equal(1, probabilities.Sum(), Tolerance);
    }
}