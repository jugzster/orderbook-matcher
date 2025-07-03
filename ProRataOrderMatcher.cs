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

            // Pro-rata allocation for buys
            foreach (var buy in buys)
            {
                if (buy.RemainingVolume == 0)
                    continue;

                double buyRatio = (double)buy.RemainingVolume / totalBuyVolume;
                // Use Math.Floor to allocate whole shares and to avoid over-allocation if rounding
                int buyShare = (int)Math.Floor(buyRatio * matchVolume);

                foreach (var sell in sells.Where(s => s.RemainingVolume > 0))
                {
                    if (buyShare == 0)
                        break;
                    // Allocate shares to sell orders
                    int sellAllocation = Math.Min(sell.RemainingVolume, buyShare);
                    if (sellAllocation == 0)
                        continue;

                    // Update matched orders
                    buy.RemainingVolume -= sellAllocation;
                    sell.RemainingVolume -= sellAllocation;

                    // Record the match
                    buy.MatchedOrders.Add(new Match(sell.OrderId, notional, sellAllocation));
                    sell.MatchedOrders.Add(new Match(buy.OrderId, notional, sellAllocation));

                    buyShare -= sellAllocation;
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