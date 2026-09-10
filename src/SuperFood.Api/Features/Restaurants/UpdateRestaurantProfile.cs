using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SuperFood.Api.Common;
using SuperFood.Api.Common.Exceptions;
using SuperFood.Contracts.Restaurants;
using SuperFood.Domain;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>US-0201: restaurant_owner completes their restaurant's public profile.</summary>
public record UpdateRestaurantProfileCommand(
    Guid RestaurantId, string Name, string? LogoUrl, string? Address, string? ContactPhone, string? ContactEmail)
    : IRequest;

public class UpdateRestaurantProfileValidator : AbstractValidator<UpdateRestaurantProfileCommand>
{
    public UpdateRestaurantProfileValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateRestaurantProfileHandler(SuperFoodDbContext db) : IRequestHandler<UpdateRestaurantProfileCommand>
{
    public async Task Handle(UpdateRestaurantProfileCommand request, CancellationToken cancellationToken)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Id == request.RestaurantId, cancellationToken)
            ?? throw new NotFoundException("Restaurant was not found.");

        restaurant.Name = request.Name;
        restaurant.LogoUrl = request.LogoUrl;
        restaurant.Address = request.Address;
        restaurant.ContactPhone = request.ContactPhone;
        restaurant.ContactEmail = request.ContactEmail;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateRestaurantProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/restaurants/me/profile",
            async (UpdateRestaurantProfileRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var restaurantId = http.User.GetRestaurantId()
                    ?? throw new UnauthorizedAccessException("No restaurant context.");
                await sender.Send(new UpdateRestaurantProfileCommand(
                    restaurantId, request.Name, request.LogoUrl, request.Address, request.ContactPhone, request.ContactEmail), ct);
                return Results.NoContent();
            })
        .WithName("UpdateRestaurantProfile")
        .WithTags("Restaurants")
        .RequirePermission(Permissions.RestaurantManage);
    }
}
