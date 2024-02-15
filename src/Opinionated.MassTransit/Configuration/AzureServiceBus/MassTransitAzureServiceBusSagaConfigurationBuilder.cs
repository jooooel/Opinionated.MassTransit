using System;
using System.Collections.Generic;
using MassTransit;
using Opinionated.MassTransit.Framework.Events;

namespace Opinionated.MassTransit.Configuration.AzureServiceBus;

public class MassTransitAzureServiceBusSagaConfigurationBuilder : IMassTransitSagaConfigurationBuilder
{
    private readonly string _subscriptionName;
    private readonly IList<Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>> _receiveEndpointConfigurators = new List<Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>>();
    private readonly Func<IBusRegistrationContext, Action<IReceiveEndpointConfigurator>> _subscriptionEndpointConfigurator;

    public MassTransitAzureServiceBusSagaConfigurationBuilder(string subscriptionName, Func<IBusRegistrationContext, Action<IReceiveEndpointConfigurator>> subscriptionEndpointConfigurator)
    {
        _subscriptionName = subscriptionName;
        _subscriptionEndpointConfigurator = subscriptionEndpointConfigurator;
    }

    public IMassTransitSagaConfigurationBuilder WithEvent<TEvent>() where TEvent : class, IEvent
    {
        _receiveEndpointConfigurators.Add((context, factoryConfigurator) =>
            factoryConfigurator.SubscriptionEndpoint<TEvent>(
                ReplaceIllegarChars(_subscriptionName),
                _subscriptionEndpointConfigurator(context)
            )
        );

        return this;
    }

    public IEnumerable<Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>> Build()
    {
        return _receiveEndpointConfigurators;
    }

    private static string ReplaceIllegarChars(string subscriptionName)
    {
        // The saga will consume messages from both a queue and one or more subscriptions.
        // We use the same name for the queue as for the subscription, for example updateservice/some-saga-name.
        // Subscriptions can't contain / so we replace that with a -
        return subscriptionName.Replace("/", "-");
    }
}