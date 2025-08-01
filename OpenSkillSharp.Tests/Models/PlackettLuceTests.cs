using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Tests.Util;

namespace OpenSkillSharp.Tests.Models;

public class PlackettLuceTests
{
    private readonly ModelTestData _testData = ModelTestData.FromJson("plackettluce");
    private PlackettLuce TestModel => new() { Mu = _testData.Model.Mu, Sigma = _testData.Model.Sigma };

    [Fact]
    public void ModelValues_Defaults()
    {
        var model = new PlackettLuce();
        
        Assert.Equal(25D, model.Mu);
        Assert.Equal(25D / 3, model.Sigma);
        Assert.Equal(25D / 6, model.Beta);
        Assert.Equal(0.0001, model.Kappa);
        Assert.Equal(25D / 300, model.Tau);
        Assert.False(model.LimitSigma);
        Assert.False(model.Balance);
    }

    [Fact]
    public void CalculateTeamSqrtSigma()
    {
        var model = new PlackettLuce();
        var teamRatings = model.CalculateTeamRatings(
            [
                new Team
                {
                    Players = [model.Rating()]
                },
                new Team
                {
                    Players = [model.Rating(), model.Rating()]
                }
            ]
        ).ToList();
        
        var teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);
        
        Assert.Equal(15.590239, teamSqrtSigma, 0.000001);
    }
    
    [Fact]
    public void CalculateTeamSqrtSigma_5v5()
    {
        var model = new PlackettLuce();
        var teamRatings = model.CalculateTeamRatings(
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
        ).ToList();
        
        var teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);
        
        Assert.Equal(27.003, teamSqrtSigma, 0.001);
    }

    [Fact]
    public void CalculateSumQ()
    {
        var model = new PlackettLuce();
        var teamRatings = model.CalculateTeamRatings(
            [
                new Team
                {
                    Players = [model.Rating()]
                },
                new Team
                {
                    Players = [model.Rating(), model.Rating()]
                }
            ]
        ).ToList();
        var teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);
        
        var sumQ = model.CalculateSumQ(teamRatings, teamSqrtSigma);
        
        Assert.Equal([29.67892702634643, 24.70819334370875], sumQ);
    }
    
    [Fact]
    public void CalculateSumQ_5v5()
    {
        var model = new PlackettLuce();
        var teamRatings = model.CalculateTeamRatings(
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
        ).ToList();
        var teamSqrtSigma = model.CalculateTeamSqrtSigma(teamRatings);
        
        var sumQ = model.CalculateSumQ(teamRatings, teamSqrtSigma).ToList();
        
        Assert.Equal(204.8437881, sumQ[0], Constants.DoubleTolerance);
        Assert.Equal(102.421894, sumQ[1], Constants.DoubleTolerance);
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
        var model = new PlackettLuce();
        
        var gamma = model.Gamma(
            c, 
            k, 
            mu, 
            sigmaSq,
            [model.Rating(), model.Rating(), model.Rating(), model.Rating(),model.Rating()],
            qRank,
            null
        );
        
        Assert.Equal(expected, gamma);
    }
    
    [Fact]
    public void Rate_Normal()
    {
        // Arrange
        var expectedRatings = _testData.Normal;
        var teams = TestModel.MockTeams(expectedRatings);
        
        // Act
        var results = TestModel.Rate(teams);
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }

    [Fact]
    public void Rate_Ranks()
    {
        var expectedRatings = _testData.Ranks;
        var teams = TestModel.MockTeams(expectedRatings);
        
        // Act
        var results = TestModel.Rate(
            teams, 
            ranks: [2, 1, 4, 3]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }
    
    [Fact]
    public void Rate_Scores()
    {
        var expectedRatings = _testData.Scores;
        var teams = TestModel.MockTeams(expectedRatings);
        
        // Act
        var results = TestModel.Rate(
            teams, 
            scores: [1, 2]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }
    
    [Fact]
    public void Rate_Margins()
    {
        var expectedRatings = _testData.Margins;
        var marginTestModel = new PlackettLuce
        {
            Mu = _testData.Model.Mu,
            Sigma = _testData.Model.Sigma,
            Margin = 2D
        };
        var teams = marginTestModel.MockTeams(expectedRatings);
        
        // Act
        var results = marginTestModel.Rate(
            teams,
            scores: [10, 5, 5, 2, 1],
            weights: [[1, 2], [2, 1], [1, 2], [3, 1], [1, 2]]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }
    
    [Fact]
    public void Rate_LimitSigma()
    {
        var expectedRatings = _testData.LimitSigma;
        var limitSigmaTestModel = new PlackettLuce
        {
            Mu = _testData.Model.Mu,
            Sigma = _testData.Model.Sigma,
            LimitSigma = true
        };
        var teams = limitSigmaTestModel.MockTeams(expectedRatings);
        
        // Act
        var results = limitSigmaTestModel.Rate(
            teams,
            ranks: [2, 1, 3]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }
    
    [Fact]
    public void Rate_Ties()
    {
        var expectedRatings = _testData.Ties;
        var teams = TestModel.MockTeams(expectedRatings);
        
        // Act
        var results = TestModel.Rate(
            teams,
            ranks: [1, 2, 1]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }
    
    [Fact]
    public void Rate_Weights()
    {
        var expectedRatings = _testData.Weights;
        var teams = TestModel.MockTeams(expectedRatings);
        
        // Act
        var results = TestModel.Rate(
            teams,
            ranks: [2, 1, 4, 3],
            weights: [[2, 0, 0], [1, 2], [0, 0, 1], [0, 1]]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }
    
    [Fact]
    public void Rate_Balance()
    {
        var expectedRatings = _testData.Balance;
        var balanceModel = new PlackettLuce
        {
            Mu = _testData.Model.Mu,
            Sigma = _testData.Model.Sigma,
            Balance = true
        };
        var teams = balanceModel.MockTeams(expectedRatings);
        
        // Act
        var results = balanceModel.Rate(
            teams,
            ranks: [1, 2]
        );
        
        // Assert
        Assertions.RatingResultsEqual(expectedRatings, results);
    }

    [Fact]
    public void PredictWin()
    {
        var model = new PlackettLuce();
        var teamOne = new Team
        {
            Players =
            [
                model.Rating(),
                model.Rating(32.444, 5.123)
            ]
        };
        var teamTwo = new Team
        {
            Players =
            [
                model.Rating(73.381, 1.421),
                model.Rating(25.188, 6.211)
            ]
        };

        var probabilities = model.PredictWin([teamOne, teamTwo]);
        
        Assert.Equal(1, probabilities.Sum(), 0.0001);
    }

    [Fact]
    public void PredictWin_5Teams()
    {
        var model = new PlackettLuce();
        var a1 = model.Rating();
        var a2 = model.Rating(32.444, 5.123);
        var b1 = model.Rating(73.381, 1.421);
        var b2 = model.Rating(25.188, 6.211);

        var probabilities = model.PredictWin([
            new Team { Players = [a1, a2] },
            new Team { Players = [b1, b2] },
            new Team { Players = [a2] },
            new Team { Players = [a1] },
            new Team { Players = [b1] }
        ]);
        
        Assert.Equal(1, probabilities.Sum(), 0.0001);
    }

    [Fact]
    public void PredictDraw()
    {
        var model = new PlackettLuce();

        var probability = model.PredictDraw([
            new Team { Players = [model.Rating(25, 1), model.Rating(25, 1)] },
            new Team { Players = [model.Rating(25, 1), model.Rating(25, 1)] }
        ]);
        
        Assert.Equal(0.2433180271619435, probability, 0.0000001);
    }

    [Fact]
    public void PredictDraw_ProducesLowProbability_GivenUnevenTeams()
    {
        var model = new PlackettLuce();

        var probability = model.PredictDraw([
            new Team { Players = [model.Rating(35, 1), model.Rating(35, 1)] },
            new Team { Players = [model.Rating(35, 1), model.Rating(35, 1), model.Rating(35, 1)] }
        ]);
        
        Assert.Equal(0.0002807397636509501, probability, 0.0000001);
    }
}