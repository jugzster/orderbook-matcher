using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        // B1 and C1 should be partially matched
        var b1 = result.First(o => o.OrderId == "B1");
        b1.MatchState.ShouldBe(MatchState.PartialMatch);
        b1.RemainingVolume.ShouldBe(100);
        b1.MatchedOrders.Sum(m => m.Volume).ShouldBe(50);

        var c1 = result.First(o => o.OrderId == "C1");
        c1.MatchState.ShouldBe(MatchState.PartialMatch);
        c1.RemainingVolume.ShouldBe(0);
        c1.MatchedOrders.Sum(m => m.Volume).ShouldBe(50);
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

        result[0].MatchState.ShouldBe(MatchState.NoMatch);
        result[0].MatchedOrders.Count.ShouldBe(0);

        result[1].MatchState.ShouldBe(MatchState.NoMatch);
        result[1].MatchedOrders.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_InvalidOrders_InvalidOrderState()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 0, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Sell, -10, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        result[0].MatchState.ShouldBe(MatchState.InvalidOrder);
        result[0].MatchedOrders.Count.ShouldBe(0);

        result[1].MatchState.ShouldBe(MatchState.InvalidOrder);
        result[1].MatchedOrders.Count.ShouldBe(0);
    }

    [TestMethod]
    public void MatchOrders_MultipleNotionals_OnlySameNotionalMatched()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 100, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Buy, 100, 6.00m, DateTime.Now),
            new Order("C", "C1", Direction.Sell, 100, 5.00m, DateTime.Now),
            new Order("D", "D1", Direction.Sell, 100, 6.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // A1 and C1 matched
        result.Find(o => o.OrderId == "A1").MatchState.ShouldBe(MatchState.FullMatch);
        result.Find(o => o.OrderId == "C1").MatchState.ShouldBe(MatchState.FullMatch);

        // B1 and D1 matched
        result.Find(o => o.OrderId == "B1").MatchState.ShouldBe(MatchState.FullMatch);
        result.Find(o => o.OrderId == "D1").MatchState.ShouldBe(MatchState.FullMatch);
    }

    [TestMethod]
    public void MatchOrders_ProRataRounding_DistributesRemainder()
    {
        var orders = new List<Order>
        {
            new Order("A", "A1", Direction.Buy, 50, 5.00m, DateTime.Now),
            new Order("B", "B1", Direction.Buy, 200, 5.00m, DateTime.Now),
            new Order("C", "C1", Direction.Sell, 200, 5.00m, DateTime.Now)
        };
        var matcher = new ProRataOrderMatcher();

        var result = matcher.MatchOrders(orders);

        // A1 gets 40, B1 gets 160, C1 is fully matched
        var a1 = result.Find(o => o.OrderId == "A1");
        var b1 = result.Find(o => o.OrderId == "B1");
        var c1 = result.Find(o => o.OrderId == "C1");

        a1.MatchState.ShouldBe(MatchState.PartialMatch);
        a1.RemainingVolume.ShouldBe(10);
        a1.MatchedOrders.Sum(m => m.Volume).ShouldBe(40);

        b1.MatchState.ShouldBe(MatchState.PartialMatch);
        b1.RemainingVolume.ShouldBe(40);
        b1.MatchedOrders.Sum(m => m.Volume).ShouldBe(160);

        c1.MatchState.ShouldBe(MatchState.FullMatch);
        c1.RemainingVolume.ShouldBe(0);
        c1.MatchedOrders.Sum(m => m.Volume).ShouldBe(200);
    }

    [TestMethod]
    public void MatchOrders_ZeroOrders_EmptyResult()
    {
        var matcher = new ProRataOrderMatcher();
     
        var result = matcher.MatchOrders([]);
        
        result.Count.ShouldBe(0);
    }
}