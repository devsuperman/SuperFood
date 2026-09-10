using FluentAssertions;
using SuperFood.Domain.Entities;
using SuperFood.Domain.Enums;
using Xunit;

namespace SuperFood.UnitTests.Domain;

/// <summary>Verifies the Order.Total computation backing every order/bill view (EPIC-07/08/09).</summary>
public class OrderTests
{
    [Fact]
    public void Total_sums_quantity_times_unit_plus_variation_price()
    {
        var order = new Order
        {
            Items =
            [
                new OrderItem { UnitPrice = 10m, VariationPriceAdjustment = 2m, Quantity = 2, Status = OrderItemStatus.Pending },
                new OrderItem { UnitPrice = 5m, VariationPriceAdjustment = 0m, Quantity = 1, Status = OrderItemStatus.Ready }
            ]
        };

        order.Total.Should().Be(29m); // (10+2)*2 + (5+0)*1
    }

    [Fact]
    public void Total_excludes_cancelled_items()
    {
        var order = new Order
        {
            Items =
            [
                new OrderItem { UnitPrice = 10m, Quantity = 1, Status = OrderItemStatus.Pending },
                new OrderItem { UnitPrice = 100m, Quantity = 5, Status = OrderItemStatus.Cancelled }
            ]
        };

        order.Total.Should().Be(10m);
    }
}
