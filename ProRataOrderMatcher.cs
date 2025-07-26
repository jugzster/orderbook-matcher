// Matches orders based on pro-rata allocation
namespace OrderbookMatcher;

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
            // Get the sell orders for this notional
            if (!sellGroupDict.TryGetValue(notional, out var sells))
                continue;

            var buys = buyGroup.ToList();
            var sellList = sells.ToList();

            var totalBuyVolume = buys.Sum(b => b.RemainingVolume);
            var totalSellVolume = sells.Sum(s => s.RemainingVolume);
            // Use the minimum of total buy and sell volumes to avoid over-matching
            var matchVolume = Math.Min(totalBuyVolume, totalSellVolume);

            if (matchVolume == 0)
                continue;

            // If buy volume is greater than or equal to sell volume, compute ratio based on buy volume then allocate to sells
            if (totalBuyVolume >= totalSellVolume)
            {
                List<(Order order, int toAllocate)> allocations = GetProRataAllocations(buys, matchVolume);

                // Use queue for O(n) matching, ensures order is fully "drained" only once
                var queue = new Queue<Order>(sells.Where(s => s.RemainingVolume > 0));

                foreach (var (buy, sharesToAllocate) in allocations)
                {
                    if (buy.RemainingVolume == 0 || sharesToAllocate == 0)
                        continue;

                    var remaining = sharesToAllocate; // sharesToAllocate is readonly
                    while (remaining > 0 && queue.Count > 0)
                    {
                        var sell = queue.Peek();
                        // Take the lower of the two to ensure we don't over-allocate in case other side's shares are not enough
                        int allocation = Math.Min(sell.RemainingVolume, remaining);
                        if (allocation == 0)
                        {
                            queue.Dequeue(); // Remove drained order
                            continue;
                        }

                        // Update matched orders
                        buy.RemainingVolume -= allocation;
                        sell.RemainingVolume -= allocation;

                        // Record the match
                        buy.MatchedOrders.Add(new Match(sell.OrderId, notional, allocation));
                        sell.MatchedOrders.Add(new Match(buy.OrderId, notional, allocation));

                        remaining -= allocation;
                        if (sell.RemainingVolume == 0)
                            queue.Dequeue(); // Remove drained order
                    }
                }
            }
            // If sell volume is greater than buy volume, compute ratio based on sell volume then allocate to buys
            else
            {
                List<(Order order, int toAllocate)> allocations = GetProRataAllocations(sellList, matchVolume);

                // Use queue for O(n) matching, ensures order is fully "drained" only once
                var queue = new Queue<Order>(buys.Where(s => s.RemainingVolume > 0));

                foreach (var (sell, sharesToAllocate) in allocations)
                {
                    if (sell.RemainingVolume == 0 || sharesToAllocate == 0)
                        continue;

                    var remaining = sharesToAllocate; // sharesToAllocate is readonly
                    while (remaining > 0 && queue.Count > 0)
                    {
                        var buy = queue.Peek();
                        // Take the lower of the two to ensure we don't over-allocate in case other side's shares are not enough
                        int allocation = Math.Min(buy.RemainingVolume, sharesToAllocate);
                        if (allocation == 0)
                        {
                            queue.Dequeue(); // Remove drained order
                            continue;
                        }

                        // Update matched orders
                        sell.RemainingVolume -= allocation;
                        buy.RemainingVolume -= allocation;

                        // Record the match
                        sell.MatchedOrders.Add(new Match(buy.OrderId, notional, allocation));
                        buy.MatchedOrders.Add(new Match(sell.OrderId, notional, allocation));

                        remaining -= allocation;
                        if (buy.RemainingVolume == 0)
                            queue.Dequeue(); // Remove drained order
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

    private List<(Order order, int toAllocate)> GetProRataAllocations(List<Order> orders, int matchVolume)
    {
        int totalRemainingVolume = orders.Sum(o => o.RemainingVolume);
        if (totalRemainingVolume == 0 || matchVolume == 0)
            return orders.Select(order => (order, 0)).ToList();

        List<(Order order, int floorAlloc, double remainder)> allocations = [];
        int floorSum = 0;

        // Compute the allocations based on ratio of order's remaining volume to total remaining volume
        // Store the remainder for allocation later
        foreach (var order in orders)
        {
            double ratio = (double)order.RemainingVolume / totalRemainingVolume;
            double exactAmount = ratio * matchVolume;
            int floorAlloc = (int)Math.Floor(exactAmount);
            double remainder = exactAmount - floorAlloc;

            allocations.Add((order, floorAlloc, remainder));
            floorSum += floorAlloc;
        }

        // Distribute any leftover shares (due to rounding down) to orders with largest remainders
        int totalRemainder = matchVolume - floorSum;
        var sorted = allocations.OrderByDescending(x => x.remainder)
            .ThenBy(x => x.order.OrderId) // Tie-breaker
            .ToList();

        for (int i = 0; i < totalRemainder; i++)
        {
            var order = sorted[i]; // Tuple is a value type, so need to copy first...
            order.floorAlloc += 1; // Modify the copy...
            sorted[i] = order; // Then assign back
        }

        return sorted.Select(x => (x.order, x.floorAlloc)).ToList();
    }
}