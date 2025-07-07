namespace OrderbookMatcher;

public interface IOrderMatcher
{
    List<Order> MatchOrders(List<Order> orders);
}