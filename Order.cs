namespace OrderbookMatcher;

public class Order(string companyId, string orderId, Direction direction, int volume, decimal notional,
    DateTime orderDateTime)
{
    public string CompanyId { get; init; } = companyId;
    public string OrderId { get; init; } = orderId;
    public Direction Direction { get; init; } = direction;
    public int Volume { get; init; } = volume;
    public decimal Notional { get; init; } = notional;
    public DateTime OrderDateTime { get; init; } = orderDateTime;
    public MatchState MatchState { get; set; } = MatchState.Pending;
    public int RemainingVolume { get; set; } = volume; // Initially, remaining volume is the same as original volume
    public List<Match> MatchedOrders { get; set; } = [];

    public void ResetMatchState()
    {
        RemainingVolume = Volume;
        MatchedOrders.Clear();
        MatchState = MatchState.Pending;

        if (Volume <= 0)
            MatchState = MatchState.InvalidOrder;
    }
}
