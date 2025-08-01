using OpenSkillSharp.Domain.Rating;

namespace OpenSkillSharp.Util;

public static class RatingExtensions
{
    public static IEnumerable<double> CalculateRankings(
        this IList<ITeam> game,
        IList<double>? ranks = null
    )
    {
        if (!game.Any())
        {
            return new List<double>();
        }
        
        var teamScores = ranks is not null
            ? ranks.Take(game.Count).ToList()
            : Enumerable.Range(0, game.Count).Select(i => (double)i).ToList();

        var rankMap = teamScores
            .OrderBy(s => s)
            .Select((score, idx) => (score, idx))
            .GroupBy(t => t.score)
            .ToDictionary(g => g.Key, g => (double)g.First().idx);

        return teamScores.Select(s => rankMap[s]).ToList();
    }

    public static IEnumerable<int> CountRankOccurrences(this IList<ITeamRating> teamRatings)
    {
        var rankCounts = teamRatings
            .GroupBy(tr => tr.Rank)
            .ToDictionary(g => g.Key, g => g.Count());
        
        return teamRatings.Select(team => rankCounts[team.Rank]);
    }
}