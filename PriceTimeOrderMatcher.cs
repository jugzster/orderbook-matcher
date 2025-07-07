// Matches orders based on price-time priority
namespace OrderbookMatcher;

public class PriceTimeOrderMatcher : IOrderMatcher
{
    public List<Order> MatchOrders(List<Order> orders)
    {
        // Reset order state and match list
        foreach (var order in orders)
            order.ResetMatchState();

        // Sort orders by price-time priority
        // For buy orders, higher price first
        var buyOrders = orders
            .Where(o => o.Direction == Direction.Buy && o.MatchState != MatchState.InvalidOrder)
            .OrderByDescending(o => o.Notional)
            .ThenBy(o => o.OrderDateTime)
            .ToList();

        // For sell orders, lower price first
        var sellOrders = orders
            .Where(o => o.Direction == Direction.Sell && o.MatchState != MatchState.InvalidOrder)
            .OrderBy(o => o.Notional)
            .ThenBy(o => o.OrderDateTime)
            .ToList();

        // Use queue for O(n) matching, avoids duplicate enumeration
        var sellQueue = new Queue<Order>(sellOrders);

        // Go through each buy order and try to match with sell orders
        foreach (var buyOrder in buyOrders)
        {
            if (buyOrder.RemainingVolume <= 0)
                continue;

            while (sellQueue.Count > 0)
            {
                var sellOrder = sellQueue.Peek();
                if (sellOrder.RemainingVolume <= 0)
                {
                    sellQueue.Dequeue();
                    continue;
                }

                // Only match if buy price is greater than or equal to sell price
                if (buyOrder.Notional >= sellOrder.Notional)
                {
                    // Determine the volume to match
                    var matchVolume = Math.Min(buyOrder.RemainingVolume, sellOrder.RemainingVolume);

                    // Update remaining volumes
                    buyOrder.RemainingVolume -= matchVolume;
                    sellOrder.RemainingVolume -= matchVolume;

                    // Create a match record
                    buyOrder.MatchedOrders.Add(new Match(sellOrder.OrderId, sellOrder.Notional, matchVolume));
                    sellOrder.MatchedOrders.Add(new Match(buyOrder.OrderId, buyOrder.Notional, matchVolume));

                    // If the sell order is fully matched, remove it from the queue
                    if (sellOrder.RemainingVolume == 0)
                        sellQueue.Dequeue();

                    // If the buy order is fully matched, move to the next buy order
                    if (buyOrder.RemainingVolume == 0)
                        break;
                }
                else
                {
                    break; // No more sell orders at acceptable price
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