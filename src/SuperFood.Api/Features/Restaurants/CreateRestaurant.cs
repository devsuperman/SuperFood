using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SuperFood.Api.Common;
using SuperFood.Contracts.Restaurants;
using SuperFood.Domain;
using SuperFood.Domain.Entities;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;

namespace SuperFood.Api.Features.Restaurants;

/// <summary>US-0101: platform_admin creates a new restaurant tenant + its initial owner.</summary>
public record CreateRestaurantCommand(
    string Name,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string OwnerFullName,
    string OwnerEmail) : IRequest<CreateRestaurantResponse>;

public class CreateRestaurantValidator : AbstractValidator<CreateRestaurantCommand>
{
    public CreateRestaurantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerFullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress();
    }
}

public class CreateRestaurantHandler(SuperFoodDbContext db, UserManager<ApplicationUser> userManager)
    : IRequestHandler<CreateRestaurantCommand, CreateRestaurantResponse>
{
    public async Task<CreateRestaurantResponse> Handle(CreateRestaurantCommand request, CancellationToken cancellationToken)
    {
        var restaurant = new Restaurant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Address = request.Address
        };
        db.Restaurants.Add(restaurant);

        var ownerRole = new Role
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            Name = "Owner",
            Permissions = [.. Permissions.All]
        };
        db.Set<Role>().Add(ownerRole);

        // No email service in this iteration (docs/user-stories.md §4): return
        // a temporary password the platform_admin relays to the owner.
        var temporaryPassword = $"{Convert.ToBase64String(RandomNumberGenerator.GetBytes(9))}!1Aa";

        var owner = new ApplicationUser
        {
            UserName = request.OwnerEmail,
            Email = request.OwnerEmail,
            FullName = request.OwnerFullName,
            RestaurantId = restaurant.Id,
            RoleId = ownerRole.Id,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(owner, temporaryPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors
                .Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.OwnerEmail), e.Description)));
        }

        await db.SaveChangesAsync(cancellationToken);

        return new CreateRestaurantResponse(restaurant.Id, owner.Id, owner.Email!, temporaryPassword);
    }
}

public class CreateRestaurantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/restaurants", async (CreateRestaurantRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateRestaurantCommand(
                request.Name, request.ContactEmail, request.ContactPhone, request.Address,
                request.OwnerFullName, request.OwnerEmail), ct);
            return Results.Created($"/api/restaurants/{result.RestaurantId}", result);
        })
        .WithName("CreateRestaurant")
        .WithTags("Restaurants")
        .RequirePlatformAdmin();
    }
}
