// Matches orders based on pro-rata allocation
public class ProRataOrderMatcher : IOrderMatcher
{
    public List<Order> MatchOrders(List<Order> orders)
    {
        // Reset order state and match list
        foreach (var order in orders)
            order.ResetMatchState();

        // Group orders by notional and direction
        var buyGroups = orders
            .Where(o => o.Direction == Direction.Buy && o.MatchState == MatchState.Pending)
            .GroupBy(o => o.Notional);

        // Group orders into a dictionary for O(1) lookup
        var sellGroupDict = orders
            .Where(o => o.Direction == Direction.Sell && o.MatchState == MatchState.Pending)
            .GroupBy(o => o.Notional)
            .ToDictionary(g => g.Key, g => g);

        foreach (var buyGroup in buyGroups)
        {
            var notional = buyGroup.Key;
            if (!sellGroupDict.TryGetValue(notional, out var sells))
                continue;

            var buys = buyGroup.ToList();

            var totalBuyVolume = buys.Sum(b => b.RemainingVolume);
            var totalSellVolume = sells.Sum(s => s.RemainingVolume);
            // Use the minimum of total buy and sell volumes to avoid over-matching
            var matchVolume = Math.Min(totalBuyVolume, totalSellVolume);

            if (matchVolume == 0)
                continue;

            // If buy volume is greater than or equal to sell volume, compute ratio based on buy volume then allocate to sells
            if (totalBuyVolume >= totalSellVolume)
            {
                foreach (var buy in buys)
                {
                    if (buy.RemainingVolume == 0)
                        continue;

                    var ratio = (double)buy.RemainingVolume / totalBuyVolume;
                    // Use Math.Floor to allocate whole shares and to avoid over-allocation if rounding
                    var sharesToAllocate = (int)Math.Floor(ratio * matchVolume);

                    var queue = new Queue<Order>(sells.Where(s => s.RemainingVolume > 0));
                    while (sharesToAllocate > 0 && queue.Count > 0)
                    {
                        var sell = queue.Peek();

                        // Use Math.Min to ensure we don't allocate more than available
                        int allocation = Math.Min(sell.RemainingVolume, sharesToAllocate);
                        if (allocation == 0)
                        {
                            queue.Dequeue(); // Remove exhausted order
                            continue;
                        }

                        // Update matched orders
                        buy.RemainingVolume -= allocation;
                        sell.RemainingVolume -= allocation;

                        // Record the match
                        buy.MatchedOrders.Add(new Match(sell.OrderId, notional, allocation));
                        sell.MatchedOrders.Add(new Match(buy.OrderId, notional, allocation));

                        sharesToAllocate -= allocation;
                        if (sell.RemainingVolume == 0)
                            queue.Dequeue(); // Remove exhausted order
                    }
                }
            }
            // If sell volume is greater than buy volume, compute ratio based on sell volume then allocate to buys
            else
            {
                foreach (var sell in sells)
                {
                    if (sell.RemainingVolume == 0)
                        continue;

                    var ratio = (double)sell.RemainingVolume / totalSellVolume;
                    // Use Math.Floor to allocate whole shares and to avoid over-allocation if rounding
                    var sharesToAllocate = (int)Math.Floor(ratio * matchVolume);

                    var queue = new Queue<Order>(buys.Where(s => s.RemainingVolume > 0));
                    while (sharesToAllocate > 0 && queue.Count > 0)
                    {
                        var buy = queue.Peek();

                        // Use Math.Min to ensure we don't allocate more than available
                        int allocation = Math.Min(buy.RemainingVolume, sharesToAllocate);
                        if (allocation == 0)
                        {
                            queue.Dequeue(); // Remove exhausted order
                            continue;
                        }

                        // Update matched orders
                        sell.RemainingVolume -= allocation;
                        buy.RemainingVolume -= allocation;

                        // Record the match
                        sell.MatchedOrders.Add(new Match(buy.OrderId, notional, allocation));
                        buy.MatchedOrders.Add(new Match(sell.OrderId, notional, allocation));

                        sharesToAllocate -= allocation;
                        if (buy.RemainingVolume == 0)
                            queue.Dequeue(); // Remove exhausted order
                    }
                }
            }
        }

        // After all matching attempts, update the final MatchState for all original orders
        foreach (var order in orders)
        {
            order.MatchState = order.RemainingVolume switch
            {
                0 when order.MatchedOrders.Count > 0 => MatchState.FullMatch,
                > 0 when order.MatchedOrders.Count > 0 => MatchState.PartialMatch,
                > 0 => MatchState.NoMatch,
                _ => order.MatchState
            };
        }

        return orders;
    }
}