namespace Opinionated.MassTransit.Configuration;

public class ConsumerConfiguration : IConsumerConfiguration
{
    public int PrefetchCount { get; set; } = 20;

    public int[] RetryIntervals { get; set; } = { 500, 2000 };

    public int MaxAutoRenewDuration { get; set;  } = 30;

    public int ConcurrentMessageLimit { get; set; } = 10;
}