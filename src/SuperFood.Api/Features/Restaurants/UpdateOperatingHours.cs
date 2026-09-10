using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Contracts.Restaurants;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>
/// US-0202: set operating hours per day. Customer ordering is blocked outside
/// these hours (enforced where orders are created, EPIC-07/EPIC-08).
/// </summary>
public record UpdateOperatingHoursCommand(Guid RestaurantId, List<OperatingHourDto> Hours) : IRequest;

public class UpdateOperatingHoursHandler(SuperFoodDbContext db) : IRequestHandler<UpdateOperatingHoursCommand>
{
    public async Task Handle(UpdateOperatingHoursCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.RestaurantOperatingHours
            .Where(h => h.RestaurantId == request.RestaurantId)
            .ToListAsync(cancellationToken);
        db.RestaurantOperatingHours.RemoveRange(existing);

        db.RestaurantOperatingHours.AddRange(request.Hours.Select(h => new RestaurantOperatingHour
        {
            Id = Guid.NewGuid(),
            RestaurantId = request.RestaurantId,
            DayOfWeek = h.DayOfWeek,
            OpenTime = h.OpenTime,
            CloseTime = h.CloseTime,
            IsClosed = h.IsClosed
        }));

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateOperatingHoursEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/restaurants/me/hours",
            async (UpdateOperatingHoursRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new UpdateOperatingHoursCommand(restaurantId, request.Hours), ct);
                return Results.NoContent();
            })
        .WithName("UpdateOperatingHours")
        .WithTags("Restaurants")
        .RequirePermission(Permissions.RestaurantManage);
    }
}
