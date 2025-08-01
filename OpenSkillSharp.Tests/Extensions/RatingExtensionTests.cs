using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp.Tests.Extensions;

public class RatingExtensionTests
{
    [Fact]
    public void CountRankOccurrences()
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

        var rankOccurrences = teamRatings.CountRankOccurrences().ToList();
        
        Assert.Equal([1, 1], rankOccurrences);
    }
    
    [Fact]
    public void CountRankOccurrences_1TeamPerRank()
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
                },
                new Team
                {
                    Players = [model.Rating(), model.Rating()]
                },
                new Team
                {
                    Players = [model.Rating()]
                }
            ]
        ).ToList();

        var rankOccurrences = teamRatings.CountRankOccurrences().ToList();
        
        Assert.Equal([1, 1, 1, 1], rankOccurrences);
    }
    
    [Fact]
    public void CountRankOccurrences_SharedRanks()
    {
        var model = new PlackettLuce();
        var teamRatings = model.CalculateTeamRatings(
            teams: [
                new Team
                {
                    Players = [model.Rating()]
                },
                new Team
                {
                    Players = [model.Rating(), model.Rating()]
                },
                new Team
                {
                    Players = [model.Rating(), model.Rating()]
                },
                new Team
                {
                    Players = [model.Rating()]
                }
            ],
            ranks: [1, 1, 1, 4]
        ).ToList();

        var rankOccurrences = teamRatings.CountRankOccurrences().ToList();
        
        Assert.Equal([3, 3, 3, 1], rankOccurrences);
    }
}