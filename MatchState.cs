namespace OrderbookMatcher;

public enum MatchState
{
    Pending, // Initial state
    NoMatch,
    PartialMatch,
    FullMatch,
    InvalidOrder // Zero volume
}
