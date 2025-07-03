public class Order
{
    public string CompanyId { get; init; }
    public string OrderId { get; init; }
    public Direction Direction { get; init; }
    public int Volume { get; init; }
    public decimal Notional { get; init; }
    public DateTime OrderDateTime { get; init; }
    public MatchState MatchState { get; set; }
    public int RemainingVolume { get; set; }
    public List<Match> MatchedOrders { get; set; }

    public Order(string companyId, string orderId, Direction direction, int volume, decimal notional,
        DateTime orderDateTime)
    {
        CompanyId = companyId;
        OrderId = orderId;
        Direction = direction;
        Volume = volume;
        Notional = notional;
        OrderDateTime = orderDateTime;
        MatchState = MatchState.Pending;
        RemainingVolume = volume; // Initially, remaining volume is the same as original volume
        MatchedOrders = [];
    }
}
