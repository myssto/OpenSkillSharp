using OpenSkillSharp.Domain.Rating;

namespace OpenSkillSharp.Models;

public interface IOpenSkillModel
{
    /// <summary>
    /// Creates a new rating object with the configured defaults for this model. The given parameters can
    /// override the defaults for this model, but it is not recommended unless you know what you are doing.
    /// </summary>
    /// <param name="mu">
    /// Represents the initial belief about the skill of a player before any matches have been played.
    /// Known mostly as the mean of the Gaussian prior distribution.
    /// </param>
    /// <param name="sigma">
    /// Standard deviation of the prior distribution of the player.
    /// </param>
    /// <returns>A new rating object.</returns>
    public IRating Rating(double? mu = null, double? sigma = null);
    
    /// <summary>
    /// Calculate the new ratings based on the given teams and parameters.
    /// </summary>
    /// <param name="teams">A list of teams.</param>
    /// <param name="ranks">
    /// A list of numbers corresponding to the given <paramref name="teams"/> where lower values represent winners.
    /// </param>
    /// <param name="scores">
    /// A list of numbers corresponding to the given <paramref name="teams"/> where higher values represent winners.
    /// </param>
    /// <param name="weights">
    /// A list of lists of numbers corresponding to the given <paramref name="teams"/>
    /// where each inner list represents the contribution of each player to the team's performance.
    /// </param>
    /// <param name="tau">
    /// Additive dynamics parameter that prevents sigma from getting too small to increase rating change volatility.
    /// </param>
    /// <returns>
    /// A list of teams where each team contains a list of updated rating objects.
    /// </returns>
    public IEnumerable<ITeam> Rate(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null,
        double? tau = null
    );
}