using OpenSkillSharp.Rating;
using OpenSkillSharp.Util;

namespace OpenSkillSharp;

/// <summary>
/// Interfaces an OpenSkill model.
/// </summary>
public interface IOpenSkillModel
{
    /// <summary>
    /// Represents the initial belief about the skill of a player before any matches have been played.
    /// Known mostly as the mean of the Gaussian prior distribution.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.Mu"/>.
    /// </remarks>
    double Mu { get; set; }

    /// <summary>
    /// Standard deviation of the prior distribution of the player.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.Sigma"/>.
    /// </remarks>
    double Sigma { get; set; }

    /// <summary>
    /// Hyperparameter that determines the level of uncertainty or variability present in the prior
    /// distribution of ratings.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.Beta"/>.
    /// </remarks>
    double Beta { get; set; }

    /// <summary>
    /// The value of <see cref="Beta"/> squared.
    /// </summary>
    double BetaSq { get; }

    /// <summary>
    /// Arbitrary, small, positive, real number that is used to prevent the variance of the posterior distribution
    /// from becoming too small or negative. It can also be thought of as a regularization parameter.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.Kappa"/>.
    /// </remarks>
    double Kappa { get; set; }

    /// <summary>
    /// Delegate function used as the formula to calculate gamma.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.Gamma"/>.
    /// </remarks>
    GammaFactory Gamma { get; set; }

    /// <summary>
    /// Additive dynamics parameter that prevents sigma from getting too small to increase rating change volatility.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.Tau"/>.
    /// </remarks>
    double Tau { get; set; }

    /// <summary>
    /// The margin of victory needed for a win to be considered impressive.
    /// </summary>
    /// <remarks>
    /// Default value of 0.
    /// </remarks>
    double Margin { get; set; }

    /// <summary>
    /// Determines whether to restrict the value of sigma from increasing.
    /// </summary>
    bool LimitSigma { get; set; }

    /// <summary>
    /// Determines whether to emphasize rating outliers.
    /// </summary>
    bool Balance { get; set; }

    /// <summary>
    /// Tuple of (min, max) bounds for normalizing player weights within a team. Weights are scaled to this range.
    /// </summary>
    /// <remarks>
    /// Default value supplied from <see cref="ModelDefaults.WeightBounds"/>.
    /// Can be set to <see langword="null"/> to disable weight normalization.
    /// </remarks>
    (double Min, double Max)? WeightBounds { get; set; }

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
    IRating Rating(double? mu = null, double? sigma = null);

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
    /// <exception cref="ArgumentException">
    /// Thrown for various combinations of argument usage.
    /// <br/>
    /// If the size of <paramref name="teams"/> is less than 2, or if any team contains zero
    /// <see cref="ITeam.Players">Players</see>.
    /// <br/>
    /// If <paramref name="ranks"/> is not <see langword="null"/>, but is not equal in size to <paramref name="teams"/>.
    /// If <paramref name="ranks"/> and <paramref name="scores"/> are both not <see langword="null"/>.
    /// <br/>
    /// If <paramref name="scores"/> is not <see langword="null"/>, but is not equal in size to <paramref name="teams"/>.
    /// <br/>
    /// If <paramref name="weights"/> is not <see langword="null"/>, but is not equal in size to <paramref name="teams"/>,
    /// or each list of weights is not equal in size to the corresponding number of <see cref="ITeam.Players">Players</see>.
    /// </exception>
    IEnumerable<ITeam> Rate(
        IList<ITeam> teams,
        IList<double>? ranks = null,
        IList<double>? scores = null,
        IList<IList<double>>? weights = null,
        double? tau = null
    );

    /// <summary>
    /// Predict the likelihood of each team to win a match against teams of one or more players.
    /// </summary>
    /// <param name="teams">A list of two or more teams.</param>
    /// <returns>A list of numbers representing the odds of each team winning.</returns>
    /// <exception cref="ArgumentException">
    /// If the size of <paramref name="teams"/> is less than 2, or if any team contains zero
    /// <see cref="ITeam.Players">Players</see>.
    /// </exception>
    IEnumerable<double> PredictWin(IList<ITeam> teams);

    /// <summary>
    /// Predict how likely a match of teams of one or more players will conclude in a draw.
    /// </summary>
    /// <remarks>
    /// This measure is also commonly referred to as "match quality", as the lower the probability of a draw is in
    /// a game, the more "one-sided" it is considered.
    /// </remarks>
    /// <param name="teams">A list of two or more teams.</param>
    /// <returns>A number representing the odds of a draw as a percentage from 0.0 to 1.0</returns>
    /// <exception cref="ArgumentException">
    /// If the size of <paramref name="teams"/> is less than 2, or if any team contains zero
    /// <see cref="ITeam.Players">Players</see>.
    /// </exception>
    double PredictDraw(IList<ITeam> teams);

    /// <summary>
    /// Predict the shape of a match outcome.
    /// </summary>
    /// <param name="teams">A list of two or more teams.</param>
    /// <returns>A list of tuples containing a team's predicted rank and the probability for each given team.</returns>
    /// <exception cref="ArgumentException">
    /// If the size of <paramref name="teams"/> is less than 2, or if any team contains zero
    /// <see cref="ITeam.Players">Players</see>.
    /// </exception>
    List<(int rank, double probability)> PredictRank(IList<ITeam> teams);
}