namespace SuperFood.Contracts.Menu;

public record ProductVariationDto(Guid? Id, string Name, decimal PriceAdjustment);

public record ProductResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? PhotoUrl,
    bool IsAvailable,
    bool IsActive,
    int SortOrder,
    List<ProductVariationDto> Variations);

public record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? PhotoUrl,
    List<ProductVariationDto> Variations);

public record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? PhotoUrl,
    List<ProductVariationDto> Variations);

public record ReorderProductsRequest(List<Guid> OrderedProductIds);

public record SetProductAvailabilityRequest(bool IsAvailable);

public record SetProductActiveRequest(bool IsActive);
