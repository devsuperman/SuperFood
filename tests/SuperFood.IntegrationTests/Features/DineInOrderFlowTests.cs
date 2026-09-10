using System.Net.Http.Json;
using FluentAssertions;
using SuperFood.Contracts.Auth;
using SuperFood.Contracts.Menu;
using SuperFood.Contracts.Orders;
using SuperFood.Contracts.Restaurants;
using SuperFood.Contracts.Tables;
using Xunit;

namespace SuperFood.IntegrationTests.Features;

/// <summary>
/// End-to-end across EPIC-01 (create restaurant), EPIC-04/05 (menu),
/// EPIC-06 (table), and EPIC-07 (customer places a dine-in order via the
/// table's QR token) - the same path exercised manually during development.
/// </summary>
public class DineInOrderFlowTests(SuperFoodApiFactory factory) : IClassFixture<SuperFoodApiFactory>
{
    [Fact]
    public async Task Customer_can_browse_and_order_from_a_freshly_created_restaurant()
    {
        var client = factory.CreateClient();

        var adminLogin = await client.PostAsJsonAsync("api/auth/login",
            new LoginRequest("admin@superfood.dev", "Passw0rd!Admin"));
        adminLogin.EnsureSuccessStatusCode();
        var admin = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", admin!.AccessToken);

        var createRestaurant = await client.PostAsJsonAsync("api/restaurants", new CreateRestaurantRequest(
            "Pizza Palace", "hi@pizza.dev", null, null, "Mario Rossi", $"mario-{Guid.NewGuid():N}@pizza.dev"));
        createRestaurant.EnsureSuccessStatusCode();
        var restaurant = await createRestaurant.Content.ReadFromJsonAsync<CreateRestaurantResponse>();

        var ownerLogin = await client.PostAsJsonAsync("api/auth/login",
            new LoginRequest(restaurant!.OwnerEmail, restaurant.TemporaryPassword));
        ownerLogin.EnsureSuccessStatusCode();
        var owner = await ownerLogin.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", owner!.AccessToken);

        var createCategory = await client.PostAsJsonAsync("api/menu/categories", new CreateCategoryRequest("Pizzas"));
        createCategory.EnsureSuccessStatusCode();
        var category = await createCategory.Content.ReadFromJsonAsync<CategoryResponse>();

        var createProduct = await client.PostAsJsonAsync("api/menu/products", new CreateProductRequest(
            category!.Id, "Margherita", "Classic", 10.5m, null, []));
        createProduct.EnsureSuccessStatusCode();
        var product = await createProduct.Content.ReadFromJsonAsync<ProductResponse>();

        var createTable = await client.PostAsJsonAsync("api/tables", new CreateTableRequest("T1", 4));
        createTable.EnsureSuccessStatusCode();
        var table = await createTable.Content.ReadFromJsonAsync<TableResponse>();

        // The customer flow is anonymous - a fresh client with no auth header.
        var anonymousClient = factory.CreateClient();
        var items = new List<OrderItemInputDto> { new(product!.Id, null, 2, "extra cheese") };
        var placeOrder = await anonymousClient.PostAsJsonAsync(
            $"api/public/restaurants/{restaurant.RestaurantId}/tables/{table!.QrCodeToken}/orders", items);

        placeOrder.EnsureSuccessStatusCode();
        var order = await placeOrder.Content.ReadFromJsonAsync<OrderResponse>();

        order!.Type.Should().Be("DineIn");
        order.Status.Should().Be("Received");
        order.Items.Should().ContainSingle(i => i.ProductName == "Margherita" && i.Quantity == 2);
        order.Total.Should().Be(21.0m);

        var kitchenQueue = await client.GetFromJsonAsync<List<OrderResponse>>("api/kitchen/queue");
        kitchenQueue.Should().ContainSingle(o => o.Id == order.Id);
    }
}
