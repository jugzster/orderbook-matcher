namespace OrderbookMatcher.Tests;

using OrderbookMatcher;
using Shouldly;

[TestClass]
public class ProRataOrderMatcherTests
{
    [TestMethod]
    public void MatchOrders_BuyAndSellSameVolume_FullMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Sell, 100, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.FullMatch);
        a1.RemainingVolume.ShouldBe(0);
        a1.MatchedOrders.Count.ShouldBe(1);
        a1.MatchedOrders[0].OrderId.ShouldBe("B1");

        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.FullMatch);
        b1.RemainingVolume.ShouldBe(0);
        b1.MatchedOrders.Count.ShouldBe(1);
        b1.MatchedOrders[0].OrderId.ShouldBe("A1");
    }

    [TestMethod]
    public void MatchOrders_BuyMoreThanSell_PartialMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 200, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Buy, 100, 5.00m, DateTime.Now),
            new Order("C", "C1", Direction.Sell, 150, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // Total: Buy 300, Sell 150
        // A gets 66% of 150 (100)
        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.PartialMatch);
        a1.RemainingVolume.ShouldBe(100);
        a1.MatchedOrders.Sum(m => m.Volume).ShouldBe(100);
        a1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("C1", 5.00m, 100)
        });

        // B gets 33% of 150 (50)
        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.PartialMatch);
        b1.RemainingVolume.ShouldBe(50);
        b1.MatchedOrders.Sum(m => m.Volume).ShouldBe(50);
        b1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("C1", 5.00m, 50)
        });

        var c1 = result.First(o => o.OrderId == "C1");
        c1.MatchState.ShouldBe(MatchState.FullMatch);
        c1.RemainingVolume.ShouldBe(0);
        c1.MatchedOrders.Sum(m => m.Volume).ShouldBe(150);
        c1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("A1", 5.00m, 100),
            new Match("B1", 5.00m, 50),
        });
    }

    [TestMethod]
    public void MatchOrders_SellMoreThanBuy_PartialMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Sell, 150, 5.00m, DateTime.Now),
            new Order("C", "C1", Direction.Sell, 50, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // Total: Buy 100, Sell 200
        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.FullMatch);
        a1.RemainingVolume.ShouldBe(0);
        a1.MatchedOrders.Sum(m => m.Volume).ShouldBe(100);
        a1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("B1", 5.00m, 75),
            new Match("C1", 5.00m, 25)
        });

        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.PartialMatch);
        b1.RemainingVolume.ShouldBe(75);
        b1.MatchedOrders.Sum(m => m.Volume).ShouldBe(75);
        b1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("A1", 5.00m, 75),
        });

        var c1 = result.First(o => o.OrderId == "C1");
        c1.MatchState.ShouldBe(MatchState.PartialMatch);
        c1.RemainingVolume.ShouldBe(25);
        c1.MatchedOrders.Sum(m => m.Volume).ShouldBe(25);
        c1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("A1", 5.00m, 25),
        });
    }

    [TestMethod]
    public void MatchOrders_NoMatchingNotional_NoMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 4.00m, DateTime.Now),
            new Order("B", "B1", Direction.Sell, 100, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.NoMatch);
        a1.MatchedOrders.Count.ShouldBe(0);

        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.NoMatch);
        b1.MatchedOrders.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_ZeroAndNegativeVolume_InvalidOrderState()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 0, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Sell, -10, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.InvalidOrder);
        a1.MatchedOrders.Count.ShouldBe(0);

        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.InvalidOrder);
        b1.MatchedOrders.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_MultipleNotionals_CorrectStatesBasedOnProRataAllocation()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 50, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Buy, 200, 5.00m, DateTime.Now),
            new Order("C", "C1", Direction.Sell, 200, 5.00m, DateTime.Now),
            new Order("D", "D1", Direction.Buy, 300, 6.00m, DateTime.Now),
            new Order("E", "E1", Direction.Sell, 50, 6.00m, DateTime.Now),
            new Order("F", "F1", Direction.Sell, 150, 6.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // A1 gets 40, B1 gets 160, C1 is fully matched
        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.PartialMatch);
        a1.RemainingVolume.ShouldBe(10);
        a1.MatchedOrders.Sum(m => m.Volume).ShouldBe(40);
        a1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("C1", 5.00m, 40)
        });

        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.PartialMatch);
        b1.RemainingVolume.ShouldBe(40);
        b1.MatchedOrders.Sum(m => m.Volume).ShouldBe(160);
        b1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("C1", 5.00m, 160)
        });

        var c1 = result.First(o => o.OrderId == "C1");
        c1.MatchState.ShouldBe(MatchState.FullMatch);
        c1.RemainingVolume.ShouldBe(0);
        c1.MatchedOrders.Sum(m => m.Volume).ShouldBe(200);
        c1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("A1", 5.00m, 40),
            new Match("B1", 5.00m, 160)
        });

        var d1 = result.First(o => o.OrderId == "D1");
        d1.MatchState.ShouldBe(MatchState.PartialMatch);
        d1.RemainingVolume.ShouldBe(100);
        d1.MatchedOrders.Sum(m => m.Volume).ShouldBe(200);
        d1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("E1", 6.00m, 50),
            new Match("F1", 6.00m, 150)
        });

        var e1 = result.First(o => o.OrderId == "E1");
        e1.MatchState.ShouldBe(MatchState.FullMatch);
        e1.RemainingVolume.ShouldBe(0);
        e1.MatchedOrders.Sum(m => m.Volume).ShouldBe(50);
        e1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("D1", 6.00m, 50)
        });

        var f1 = result.First(o => o.OrderId == "F1");
        f1.MatchState.ShouldBe(MatchState.FullMatch);
        f1.RemainingVolume.ShouldBe(0);
        f1.MatchedOrders.Sum(m => m.Volume).ShouldBe(150);
        f1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("D1", 6.00m, 150)
        });
    }

    [TestMethod]
    public void MatchOrders_ZeroOrders_EmptyResult()
    {
        var matcher = new ProRataOrderMatcher();
     
        var result = matcher.MatchOrders([]);
        
        result.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_FirstSellLowerThanBuyRatio_AllSellSharesShouldBeAllocated()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 200, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Buy, 50, 5.00m, DateTime.Now),
            new Order("C", "C1", Direction.Sell, 50, 5.00m, DateTime.Now),
            new Order("D", "D1", Direction.Sell, 150, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // Total: Buy 250, Sell 200
        // A gets 80% of 200 (160)
        var a1 = result.First(o => o.OrderId == "A1");
        a1.MatchState.ShouldBe(MatchState.PartialMatch);
        a1.RemainingVolume.ShouldBe(40);
        a1.MatchedOrders.Sum(m => m.Volume).ShouldBe(160);
        a1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("C1", 5.00m, 50),
            new Match("D1", 5.00m, 110),
        });

        // B gets 20% of 200 (40)
        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.PartialMatch);
        b1.RemainingVolume.ShouldBe(10);
        b1.MatchedOrders.Sum(m => m.Volume).ShouldBe(40);
        b1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("D1", 5.00m, 40)
        });

        // C1 is fully matched
        var c1 = result.First(o => o.OrderId == "C1");
        c1.MatchState.ShouldBe(MatchState.FullMatch);
        c1.RemainingVolume.ShouldBe(0);
        c1.MatchedOrders.Sum(m => m.Volume).ShouldBe(50);
        c1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("A1", 5.00m, 50),
        });

        // D1 is fully matched
        var d1 = result.First(o => o.OrderId == "D1");
        d1.MatchState.ShouldBe(MatchState.FullMatch);
        d1.RemainingVolume.ShouldBe(0);
        d1.MatchedOrders.Sum(m => m.Volume).ShouldBe(150);
        d1.MatchedOrders.ShouldBeEquivalentTo(new List<Match>()
        {
            new Match("A1", 5.00m, 110),
            new Match("B1", 5.00m, 40)
        });
    }


    [TestMethod]
    public void MatchOrders_WithLeftoverShares_LeftoverSharesDistributed()
    {
        // 3 buys, 2 sells, total buy=300, total sell=100
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 200, 5.00m, DateTime.Now.AddSeconds(1)),
            new Order("A", "A2", Direction.Buy, 75, 5.00m, DateTime.Now.AddSeconds(2)),
            new Order("A", "A3", Direction.Buy, 25, 5.00m, DateTime.Now.AddSeconds(3)),
            new Order("B", "B1", Direction.Sell, 50, 5.00m, DateTime.Now),
            new Order("B", "B2", Direction.Sell, 50, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // Buys should get 67, 25, 8 (leftover share goes to order with biggest remainder)
        var a1Sum = result.First(o => o.OrderId == "A1").MatchedOrders.Sum(m => m.Volume);
        a1Sum.ShouldBe(67);

        var a2Sum = result.First(o => o.OrderId == "A2").MatchedOrders.Sum(m => m.Volume);
        a2Sum.ShouldBe(25);

        var a3Sum = result.First(o => o.OrderId == "A3").MatchedOrders.Sum(m => m.Volume); ;
        a3Sum.ShouldBe(8);

        var totalMatched = a1Sum + a2Sum + a3Sum;
        totalMatched.ShouldBe(100);
    }

    [TestMethod]
    public void MatchOrders_AllBuyOrders_NoMatch()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 5.00m, DateTime.Now),
            new Order("A", "A2", Direction.Buy, 200, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        result.All(o => o.MatchState == MatchState.NoMatch).ShouldBeTrue();
    }

    [TestMethod]
    public void MatchOrders_AllSellOrders_NoMatch()
    {
        var orders = new List<Order>
        {
            new Order("B", "B1", Direction.Sell, 100, 5.00m, DateTime.Now),
            new Order("B", "B2", Direction.Sell, 200, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        result.All(o => o.MatchState == MatchState.NoMatch).ShouldBeTrue();
    }
}