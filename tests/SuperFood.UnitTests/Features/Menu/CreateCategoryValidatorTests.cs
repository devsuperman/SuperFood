using FluentAssertions;
using SuperFood.Api.Features.Menu.Categories;
using Xunit;

namespace SuperFood.UnitTests.Features.Menu;

/// <summary>Verifies US-0401's validation rules.</summary>
public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Rejects_empty_name()
    {
        var result = _validator.Validate(new CreateCategoryCommand(Guid.NewGuid(), ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryCommand.Name));
    }

    [Fact]
    public void Rejects_name_over_150_characters()
    {
        var result = _validator.Validate(new CreateCategoryCommand(Guid.NewGuid(), new string('a', 151)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_a_valid_name()
    {
        var result = _validator.Validate(new CreateCategoryCommand(Guid.NewGuid(), "Pizzas"));

        result.IsValid.Should().BeTrue();
    }
}
