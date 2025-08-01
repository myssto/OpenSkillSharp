using OpenSkillSharp.Domain.Rating;
using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Tests.Util;

public static class TestDataExtensions
{
    public static IList<ITeam> MockTeams(this IOpenSkillModel model, IList<ITeam> teams) =>
        teams.Select(t => new Team
        {
            Players = t.Players.Select(_ => model.Rating())
        }).Cast<ITeam>().ToList();
}