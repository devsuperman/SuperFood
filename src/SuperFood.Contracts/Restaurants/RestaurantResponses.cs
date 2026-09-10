namespace SuperFood.Contracts.Restaurants;

public record RestaurantSummaryResponse(Guid Id, string Name, string Status, DateTimeOffset CreatedAt);

public record RestaurantDetailsResponse(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string? LogoUrl,
    bool DineInEnabled,
    bool DeliveryEnabled,
    int StaffCount);
