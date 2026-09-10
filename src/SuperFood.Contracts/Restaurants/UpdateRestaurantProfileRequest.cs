namespace SuperFood.Contracts.Restaurants;

public record UpdateRestaurantProfileRequest(
    string Name,
    string? LogoUrl,
    string? Address,
    string? ContactPhone,
    string? ContactEmail);
