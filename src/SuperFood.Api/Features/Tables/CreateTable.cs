using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using SuperFood.Api.Common;
using SuperFood.Contracts.Tables;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Tables;

/// <summary>US-0601/US-0602: create a table; a QR token is generated with it.</summary>
public record CreateTableCommand(Guid RestaurantId, string Identifier, int Capacity) : IRequest<TableResponse>;

public class CreateTableValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacity).GreaterThan(0);
    }
}

public class CreateTableHandler(SuperFoodDbContext db, IConfiguration configuration)
    : IRequestHandler<CreateTableCommand, TableResponse>
{
    public async Task<TableResponse> Handle(CreateTableCommand request, CancellationToken cancellationToken)
    {
        var table = new Table
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            Identifier = request.Identifier,
            Capacity = request.Capacity
        };

        db.Tables.Add(table);
        await db.SaveChangesAsync(cancellationToken);

        return TableEndpointHelpers.ToResponse(table, request.RestaurantId, configuration);
    }
}

public class CreateTableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tables",
            async (CreateTableRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                var result = await sender.Send(new CreateTableCommand(restaurantId, request.Identifier, request.Capacity), ct);
                return Results.Created($"/api/tables/{result.Id}", result);
            })
        .WithName("CreateTable")
        .WithTags("Tables")
        .RequirePermission(Permissions.TablesManage);
    }
}

internal static class TableEndpointHelpers
{
    public static TableResponse ToResponse(Table table, Guid restaurantId, IConfiguration configuration)
    {
        var baseUrl = configuration["Client:PublicBaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var qrCodeUrl = $"{baseUrl}/order/{restaurantId}/table/{table.QrCodeToken}";
        return new TableResponse(table.Id, table.Identifier, table.Capacity, table.Status.ToString(), table.QrCodeToken, qrCodeUrl);
    }
}
