using System.ComponentModel.DataAnnotations;
using Terrarium.Configuration;
using Xunit;

namespace Terrarium.Configuration.Tests;

/// <summary>
/// Tests that validation annotations on GameSettings catch invalid values.
/// Uses DataAnnotations validation (same system used by IOptions ValidateDataAnnotations).
/// </summary>
public class ValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Default_GameSettings_Pass_Validation()
    {
        var settings = new GameSettings();
        var results = ValidateModel(settings);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(49)]   // Below minimum (50)
    [InlineData(201)]  // Above maximum (200)
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1000)]
    public void CpuThrottle_Out_Of_Range_Fails_Validation(int value)
    {
        var settings = new GameSettings { CpuThrottle = value };

        var results = ValidateModel(settings);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(GameSettings.CpuThrottle)));
    }

    [Theory]
    [InlineData(50)]   // Minimum
    [InlineData(100)]  // Default
    [InlineData(200)]  // Maximum
    [InlineData(75)]
    [InlineData(150)]
    public void CpuThrottle_Within_Range_Passes_Validation(int value)
    {
        var settings = new GameSettings { CpuThrottle = value };

        var results = ValidateModel(settings);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(GameSettings.CpuThrottle)));
    }

    [Fact]
    public void CpuThrottle_At_Boundary_50_Passes()
    {
        var settings = new GameSettings { CpuThrottle = 50 };
        var results = ValidateModel(settings);
        Assert.Empty(results);
    }

    [Fact]
    public void CpuThrottle_At_Boundary_200_Passes()
    {
        var settings = new GameSettings { CpuThrottle = 200 };
        var results = ValidateModel(settings);
        Assert.Empty(results);
    }
}
