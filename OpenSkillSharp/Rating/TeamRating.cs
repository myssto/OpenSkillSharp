using OpenSkillSharp.Domain.Rating;

namespace OpenSkillSharp.Rating;

public class TeamRating : Team, ITeamRating
{
    public double Mu { get; set; }
    
    public double SigmaSq { get; set; }
    
    public int Rank { get; set; }

    public override ITeamRating Clone() => new TeamRating
    {
        Players = base.Clone().Players,
        Mu = Mu,
        SigmaSq = SigmaSq,
        Rank = Rank
    };
}