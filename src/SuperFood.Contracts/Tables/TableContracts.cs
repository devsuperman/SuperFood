namespace SuperFood.Contracts.Tables;

public record CreateTableRequest(string Identifier, int Capacity);

public record TableResponse(Guid Id, string Identifier, int Capacity, string Status, string QrCodeToken, string QrCodeUrl);

public record OpenTableSessionResponse(Guid SessionId, DateTimeOffset OpenedAt);
