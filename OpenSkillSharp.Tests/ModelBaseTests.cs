using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;

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
    /// The one exception to the above remarks applies to testing the behavior of weight bounds. Even though the
    /// <see cref="OpenSkillModelBase.Rate"/> method is concrete, the normalized weights have unique usage
    /// across implementations of <see cref="OpenSkillModelBase.Compute"/>. That being said, the test setup
    /// does not change across implementations and should yield the same result, so using a generic matrix
    /// of the model types ensures parity.
    /// </summary>
    public static TheoryData<Type> ModelTypes => new()
    {
        typeof(BradleyTerryFull),
        typeof(BradleyTerryPart),
        typeof(PlackettLuce),
        typeof(ThurstoneMostellerFull),
        typeof(ThurstoneMostellerPart)
    };

    [Fact]
    public void ModelValues_Defaults()
    {
        PlackettLuce model = new();

        Assert.Equal(25D, model.Mu);
        Assert.Equal(25D / 3, model.Sigma);
        Assert.Equal(25D / 6, model.Beta);
        Assert.Equal(0.0001, model.Kappa);
        Assert.Equal(25D / 300, model.Tau);
        Assert.False(model.LimitSigma);
        Assert.False(model.Balance);
        Assert.NotNull(model.WeightBounds);
        (double min, double max) = model.WeightBounds;
        Assert.Equal(1.0, min);
        Assert.Equal(2.0, max);
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
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                [1]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                [1, 2, 3]
            )
        );

        // Scores size mismatch
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                scores: [1]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                scores: [1, 2, 3]
            )
        );

        // Ranks and scores together
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                [1, 2],
                [1, 2]
            )
        );

        // Weights size mismatch
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                weights: [[1]]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
                weights: [[1], [2], [3]]
            )
        );
        Assert.Throws<ArgumentException>(() => model.Rate(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating()] }],
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
        modelNarrow.WeightBounds = new Tuple<double, double>(0.9, 1.1);
        IList<IList<double>> weights = [[0.5, 1.0, 2.0], [1.0, 1.0, 1.0]];

        List<ITeam> resultDefault = modelDefault.Rate(
                [
                    new Team { Players = [modelDefault.Rating(), modelDefault.Rating(), modelDefault.Rating()] },
                    new Team { Players = [modelDefault.Rating(), modelDefault.Rating(), modelDefault.Rating()] }
                ],
                [1, 2],
                weights: weights
            )
            .ToList();

        List<ITeam> resultNarrow = modelNarrow.Rate(
                [
                    new Team { Players = [modelNarrow.Rating(), modelNarrow.Rating(), modelNarrow.Rating()] },
                    new Team { Players = [modelNarrow.Rating(), modelNarrow.Rating(), modelNarrow.Rating()] }
                ],
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
                [
                    new Team { Players = [model.Rating(), model.Rating(), model.Rating()] },
                    new Team { Players = [model.Rating(), model.Rating(), model.Rating()] }
                ],
                [1, 2],
                weights: [[1.0, 1.0, 1.0], [1.0, 1.0, 1.0]]
            )
            .ToList();
        List<double> muChanges = result[0].Players.Select(p => p.Mu - 25).ToList();

        Assert.Null(model.WeightBounds);
        Assert.Equal(muChanges[0], muChanges[1], 1e-6);
        Assert.Equal(muChanges[1], muChanges[2], 1e-6);
    }

    [Fact]
    public void CalculateTeamSqrtSigma()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating(), model.Rating()] }]
            )
            .ToList();

        double teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);

        Assert.Equal(15.590239, teamSqrtSigma, 0.000001);
    }

    [Fact]
    public void CalculateTeamSqrtSigma_5v5()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
                [
                    new Team
                    {
                        Players = [model.Rating(), model.Rating(), model.Rating(), model.Rating(), model.Rating()]
                    },
                    new Team
                    {
                        Players = [model.Rating(), model.Rating(), model.Rating(), model.Rating(), model.Rating()]
                    }
                ]
            )
            .ToList();

        double teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);

        Assert.Equal(27.003, teamSqrtSigma, 0.001);
    }

    [Fact]
    public void CalculateSumQ()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
                [new Team { Players = [model.Rating()] }, new Team { Players = [model.Rating(), model.Rating()] }]
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
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
                [
                    new Team
                    {
                        Players = [model.Rating(), model.Rating(), model.Rating(), model.Rating(), model.Rating()]
                    },
                    new Team
                    {
                        Players = [model.Rating(), model.Rating(), model.Rating(), model.Rating(), model.Rating()]
                    }
                ]
            )
            .ToList();
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
            [model.Rating(), model.Rating(), model.Rating(), model.Rating(), model.Rating()],
            qRank,
            null
        );

        Assert.Equal(expected, gamma);
    }
}