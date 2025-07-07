1. How much time did you spend on the engineering task?  
   I spent approximately 7-8 hours on the engineering task, including time for analysis, coding, testing, and refactoring.

2. What would you add to your solution if you'd had more time? 

   I would add more unit tests to cover edge cases and ensure accuracy.

   For Pro-Rata algorithm, I would handle case where there are unallocated shares due to rounding down when allocating shares.

   Also, I would add logging to track the allocation process for better debugging, and to see potential bottlenecks in case of large datasets.

3. What do you think is the most useful feature added to the latest version of C#?  

   I think one of the most useful features is the `record` type. Records provide a concise way to define immutable data types, simplifies the creation of data objects, and enhances code readability.

```
public record Match(string OrderId, decimal Notional, int Volume);
```

   Another useful feature is pattern matching, which allows for more concise syntax for checking input, and lends itself well to functional programming.

In `PriceTimeOrderMatcher` and `ProRataOrderMatcher`:
```
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
```

4. How would you track down a performance issue in production?
   I would start by gathering performance metrics and logs to identify the specific area where the performance issue occurs. I would use profiling tools like the Performance Profiler in Visual Studio to analyze CPU and memory usage, and look for bottlenecks in the code. I may also write tests to simulate the production load and to compare performance before and after changes.

   a. Have you ever had to do this?

   Yes, in my most recent work, I investigated the slowness in the trading system screens used heavily by traders. I used Visual Studio's Performance Profiler, particularly the CPU Usage, to find the Hot Paths and longest running functions.
   
   From there I identified the inefficient calculation algorithm and optimized it. I used a Benchmarking tool to measure performance improvements, which was between 7x-200x for large datasets. The performance improvement resulted in noticeable speed improvement in the trading screens.
