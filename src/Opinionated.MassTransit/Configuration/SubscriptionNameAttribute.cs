using System;

namespace Opinionated.MassTransit.Configuration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class SubscriptionNameAttribute : Attribute
{
    public string SubscriptionName { get; }

    public SubscriptionNameAttribute(string subscriptionName)
    {
        SubscriptionName = subscriptionName;
    }
}