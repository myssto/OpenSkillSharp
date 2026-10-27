using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Tests.TestingUtil;
using OpenSkillSharp.Util;

namespace OpenSkillSharp.Tests;

/// <summary>
/// Tests methods shared between all models implemented in <see cref="OpenSkillModelBase"/>.
/// </summary>
/// <remarks>
/// Tests use <see cref="PlackettLuce"/> as a baseline instead of individually targeting each model. All methods
/// targeted in the tests are concrete implementations and not allowed to be overridden by the implementing model.
/// </remarks>
public class ModelBaseTests
{
    /// <summary>
    /// The two exceptions to the above remarks apply to testing the behavior of weight bounds and ties.
    /// Even though the <see cref="OpenSkillModelBase.Rate"/> method is concrete, the normalized weights
    /// have unique usage across implementations of <see cref="OpenSkillModelBase.Compute"/>. That being said,
    /// the test setup does not change across implementations and should yield the same result, so using a generic
    /// matrix of the model types ensures parity.
    /// </summary>
    public static TheoryData<Type> ModelTypes => new()
    {
        typeof(BradleyTerryFull),
        typeof(BradleyTerryPart),
        typeof(PlackettLuce),
        typeof(ThurstoneMostellerFull),
        typeof(ThurstoneMostellerPart)
    };

    // Stacking multiple @pytest.mark.parametrize tags will create a test for every case in the cartesian product
    // yes that is 600 cases for a single test
    public static IEnumerable<object[]> TieTestData()
    {
        foreach (object[] modelType in ModelTypes)
        foreach (double tieScore in new[] { -1, 0, 0.1, 10, 13.4 })
        foreach (int nTeams in new[] { 2, 5, 10 })
        foreach (int nPlayers in new[] { 1, 2, 5, 10 })
        foreach (string tieType in new[] { "score", "rank" })
        {
            yield return [modelType[0], tieScore, nTeams, nPlayers, tieType];
        }
    }

    [Fact]
    public void ModelValues_Defaults()
    {
        PlackettLuce model = new();

        Assert.Equal(ModelDefaults.Mu, model.Mu);
        Assert.Equal(ModelDefaults.Sigma, model.Sigma);
        Assert.Equal(ModelDefaults.Beta, model.Beta);
        Assert.Equal(ModelDefaults.Kappa, model.Kappa);
        Assert.Equal(ModelDefaults.Tau, model.Tau);
        Assert.False(model.LimitSigma);
        Assert.False(model.Balance);
        Assert.NotNull(model.WeightBounds);
        Assert.Equal(ModelDefaults.WeightBounds.Min, model.WeightBounds.Value.Min);
        Assert.Equal(ModelDefaults.WeightBounds.Max, model.WeightBounds.Value.Max);
    }

