using OpenSkillSharp.Models;
using OpenSkillSharp.Tests.Util;

namespace OpenSkillSharp.Tests.Rating;

public class RatingTests
{
    [Fact]
    public void Values_DefaultConstructor_Defaults()
    {
        var rating = new OpenSkillSharp.Rating.Rating();
        
        Assert.Equal(25D, rating.Mu);
        Assert.Equal(25D / 3, rating.Sigma);
    }
    
    [Fact]
    public void Values_FromModel_Defaults()
    {
        var rating = new PlackettLuce().Rating();
        
        Assert.Equal(25D, rating.Mu);
        Assert.Equal(25D / 3, rating.Sigma);
    }
    
    [Fact]
    public void Values_ReflectModelOverrides()
    {
        const double overrideMu = 30D;
        const double overrideSigma = 30D / 3;
        
        var model = new PlackettLuce
        {
            Mu = overrideMu,
            Sigma = overrideSigma
        };
        var rating = model.Rating();
        
        Assert.Equal(overrideMu, rating.Mu);
        Assert.Equal(overrideSigma, rating.Sigma);
    }
    
    [Fact]
    public void Values_CanBeOverriddenFromModel()
    {
        const double overrideMu = 30D;
        const double overrideSigma = 30D / 3;
        
        var model = new PlackettLuce();
        var rating = model.Rating(mu: overrideMu, sigma: overrideSigma);
        
        Assert.Equal(overrideMu, rating.Mu);
        Assert.Equal(overrideSigma, rating.Sigma);
        // Passing values to IOpenSkillModel.Rating() should be a pure operation and not modify the model
        Assert.Equal(25D, model.Mu);
        Assert.Equal(25D / 3, model.Sigma);
    }

    [Fact]
    public void Ordinal_UsingGetter_ReturnsCorrectValue()
    {
        var rating = new OpenSkillSharp.Rating.Rating
        {
            Mu = 5,
            Sigma = 2
        };
        
        Assert.Equal(-1D, rating.Ordinal);
    }

    [Fact]
    public void GetOrdinal_GivenAlphaAndTarget_ReturnsCorrectValue()
    {
        var rating = new OpenSkillSharp.Rating.Rating
        {
            Mu = 24,
            Sigma = 6
        };
        
        var result = rating.GetOrdinal(alpha: 24, target: 1500);
        
        Assert.Equal(1644D, result);
    }

    [Fact]
    public void GetOrdinal_GivenZ_ReturnsCorrectValue()
    {
        var rating = new OpenSkillSharp.Rating.Rating
        {
            Mu = 24,
            Sigma = 6
        };
        
        var result = rating.GetOrdinal(z: 2);
        
        Assert.Equal(12D, result);
    }

    [Fact]
    public void Clone_CreatesNewInstance()
    {
        var rating = new OpenSkillSharp.Rating.Rating
        {
            Mu = 25,
            Sigma = 25D / 3
        };
        
        var clone = rating.Clone();
        
        Assertions.RatingsEqual(rating, clone);
        Assert.NotSame(rating, clone);
    }
}