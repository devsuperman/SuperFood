namespace SuperFood.Contracts.Menu;

public record CategoryResponse(Guid Id, string Name, int SortOrder, bool IsVisible);

public record CreateCategoryRequest(string Name);

public record UpdateCategoryRequest(string Name);

public record ReorderCategoriesRequest(List<Guid> OrderedCategoryIds);

public record SetCategoryVisibilityRequest(bool IsVisible);
