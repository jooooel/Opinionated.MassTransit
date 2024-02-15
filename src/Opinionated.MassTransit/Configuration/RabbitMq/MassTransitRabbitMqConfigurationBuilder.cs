using System;
using System.Collections.Generic;
using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opinionated.MassTransit.Framework.Commands;
using Opinionated.MassTransit.Framework.Events;

namespace Opinionated.MassTransit.Configuration.RabbitMq;

public class MassTransitRabbitMqConfigurationBuilder : IMassTransitConfigurationBuilder
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly IList<Action<IBusRegistrationConfigurator>> _consumerRegistrations = new List<Action<IBusRegistrationConfigurator>>();
    private readonly IList<Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>> _receiveEndpointConfigurators = new List<Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>>();
    private readonly IList<Action<IBusRegistrationConfigurator>> _sagaRegistrations = new List<Action<IBusRegistrationConfigurator>>();
    private bool _useMessageScheduler;

    public MassTransitRabbitMqConfigurationBuilder(IServiceCollection services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public TransportType TransportType => TransportType.RabbitMq;

    public IMassTransitConfigurationBuilder AddCommandConsumer<TCommand, TConsumer>()
        where TCommand : class, ICommand
        where TConsumer : class, IConsumer<TCommand>
        => AddCommandConsumerAndReceiveEndpointConfiguration<TCommand, TConsumer>(ConfigureConsumer<TConsumer>, _configuration.GetConsumerConfiguration<TConsumer>());

    public IMassTransitConfigurationBuilder AddBatchCommandConsumer<TCommand, TConsumer>()
        where TCommand : class, ICommand
        where TConsumer : class, IConsumer<Batch<TCommand>>
        => AddCommandConsumerAndReceiveEndpointConfiguration<TCommand, TConsumer>(ConfigureBatchConsumer<TConsumer>, _configuration.GetBatchConsumerConfiguration<TConsumer>());

    public IMassTransitConfigurationBuilder AddEventConsumer<TEvent, TConsumer>()
        where TEvent : class, IEvent
        where TConsumer : class, IConsumer<TEvent>
        => AddEventConsumerAndReceiveEndpointConfiguration<TEvent, TConsumer>(ConfigureConsumer<TConsumer>, _configuration.GetConsumerConfiguration<TConsumer>());

    public IMassTransitConfigurationBuilder AddBatchEventConsumer<TEvent, TConsumer>()
        where TEvent : class, IEvent
        where TConsumer : class, IConsumer<Batch<TEvent>>
        => AddEventConsumerAndReceiveEndpointConfiguration<TEvent, TConsumer>(ConfigureBatchConsumer<TConsumer>, _configuration.GetBatchConsumerConfiguration<TConsumer>());

    public IMassTransitConfigurationBuilder AddSaga<TSagaStateMachineInstance>(
        Action<IBusRegistrationConfigurator> busConfigurator,
        Action<ISagaConfigurator<TSagaStateMachineInstance>> sagaConfigurator,
        Action<IMassTransitSagaConfigurationBuilder> sagaEventConfigurator)
        where TSagaStateMachineInstance : class, SagaStateMachineInstance
        => AddSagaAndReceiveEndpointConfiguration(busConfigurator, sagaConfigurator, _configuration.GetSagaConsumerConfiguration<TSagaStateMachineInstance>());

    public IMassTransitConfigurationBuilder AddMessageScheduler()
    {
        _useMessageScheduler = true;
        return this;
    }

    public void Configure()
    {
        _services.AddMassTransit(busConfigurator =>
        {
            if (_useMessageScheduler)
            {
                busConfigurator.AddDelayedMessageScheduler();
            }

            foreach (var sagaRegistration in _sagaRegistrations)
            {
                sagaRegistration?.Invoke(busConfigurator);
            }

            foreach (var consumerRegistration in _consumerRegistrations)
            {
                consumerRegistration?.Invoke(busConfigurator);
            }

            busConfigurator.UsingRabbitMq((context, rabbitMqBusFactoryConfigurator) =>
            {
                rabbitMqBusFactoryConfigurator.Host(_configuration.GetConnectionString());

                if (_useMessageScheduler)
                {
                    rabbitMqBusFactoryConfigurator.UseDelayedMessageScheduler();
                }

                // Configure receive and subscription endpoints (for queues and topics) if there are any
                // Configure subscription endpoints (for topics) if there are any
                foreach (var receiveEndpointConfigurator in _receiveEndpointConfigurators)
                {
                    receiveEndpointConfigurator?.Invoke(context, rabbitMqBusFactoryConfigurator);
                }
            });
        });

        _services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.StopTimeout = TimeSpan.FromMinutes(1);
        });
    }

    private IMassTransitConfigurationBuilder AddCommandConsumerAndReceiveEndpointConfiguration<TCommand, TConsumer>(
        Func<IConsumerConfiguration, Action<IConsumerConfigurator<TConsumer>>> consumerConfigurator,
        IConsumerConfiguration configuration)
        where TCommand : class, ICommand
        where TConsumer : class, IConsumer
    {
        var queueName = typeof(TCommand).GetCustomAttribute<EntityNameAttribute>()?.EntityName;
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentException($"{typeof(TCommand).Name} is missing the [EntityName] attribute");
        }

        // RabbitMQ doesn't support folders (slashes in the name for queues) like ASB to.
        queueName = queueName.Replace("/", "-");

        _consumerRegistrations.Add(x => x.AddConsumer<TConsumer>());
        _receiveEndpointConfigurators.Add((context, factoryConfigurator) =>
            factoryConfigurator.ReceiveEndpoint(
                queueName,
                ConfigureReceiveEndpoint(
                    context,
                    consumerConfigurator(configuration),
                    configuration
                )
            )
        );

        return this;
    }

    private IMassTransitConfigurationBuilder AddEventConsumerAndReceiveEndpointConfiguration<TEvent, TConsumer>(
        Func<IConsumerConfiguration, Action<IConsumerConfigurator<TConsumer>>> consumerConfigurator,
        IConsumerConfiguration configuration
    )
        where TEvent : class, IEvent
        where TConsumer : class, IConsumer
    {
        var topicName = typeof(TEvent).GetCustomAttribute<EntityNameAttribute>()?.EntityName;
        if (string.IsNullOrEmpty(topicName))
        {
            throw new ArgumentException($"{typeof(TEvent).Name} is missing the [EntityName] attribute");
        }

        var subscriptionName = typeof(TConsumer).GetCustomAttribute<SubscriptionNameAttribute>()?.SubscriptionName;
        if (string.IsNullOrEmpty(subscriptionName))
        {
            throw new ArgumentException($"{typeof(TConsumer).Name} is missing the [SubscriptionName] attribute");
        }

        _consumerRegistrations.Add(x => x.AddConsumer<TConsumer>());
        _receiveEndpointConfigurators.Add((context, factoryConfigurator) =>
            factoryConfigurator.ReceiveEndpoint(
                $"{topicName}-{subscriptionName}",
                ConfigureReceiveEndpoint(
                    context,
                    consumerConfigurator(configuration),
                    configuration
                )
            )
        );

        return this;
    }

    private IMassTransitConfigurationBuilder AddSagaAndReceiveEndpointConfiguration<TSagaStateMachineInstance>(
        Action<IBusRegistrationConfigurator> busConfigurator,
        Action<ISagaConfigurator<TSagaStateMachineInstance>> sagaConfigurator,
        IConsumerConfiguration configuration
    )
        where TSagaStateMachineInstance : class, SagaStateMachineInstance
    {
        var queueName = typeof(TSagaStateMachineInstance).GetCustomAttribute<EntityNameAttribute>()?.EntityName;
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentException($"{typeof(TSagaStateMachineInstance).Name} is missing the [EntityName] attribute");
        }

        // RabbitMQ doesn't support folders (slashes in the name for queues) like ASB to.
        queueName = queueName.Replace("/", "-");

        _sagaRegistrations.Add(busConfigurator);

        // This receive endpoint will be used for all messages consumed by the saga.
        // MassTransit will set up exchanges that forward commands and events to this.
        _receiveEndpointConfigurators.Add((context, factoryConfigurator) =>
            factoryConfigurator.ReceiveEndpoint(
                queueName,
                ConfigureSagaReceiveEndpoint(
                    context,
                    sagaConfigurator,
                    configuration
                )
            )
        );

        return this;
    }

    private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureReceiveEndpoint<TConsumer>(
        IRegistrationContext context,
        Action<IConsumerConfigurator<TConsumer>> consumerConfigurator,
        IConsumerConfiguration configuration
    ) where TConsumer : class, IConsumer
    {
        return receiveEndpointConfigurator =>
        {
            // Automatically remove all queued messages when restarting
            receiveEndpointConfigurator.PurgeOnStartup = true;

            // Do not publish faults since we do not subscribe to them
            receiveEndpointConfigurator.PublishFaults = false;

            // Retries are done in memory, so they cannot bee too long (or Lock would timeout)
            // If longer retries are needed, use redelivery https://masstransit-project.com/usage/exceptions.html#redelivery.
            // I think that retries are best on the endpoint level, instead of on the consumer level. If retried on the consumer
            // level, I think the same consumer instance would be used for all retries, which might not be desired.
            receiveEndpointConfigurator.UseMessageRetry(r => r.Intervals(configuration.RetryIntervals));

            receiveEndpointConfigurator.PrefetchCount = configuration.PrefetchCount;

            receiveEndpointConfigurator.ConfigureConsumer(context, consumerConfigurator);
        };
    }

    private static Action<IRabbitMqReceiveEndpointConfigurator> ConfigureSagaReceiveEndpoint<TSagaStateMachineInstance>(
        IRegistrationContext context,
        Action<ISagaConfigurator<TSagaStateMachineInstance>> sagaConfigurator,
        IConsumerConfiguration configuration
    )
        where TSagaStateMachineInstance : class, SagaStateMachineInstance
    {
        return receiveEndpointConfigurator =>
        {
            // Automatically remove all queued messages when restarting
            receiveEndpointConfigurator.PurgeOnStartup = true;

            // Do not publish faults since we do not subscribe to them
            receiveEndpointConfigurator.PublishFaults = false;

            // These settings are based on the recommendations from MassTransit to recude concurrency issues
            // https://masstransit-project.com/usage/sagas/guidance.html

            // This can go up, depending upon the database capacity
            receiveEndpointConfigurator.PrefetchCount = configuration.PrefetchCount;

            // Concurrency issues will throw exceptions, so make sure to retry messages
            receiveEndpointConfigurator.UseMessageRetry(r => r.Intervals(configuration.RetryIntervals));

            // Use the outbox pattern so that messages are sent once the operation completes.
            // No messages will be sent if there are exceptions.
            receiveEndpointConfigurator.UseInMemoryOutbox();

            receiveEndpointConfigurator.ConfigureSaga(context, sagaConfigurator);
        };
    }

    private static Action<IConsumerConfigurator<TConsumer>> ConfigureConsumer<TConsumer>(IConsumerConfiguration configuration)
        where TConsumer : class, IConsumer
    {
        if (!(configuration is ConsumerConfiguration consumerConfiguration))
        {
            throw new InvalidOperationException($"{nameof(IConsumerConfiguration)} is not of type {nameof(ConsumerConfiguration)}.");
        }

        // Configure consumer specific configuration
        return consumerConfigurator =>
        {
            // The default concurrency limit seems to be very high, so set a reasonable number
            consumerConfigurator.UseConcurrentMessageLimit(consumerConfiguration.ConcurrentMessageLimit);
        };
    }

    private static Action<IConsumerConfigurator<TConsumer>> ConfigureBatchConsumer<TConsumer>(IConsumerConfiguration configuration)
        where TConsumer : class, IConsumer
    {
        if (!(configuration is BatchConsumerConfiguration batchConfiguration))
        {
            throw new InvalidOperationException($"{nameof(IConsumerConfiguration)} is not of type {nameof(BatchConsumerConfiguration)}.");
        }

        // Configure batch consumer specific configuration
        return consumerConfigurator =>
        {
            consumerConfigurator.Options<BatchOptions>(batchOptions =>
            {
                batchOptions.MessageLimit = batchConfiguration.MessageLimit;
                batchOptions.TimeLimit = TimeSpan.FromSeconds(batchConfiguration.TimeLimitSeconds);
                batchOptions.ConcurrencyLimit = batchConfiguration.ConcurrencyLimit;
            });
        };
    }
}