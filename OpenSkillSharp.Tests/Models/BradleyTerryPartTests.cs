using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Tests.Models.Data;
using OpenSkillSharp.Tests.Util;

namespace OpenSkillSharp.Tests.Models;

public class BradleyTerryPartTests
{
    private readonly ModelTestData _testData = ModelTestData.FromJson("bradleyterrypart");
    private BradleyTerryPart TestModel => new() { Mu = _testData.Model.Mu, Sigma = _testData.Model.Sigma };
    
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
        // Arrange
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
        // Arrange
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
        // Arrange
        var expectedRatings = _testData.Margins;
        var marginTestModel = new BradleyTerryPart
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
        // Arrange
        var expectedRatings = _testData.LimitSigma;
        var limitSigmaTestModel = new BradleyTerryPart
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
        // Arrange
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
        // Arrange
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
        // Arrange
        var expectedRatings = _testData.Balance;
        var balanceModel = new BradleyTerryPart
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
    public void Rate_WindowSize0_Tau0()
    {
        // Arrange
        var windowTauModel = new BradleyTerryPart
        {
            Mu = _testData.Model.Mu,
            Sigma = _testData.Model.Sigma,
            Tau = 0,
            WindowSize = 0
        };
        var playerA = windowTauModel.Rating();
        var playerB = windowTauModel.Rating();
        
        // Act
        var results = windowTauModel.Rate(
            [
                new Team { Players = [playerA] },
                new Team { Players = [playerB] }
            ]
        ).ToList();
        
        // Assert
        Assertions.RatingsEqual(playerA, results.ElementAt(0).Players.ElementAt(0));
        Assertions.RatingsEqual(playerB, results.ElementAt(1).Players.ElementAt(0));
    }
}