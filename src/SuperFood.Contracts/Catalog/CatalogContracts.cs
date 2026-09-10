namespace SuperFood.Contracts.Catalog;

public record PublicVariationDto(Guid Id, string Name, decimal PriceAdjustment);

public record PublicProductDto(
    Guid Id, string Name, string? Description, decimal Price, string? PhotoUrl, bool IsAvailable,
    List<PublicVariationDto> Variations);

public record PublicCategoryDto(Guid Id, string Name, List<PublicProductDto> Products);

public record PublicRestaurantInfoDto(
    Guid Id, string Name, string? LogoUrl, bool DineInEnabled, bool DeliveryEnabled, bool IsOpenNow);

public record PublicMenuResponse(PublicRestaurantInfoDto Restaurant, List<PublicCategoryDto> Categories);

public record PublicTableInfoResponse(PublicRestaurantInfoDto Restaurant, string TableIdentifier);
