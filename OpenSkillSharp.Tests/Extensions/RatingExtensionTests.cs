using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp.Tests.Extensions;

public class RatingExtensionTests
{
    [Fact]
    public void CountRankOccurrences()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
            [
                new Team { Players = [model.Rating()] },
                new Team { Players = [model.Rating(), model.Rating()] }
            ]
        ).ToList();

        List<int> rankOccurrences = teamRatings.CountRankOccurrences().ToList();

        Assert.Equal([1, 1], rankOccurrences);
    }

    [Fact]
    public void CountRankOccurrences_1TeamPerRank()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
            [
                new Team { Players = [model.Rating()] },
                new Team { Players = [model.Rating(), model.Rating()] },
                new Team { Players = [model.Rating(), model.Rating()] },
                new Team { Players = [model.Rating()] }
            ]
        ).ToList();

        List<int> rankOccurrences = teamRatings.CountRankOccurrences().ToList();

        Assert.Equal([1, 1, 1, 1], rankOccurrences);
    }

    [Fact]
    public void CountRankOccurrences_SharedRanks()
    {
        PlackettLuce model = new();
        List<ITeamRating> teamRatings = model.CalculateTeamRatings(
            [
                new Team { Players = [model.Rating()] },
                new Team { Players = [model.Rating(), model.Rating()] },
                new Team { Players = [model.Rating(), model.Rating()] },
                new Team { Players = [model.Rating()] }
            ],
            [1, 1, 1, 4]
        ).ToList();

        List<int> rankOccurrences = teamRatings.CountRankOccurrences().ToList();

        Assert.Equal([3, 3, 3, 1], rankOccurrences);
    }
}