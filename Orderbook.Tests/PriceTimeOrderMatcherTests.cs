using Shouldly;

[TestClass]
public class PriceTimeOrderMatcherTests
{
    [TestMethod]
    public void MatchOrders_BuyAndSellAreSame_FullMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 5.00m, new DateTime(2025, 6, 1, 9, 0, 0)),
            new Order("B", "B1", Direction.Sell, 100, 5.00m, new DateTime(2025, 6, 1, 9, 1, 0))
        };
        var matcher = new PriceTimeOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var buyOrder = result.First(o => o.OrderId == "A1");
        buyOrder.MatchState.ShouldBe(MatchState.FullMatch);
        buyOrder.RemainingVolume.ShouldBe(0);
        buyOrder.MatchedOrders.Count.ShouldBe(1);
        buyOrder.MatchedOrders[0].OrderId.ShouldBe("B1");

        var sellOrder = result.First(o => o.OrderId == "B1");
        sellOrder.MatchState.ShouldBe(MatchState.FullMatch);
        sellOrder.RemainingVolume.ShouldBe(0);
        sellOrder.MatchedOrders.Count.ShouldBe(1);
        sellOrder.MatchedOrders[0].OrderId.ShouldBe("A1");
    }

    [TestMethod]
    public void MatchOrders_BuyIsMoreThanSell_PartialMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 150, 5.00m, new DateTime(2025, 6, 1, 9, 0, 0)),
            new Order("B", "B1", Direction.Sell, 100, 5.00m, new DateTime(2025, 6, 1, 9, 1, 0))
        };
        var matcher = new PriceTimeOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var buyOrder = result.First(o => o.OrderId == "A1");
        buyOrder.MatchState.ShouldBe(MatchState.PartialMatch);
        buyOrder.RemainingVolume.ShouldBe(50);
        buyOrder.MatchedOrders.Count.ShouldBe(1);

        var sellOrder = result.First(o => o.OrderId == "B1");
        sellOrder.MatchState.ShouldBe(MatchState.FullMatch);
        sellOrder.RemainingVolume.ShouldBe(0);
        sellOrder.MatchedOrders.Count.ShouldBe(1);
    }

    [TestMethod]
    public void MatchOrders_BuyIsLessThanSell_PartialMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 5.00m, new DateTime(2025, 6, 1, 9, 0, 0)),
            new Order("B", "B1", Direction.Sell, 150, 5.00m, new DateTime(2025, 6, 1, 9, 1, 0))
        };
        var matcher = new PriceTimeOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var buyOrder = result.First(o => o.OrderId == "A1");
        buyOrder.MatchState.ShouldBe(MatchState.FullMatch);
        buyOrder.RemainingVolume.ShouldBe(0);
        buyOrder.MatchedOrders.Count.ShouldBe(1);

        var sellOrder = result.First(o => o.OrderId == "B1");
        sellOrder.MatchState.ShouldBe(MatchState.PartialMatch);
        sellOrder.RemainingVolume.ShouldBe(50);
        sellOrder.MatchedOrders.Count.ShouldBe(1);
    }

    [TestMethod]
    public void MatchOrders_NoMatchingPrice_NoMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 4.00m, new DateTime(2025, 6, 1, 9, 0, 0)),
            new Order("B", "B1", Direction.Sell, 100, 5.00m, new DateTime(2025, 6, 1, 9, 1, 0))
        };
        var matcher = new PriceTimeOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var buyOrder = result.First(o => o.OrderId == "A1");
        buyOrder.MatchState.ShouldBe(MatchState.NoMatch);
        buyOrder.RemainingVolume.ShouldBe(100);
        buyOrder.MatchedOrders.Count.ShouldBe(0);

        var sellOrder = result.First(o => o.OrderId == "B1");
        sellOrder.MatchState.ShouldBe(MatchState.NoMatch);
        sellOrder.RemainingVolume.ShouldBe(100);
        sellOrder.MatchedOrders.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_WithInvalidOrder_InvalidOrder()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 0, 5.00m, new DateTime(2025, 6, 1, 9, 0, 0)),
            new Order("B", "B1", Direction.Sell, -10, 5.00m, new DateTime(2025, 6, 1, 9, 1, 0))
        };
        var matcher = new PriceTimeOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var buyOrder = result.First(o => o.OrderId == "A1");
        buyOrder.MatchState.ShouldBe(MatchState.InvalidOrder);
        buyOrder.MatchedOrders.Count.ShouldBe(0);

        var sellOrder = result.First(o => o.OrderId == "B1");
        sellOrder.MatchState.ShouldBe(MatchState.InvalidOrder);
        sellOrder.MatchedOrders.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_MultipleOrders_CorrectStatesBasedOnPriceTimePriority()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 4.99m, new DateTime(2025, 6, 1, 9, 27, 0)),
            new Order("B", "B1", Direction.Buy, 200, 5.00m, new DateTime(2025, 6, 1, 10, 21, 0)),
            new Order("C", "C1", Direction.Buy, 150, 5.00m, new DateTime(2025, 6, 1, 10, 26, 0)),
            new Order("D", "D1", Direction.Sell, 150, 5.00m, new DateTime(2025, 6, 1, 10, 32, 0)),
            new Order("E", "E1", Direction.Sell, 100, 5.00m, new DateTime(2025, 6, 1, 10, 33, 0)),
            new Order("F", "F1", Direction.Sell, 100, 7.00m, new DateTime(2025, 6, 1, 10, 33, 0))
        };
        var matcher = new PriceTimeOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // A1 NoMatch
        result[0].MatchState.ShouldBe(MatchState.NoMatch);
        result[0].MatchedOrders.Count.ShouldBe(0);

        // B1 matches D1 (150) and E1 (50)
        result[1].MatchState.ShouldBe(MatchState.FullMatch);
        result[1].RemainingVolume.ShouldBe(0);
        result[1].MatchedOrders.ShouldBeEquivalentTo(new List<Match>
        {
            new Match("D1", 5.00m, 150),
            new Match("E1", 5.00m, 50)
        });

        // C1 partially matches E1
        result[2].MatchState.ShouldBe(MatchState.PartialMatch);
        result[2].RemainingVolume.ShouldBe(100);
        result[2].MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("E1", 5.00m, 50)
        });

        // D1 matches B1
        result[3].MatchState.ShouldBe(MatchState.FullMatch);
        result[3].RemainingVolume.ShouldBe(0);
        result[3].MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("B1", 5.00m, 150)
        });

        // E1 matches B1 and C1
        result[4].MatchState.ShouldBe(MatchState.FullMatch);
        result[4].RemainingVolume.ShouldBe(0);
        result[4].MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("B1", 5.00m, 50),
            new Match("C1", 5.00m, 50)
        });

        // F1 NoMatch
        result[5].MatchState.ShouldBe(MatchState.NoMatch);
        result[5].MatchedOrders.Count.ShouldBe(0);
    }
}