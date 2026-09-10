using FluentAssertions;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Api.Features.Menu.Products;
using SuperFood.Domain.Entities;
using SuperFood.UnitTests.TestSupport;
using Xunit;

namespace SuperFood.UnitTests.Features.Menu;

/// <summary>Verifies US-0501 product creation, including automatic sort-order assignment.</summary>
public class CreateProductHandlerTests
{
    [Fact]
    public async Task Assigns_incrementing_sort_order_within_a_category()
    {
        using var factory = new SqliteDbContextFactory();
        await using var db = factory.CreateContext();

        var restaurant = new Restaurant { Id = Guid.NewGuid(), Name = "Pizza Palace" };
        var category = new Category { Id = Guid.NewGuid(), RestaurantId = restaurant.Id, Name = "Pizzas" };
        db.Restaurants.Add(restaurant);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var restaurantId = restaurant.Id;

        var handler = new CreateProductHandler(db);

        var first = await handler.Handle(
            new CreateProductCommand(restaurantId, category.Id, "Margherita", null, 10m, null, []), default);
        var second = await handler.Handle(
            new CreateProductCommand(restaurantId, category.Id, "Diavola", null, 12m, null, []), default);

        first.SortOrder.Should().Be(0);
        second.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_category()
    {
        using var factory = new SqliteDbContextFactory();
        await using var db = factory.CreateContext();
        var handler = new CreateProductHandler(db);

        var act = () => handler.Handle(
            new CreateProductCommand(Guid.NewGuid(), Guid.NewGuid(), "Margherita", null, 10m, null, []), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
