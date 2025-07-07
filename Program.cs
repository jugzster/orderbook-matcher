using OrderbookMatcher;

Console.WriteLine("--- Orders matched by price-time ---");
Console.WriteLine("Before matching:");
List<Order> priceTimeOrders =
[
    new Order("A", "A1", Direction.Buy, 100, 4.99m, new DateTime(2025, 6, 1, 9, 27, 0)),
    new Order("B", "B1", Direction.Buy, 200, 5.00m, new DateTime(2025, 6, 1, 10, 21, 0)),
    new Order("C", "C1", Direction.Buy, 150, 5.00m, new DateTime(2025, 6, 1, 10, 26, 0)),
    new Order("D", "D1", Direction.Sell, 150, 5.00m, new DateTime(2025, 6, 1, 10, 32, 0)),
    new Order("E", "E1", Direction.Sell, 100, 5.00m, new DateTime(2025, 6, 1, 10, 33, 0))
];
Print(priceTimeOrders);

IOrderMatcher priceTimeOrderMatcher = new PriceTimeOrderMatcher();
List<Order> priceTimeMatchedOrders = priceTimeOrderMatcher.MatchOrders(priceTimeOrders);
Console.WriteLine("\nAfter matching:");
Print(priceTimeMatchedOrders);


Console.WriteLine("\n--- Orders matched by pro-rata ---");
Console.WriteLine("Before matching:");
List<Order> proRataOrders =
[
    new Order("A", "A1", Direction.Buy, 50, 5.00m, new DateTime(2025, 6, 1, 9, 27, 0)),
    new Order("B", "B1", Direction.Buy, 200, 5.00m, new DateTime(2025, 6, 1, 10, 21, 0)),
    new Order("C", "C1", Direction.Sell, 200, 5.00m, new DateTime(2025, 6, 1, 10, 26, 0)),
    new Order("D", "D1", Direction.Buy, 300, 6.00m, new DateTime(2025, 6, 1, 9, 27, 0)),
    new Order("E", "E1", Direction.Sell, 50, 6.00m, new DateTime(2025, 6, 1, 10, 21, 0)),
    new Order("F", "F1", Direction.Sell, 150, 6.00m, new DateTime(2025, 6, 1, 10, 26, 0)),
];
Print(proRataOrders);

IOrderMatcher proRataOrderMatcher = new ProRataOrderMatcher();
List<Order> proRataMatchedOrders = proRataOrderMatcher.MatchOrders(proRataOrders);
Console.WriteLine("\nAfter matching:");
Print(proRataMatchedOrders);

Console.WriteLine("Done! Press any key to exit...");

static void Print(List<Order> orders)
{
    foreach (var order in orders.OrderBy(o => o.OrderId))
    {
        Console.WriteLine($"OrderId {order.OrderId} {order.Direction} - {order.MatchState}, Notional {order.Notional}, " +
            $"Original {order.Volume}, Remaining {order.RemainingVolume}");
        foreach (var match in order.MatchedOrders)
        {
            Console.WriteLine($"  - Matched with {match.OrderId}, Notional {match.Notional}, Volume {match.Volume}");
        }
    }
}