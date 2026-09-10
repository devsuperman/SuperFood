using FluentAssertions;
using SuperFood.Api.Features.Restaurants;
using Xunit;

namespace SuperFood.UnitTests.Features.Restaurants;

/// <summary>Verifies US-0101's validation rules.</summary>
public class CreateRestaurantValidatorTests
{
    private readonly CreateRestaurantValidator _validator = new();

    [Fact]
    public void Rejects_missing_owner_email()
    {
        var result = _validator.Validate(new CreateRestaurantCommand("Pizza Palace", null, null, null, "Mario", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRestaurantCommand.OwnerEmail));
    }

    [Fact]
    public void Rejects_invalid_owner_email_format()
    {
        var result = _validator.Validate(new CreateRestaurantCommand("Pizza Palace", null, null, null, "Mario", "not-an-email"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_a_valid_command()
    {
        var result = _validator.Validate(
            new CreateRestaurantCommand("Pizza Palace", "hi@pizza.dev", "555-1234", "1 Main St", "Mario", "mario@pizza.dev"));

        result.IsValid.Should().BeTrue();
    }
}
