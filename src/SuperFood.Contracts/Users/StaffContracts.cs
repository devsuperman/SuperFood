namespace SuperFood.Contracts.Users;

public record InviteStaffRequest(string FullName, string Email, Guid RoleId);

public record InviteStaffResponse(Guid UserId, string Email, string TemporaryPassword);

public record StaffSummaryResponse(Guid Id, string FullName, string Email, Guid? RoleId, string? RoleName, bool IsActive);

public record AssignRoleRequest(Guid RoleId);
