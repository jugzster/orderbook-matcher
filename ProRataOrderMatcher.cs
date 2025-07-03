// Matches orders based on pro-rata allocation
public class ProRataOrderMatcher : IOrderMatcher
{
    public List<Order> MatchOrders(List<Order> orders)
    {
        // Calculate based on pro-rata allocation
        // Reset order state and match list
        foreach (var order in orders)
        {
            order.RemainingVolume = order.Volume;
            order.MatchedOrders.Clear();
            order.MatchState = MatchState.Pending;
         
            // Invalid orders
            if (order.Volume <= 0)
                order.MatchState = MatchState.InvalidOrder;
        }

        // Group orders by notional and direction
        var buyGroups = orders
            .Where(o => o.Direction == Direction.Buy && o.MatchState == MatchState.Pending)
            .GroupBy(o => o.Notional)
            .OrderByDescending(g => g.Key); // Highest notional first

        var sellGroups = orders
            .Where(o => o.Direction == Direction.Sell && o.MatchState == MatchState.Pending)
            .GroupBy(o => o.Notional);

        foreach (var buyGroup in buyGroups)
        {
            var notional = buyGroup.Key;
            var matchingSellGroup = sellGroups.FirstOrDefault(g => g.Key == notional);
            if (matchingSellGroup == null)
                continue;

            var buys = buyGroup.ToList();
            var sells = matchingSellGroup.ToList();

            int totalBuyVolume = buys.Sum(b => b.RemainingVolume);
            int totalSellVolume = sells.Sum(s => s.RemainingVolume);
            // Use the minimum of total buy and sell volumes to aoid over-matching
            int matchVolume = Math.Min(totalBuyVolume, totalSellVolume);

            if (matchVolume == 0)
                continue;

            // If buy volume is greater than or equal to sell volume, compute ratio based on buy volume then allocate to sells
            if (totalBuyVolume >= totalSellVolume)
            {
                foreach (var buy in buys)
                {
                    if (buy.RemainingVolume == 0)
                        continue;

                    double ratio = (double)buy.RemainingVolume / totalBuyVolume;
                    // Use Math.Floor to allocate whole shares and to avoid over-allocation if rounding
                    int sharesToAllocate = (int)Math.Floor(ratio * matchVolume);

                    foreach (var sell in sells.Where(s => s.RemainingVolume > 0))
                    {
                        if (sharesToAllocate == 0)
                            break;
                        // Use Math.Min to ensure we don't allocate more than available
                        int allocation = Math.Min(sell.RemainingVolume, sharesToAllocate);
                        if (allocation == 0)
                            continue;

                        // Update matched orders
                        buy.RemainingVolume -= allocation;
                        sell.RemainingVolume -= allocation;

                        // Record the match
                        buy.MatchedOrders.Add(new Match(sell.OrderId, notional, allocation));
                        sell.MatchedOrders.Add(new Match(buy.OrderId, notional, allocation));

                        sharesToAllocate -= allocation;
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

                    double ratio = (double)sell.RemainingVolume / totalSellVolume;
                    // Use Math.Floor to allocate whole shares and to avoid over-allocation if rounding
                    int sharesToAllocate = (int)Math.Floor(ratio * matchVolume);

                    foreach (var buy in buys.Where(s => s.RemainingVolume > 0))
                    {
                        if (sharesToAllocate == 0)
                            break;
                        // Use Math.Min to ensure we don't allocate more than available
                        int allocation = Math.Min(buy.RemainingVolume, sharesToAllocate);
                        if (allocation == 0)
                            continue;

                        // Update matched orders
                        sell.RemainingVolume -= allocation;
                        buy.RemainingVolume -= allocation;

                        // Record the match
                        sell.MatchedOrders.Add(new Match(buy.OrderId, notional, allocation));
                        buy.MatchedOrders.Add(new Match(sell.OrderId, notional, allocation));

                        sharesToAllocate -= allocation;
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