using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Tests.TestingUtil;

namespace OpenSkillSharp.Tests.Models;

public class BradleyTerryPartTests : OpenSkillModelTests<BradleyTerryPart>
{
    [Fact]
    public void Rate_WindowSize0_Tau0()
    {
        // Arrange
        BradleyTerryPart windowTauModel = new()
        {
            Mu = _testData.Model.Mu, Sigma = _testData.Model.Sigma, Tau = 0, WindowSize = 0
        };
        IRating playerA = windowTauModel.Rating();
        IRating playerB = windowTauModel.Rating();

        // Act
        List<ITeam> results = windowTauModel.Rate(
                [new Team { Players = [playerA] }, new Team { Players = [playerB] }]
            )
            .ToList();

        // Assert
        Assertions.RatingsEqual(playerA, results.ElementAt(0).Players.ElementAt(0));
        Assertions.RatingsEqual(playerB, results.ElementAt(1).Players.ElementAt(0));
    }
}