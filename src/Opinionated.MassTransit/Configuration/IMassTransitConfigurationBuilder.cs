using System;
using MassTransit;
using Opinionated.MassTransit.Framework.Commands;
using Opinionated.MassTransit.Framework.Events;

namespace Opinionated.MassTransit.Configuration;

public interface IMassTransitConfigurationBuilder
{
    public TransportType TransportType { get; }

    public IMassTransitConfigurationBuilder AddCommandConsumer<TCommand, TConsumer>()
        where TCommand : class, ICommand
        where TConsumer : class, IConsumer<TCommand>;

    public IMassTransitConfigurationBuilder AddBatchCommandConsumer<TCommand, TConsumer>()
        where TCommand : class, ICommand
        where TConsumer : class, IConsumer<Batch<TCommand>>;

    public IMassTransitConfigurationBuilder AddEventConsumer<TEvent, TConsumer>()
        where TEvent : class, IEvent
        where TConsumer : class, IConsumer<TEvent>;

    public IMassTransitConfigurationBuilder AddBatchEventConsumer<TEvent, TConsumer>()
        where TEvent : class, IEvent
        where TConsumer : class, IConsumer<Batch<TEvent>>;

    public IMassTransitConfigurationBuilder AddMessageScheduler();

    public IMassTransitConfigurationBuilder AddSaga<TSagaStateMachineInstance>(
        Action<IBusRegistrationConfigurator> busConfigurator,
        Action<ISagaConfigurator<TSagaStateMachineInstance>> sagaConfigurator,
        Action<IMassTransitSagaConfigurationBuilder> sagaEventConfigurator
    )
        where TSagaStateMachineInstance : class, SagaStateMachineInstance;

    public void Configure();
}