    [Fact]
    public void Rate_ArgumentExceptions()
    {
        PlackettLuce model = new();

        // Teams < 2
        Assert.Throws<ArgumentException>(() => model.Rate([]));
        Assert.Throws<ArgumentException>(() => model.Rate([new Team()]));

        // Teams with no players
        Assert.Throws<ArgumentException>(() => model.Rate([new Team(), new Team()]));
        Assert.Throws<ArgumentException>(() => model.Rate([new Team { Players = [model.Rating()] }, new Team()]));

        // Ranks size mismatch
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                [1]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                [1, 2, 3]
            )
        );

        // Scores size mismatch
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                scores: [1]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                scores: [1, 2, 3]
            )
        );

        // Ranks and scores together
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                [1, 2],
                [1, 2]
            )
        );

        // Weights size mismatch
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                weights: [[1]]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                weights: [[1], [2], [3]]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                model.MockTeams(2, 1),
                weights: [[1], [2, 3, 4]]
            )
        );
    }

    [Theory]
    [MemberData(nameof(ModelTypes))]
    public void Rate_CustomWeightBounds(Type modelType)
    {
        IOpenSkillModel modelDefault = (IOpenSkillModel)Activator.CreateInstance(modelType)!;
        IOpenSkillModel modelNarrow = (IOpenSkillModel)Activator.CreateInstance(modelType)!;
        modelNarrow.WeightBounds = (0.9, 1.1);
        IList<IList<double>> weights = [[0.5, 1.0, 2.0], [1.0, 1.0, 1.0]];

        List<ITeam> resultDefault = modelDefault.Rate(
                modelDefault.MockTeams(2, 3),
                [1, 2],
                weights: weights
            )
            .ToList();

        List<ITeam> resultNarrow = modelNarrow.Rate(
                modelNarrow.MockTeams(2, 3),
                [1, 2],
                weights: weights
            )
            .ToList();

        double defaultSpread = resultDefault[0].Players.ElementAt(2).Mu - resultDefault[0].Players.ElementAt(0).Mu;
        double narrowSpread = resultNarrow[0].Players.ElementAt(2).Mu - resultNarrow[0].Players.ElementAt(0).Mu;

        Assert.True(narrowSpread < defaultSpread);
    }

    [Theory]
    [MemberData(nameof(ModelTypes))]
    public void Rate_NullWeightBounds_DisablesNormalization(Type modelType)
    {
        IOpenSkillModel model = (IOpenSkillModel)Activator.CreateInstance(modelType)!;
        model.WeightBounds = null;

        List<ITeam> result = model.Rate(
                model.MockTeams(2, 3),
                [1, 2],
                weights: [[1.0, 1.0, 1.0], [1.0, 1.0, 1.0]]
            )
            .ToList();
        List<double> muChanges = result[0].Players.Select(p => p.Mu - 25).ToList();

        Assert.Null(model.WeightBounds);
        Assert.Equal(muChanges[0], muChanges[1], 1e-6);
        Assert.Equal(muChanges[1], muChanges[2], 1e-6);
    }

    [Theory]
    [MemberData(nameof(TieTestData))]
    public void Rate_Ties(
        Type modelType,
        double tieScore,
        int nTeams,
        int nPlayers,
        string tieType
    )
    {
        IOpenSkillModel model = (IOpenSkillModel)Activator.CreateInstance(modelType)!;
        IList<ITeam> originalTeams = model.MockTeams(nTeams, nPlayers);
        List<double> comparer = Enumerable.Range(0, nTeams).Select(_ => tieScore).ToList();

        List<ITeam> resultTeams = tieType == "score"
            ? model.Rate(originalTeams, scores: comparer).ToList()
            : model.Rate(originalTeams, comparer).ToList();

        List<(IRating playerBefore, IRating playerAfter)> result = originalTeams
            .SelectMany(t => t.Players)
            .Zip(resultTeams.SelectMany(t => t.Players), (playerBefore, playerAfter) => (playerBefore, playerAfter))
            .ToList();

        Assert.All(
            result,
            p =>
            {
                (IRating playerBefore, IRating playerAfter) = p;

                // * All players should end with equal mu
                Assert.Equal(playerBefore.Mu, playerAfter.Mu);
                // * All players should end with lower or equal sigma
                Assert.True(playerAfter.Sigma <= playerBefore.Sigma);
            }
        );
    }

    [Theory]
    [MemberData(nameof(ModelTypes))]
    public void Rate_Ties_CloseRatings(Type modelType)
    {
        IOpenSkillModel model = (IOpenSkillModel)Activator.CreateInstance(modelType)!;

        List<ITeam> result = model.Rate(
                [new Team { Players = [model.Rating(30)] }, new Team { Players = [model.Rating(20)] }],
                [0, 0]
            )
            .ToList();

        // * Ratings should converge on ties
        Assert.True(result[0].Players.First().Mu < 30);
        Assert.True(result[1].Players.First().Mu > 20);
    }
    
    [Fact]
    public void CalculateTeamSqrtSigma()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
                [model.MockSingleTeam(1), model.MockSingleTeam(2)]
            )
            .ToList();

        double teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);

        Assert.Equal(15.590239, teamSqrtSigma, 0.000001);
    }

    [Fact]
    public void CalculateTeamSqrtSigma_5v5()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(model.MockTeams(2, 5)).ToList();

        double teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);

        Assert.Equal(27.003, teamSqrtSigma, 0.001);
    }

    [Fact]
    public void CalculateSumQ()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
                [model.MockSingleTeam(1), model.MockSingleTeam(2)]
            )
            .ToList();
        double teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);

        IEnumerable<double> sumQ = model.CalculateSumQ(teamRatings, teamSqrtSigma);

        Assert.Equal([29.67892702634643, 24.70819334370875], sumQ);
    }

    [Fact]
    public void CalculateSumQ_5v5()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(model.MockTeams(2, 5)).ToList();
        double teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);

        List<double> sumQ = model.CalculateSumQ(teamRatings, teamSqrtSigma).ToList();

        Assert.Equal(204.8437881, sumQ[0], 0.0001);
        Assert.Equal(102.421894, sumQ[1], 0.0001);
    }

    [Theory]
    [InlineData(2, 2, 3, 4, 0, 1)]
    [InlineData(2, 2, 3, 16, 0, 2)]
    [InlineData(2, 2, 3, 64, 1, 4)]
    public void CalculateGamma(
        double c,
        double k,
        double mu,
        double sigmaSq,
        double qRank,
        double expected
    )
    {
        PlackettLuce model = new();

        double gamma = model.Gamma(
            c,
            k,
            mu,
            sigmaSq,
            model.MockSingleTeam(5).Players,
            qRank,
            null
        );

        Assert.Equal(expected, gamma);
    }
}