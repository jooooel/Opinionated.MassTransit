namespace Opinionated.MassTransit.Configuration;

public class BatchConsumerConfiguration : IConsumerConfiguration
{
    public int PrefetchCount { get; set; } = 100;

    public int[] RetryIntervals { get; set; } = { 500, 2000 };

    public int MaxAutoRenewDuration { get; } = 30;

    public int MessageLimit { get; set; } = 10;

    public int TimeLimitSeconds { get; set; } = 5;

    public int ConcurrencyLimit { get; set; } = 10;
}