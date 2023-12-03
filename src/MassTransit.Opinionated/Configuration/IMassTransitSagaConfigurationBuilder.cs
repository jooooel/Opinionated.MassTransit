using MassTransit.Opinionated.Framework.Events;

namespace MassTransit.Opinionated.Configuration;

public interface IMassTransitSagaConfigurationBuilder
{
    public IMassTransitSagaConfigurationBuilder WithEvent<TEvent>() where TEvent : class, IEvent;
}