using OpenSkillSharp.Rating;

namespace OpenSkillSharp.Tests.TestingUtil;

public static class TestDataExtensions
{
    /// <summary>
    /// Create a list of teams, filling the players with default ratings from the model.
    /// </summary>
    /// <param name="model">Openskill model.</param>
    /// <param name="teams">A list of teams used for size reference.</param>
    /// <returns>A new list of teams containing default ratings.</returns>
    public static IList<ITeam> MockTeams(this IOpenSkillModel model, IList<ITeam> teams)
    {
        return teams.Select(t => new Team { Players = t.Players.Select(_ => model.Rating()) }).Cast<ITeam>().ToList();
    }

    /// <summary>
    /// Creates a team, filling the players with default ratings from the model.
    /// </summary>
    /// <param name="model">Openskill model.</param>
    /// <param name="players">Number of players.</param>
    /// <returns>A team containing default ratings.</returns>
    public static ITeam MockSingleTeam(this IOpenSkillModel model, int players)
    {
        return new Team { Players = Enumerable.Range(1, players).Select(_ => model.Rating()).ToArray() };
    }

    /// <summary>
    /// Create a list of teams, filling the players with default ratings from the model.
    /// </summary>
    /// <param name="model">Openskill model.</param>
    /// <param name="teams">Number of teams.</param>
    /// <param name="players">Number of players per team.</param>
    /// <returns></returns>
    public static IList<ITeam> MockTeams(this IOpenSkillModel model, int teams, int players)
    {
        return Enumerable.Range(1, teams).Select(_ => model.MockSingleTeam(players)).ToList();
    }
}