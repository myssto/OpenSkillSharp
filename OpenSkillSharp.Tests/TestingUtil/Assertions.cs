using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Tests.TestingUtil;

public static class Assertions
{
    public static void RatingResultsEqual(IEnumerable<ITeam> expected, IEnumerable<ITeam> actual)
    {
        expected
            .SelectMany(t => t.Players)
            .Zip(actual.SelectMany(t => t.Players))
            .ToList()
            .ForEach(r => RatingsEqual(r.First, r.Second));
    }

    public static void RatingsEqual(IRating expected, IRating actual)
    {
        Assert.Equal(expected.Mu, actual.Mu, 0.0001);
        Assert.Equal(expected.Sigma, actual.Sigma, 0.0001);
    }
}