namespace Opinionated.MassTransit.Configuration;

public interface IConsumerConfiguration
{
    public int PrefetchCount { get; }

    public int[] RetryIntervals { get; }

    /// <summary>
    /// The maximum time in minutes the lock of a message will be extended, before the lock finally is released.
    /// This means that a consumer can not process a message longer than this.
    /// </summary>
    int MaxAutoRenewDuration { get; }
}