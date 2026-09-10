using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Catalog;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Catalog;

/// <summary>US-0701/US-1002: confirms which table a scanned QR code belongs to.</summary>
public record GetPublicTableInfoQuery(Guid RestaurantId, string QrCodeToken) : IRequest<PublicTableInfoResponse>;

public class GetPublicTableInfoHandler(SuperFoodDbContext db) : IRequestHandler<GetPublicTableInfoQuery, PublicTableInfoResponse>
{
    public async Task<PublicTableInfoResponse> Handle(GetPublicTableInfoQuery request, CancellationToken cancellationToken)
    {
        var table = await db.Tables.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.RestaurantId == request.RestaurantId && t.QrCodeToken == request.QrCodeToken, cancellationToken)
            ?? throw new NotFoundException("Table was not found.");

        var restaurant = await db.Restaurants.IgnoreQueryFilters()
            .Include(r => r.OperatingHours)
            .FirstAsync(r => r.Id == request.RestaurantId, cancellationToken);

        return new PublicTableInfoResponse(GetPublicMenuHandler.ToRestaurantInfo(restaurant), table.Identifier);
    }
}

public class GetPublicTableInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/restaurants/{restaurantId:guid}/tables/{qrToken}",
            async (Guid restaurantId, string qrToken, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetPublicTableInfoQuery(restaurantId, qrToken), ct)))
        .WithName("GetPublicTableInfo")
        .WithTags("Catalog")
        .AllowAnonymous();
    }
}
