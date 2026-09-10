namespace SuperFood.Contracts.Restaurants;

public record CreateRestaurantRequest(
    string Name,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string OwnerFullName,
    string OwnerEmail);

public record CreateRestaurantResponse(Guid RestaurantId, Guid OwnerUserId, string OwnerEmail, string TemporaryPassword);
