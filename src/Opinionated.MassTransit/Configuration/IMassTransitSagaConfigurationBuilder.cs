using Opinionated.MassTransit.Framework.Events;

namespace Opinionated.MassTransit.Configuration;

public interface IMassTransitSagaConfigurationBuilder
{
    public IMassTransitSagaConfigurationBuilder WithEvent<TEvent>() where TEvent : class, IEvent;
}