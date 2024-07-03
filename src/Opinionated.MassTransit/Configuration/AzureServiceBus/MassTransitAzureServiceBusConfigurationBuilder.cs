using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opinionated.MassTransit.Framework.Commands;
using Opinionated.MassTransit.Framework.Events;

namespace Opinionated.MassTransit.Configuration.AzureServiceBus;

public class MassTransitAzureServiceBusConfigurationBuilder : IMassTransitConfigurationBuilder
{
    private const int MaxSubscriptionNameLength = 50;

    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly IList<Action<IBusRegistrationConfigurator>> _consumerRegistrations = new List<Action<IBusRegistrationConfigurator>>();
    private readonly IList<Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>> _receiveEndpointConfigurators = new List<Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>>();
    private readonly IList<Action<IBusRegistrationConfigurator>> _sagaRegistrations = new List<Action<IBusRegistrationConfigurator>>();
    private bool _useMessageScheduler;

    public MassTransitAzureServiceBusConfigurationBuilder(IServiceCollection services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public TransportType TransportType => TransportType.AzureServiceBus;

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
        => AddEventConsumerAndSubscriptionEndpointConfiguration<TEvent, TConsumer>(ConfigureConsumer<TConsumer>, _configuration.GetConsumerConfiguration<TConsumer>());

    public IMassTransitConfigurationBuilder AddBatchEventConsumer<TEvent, TConsumer>()
        where TEvent : class, IEvent
        where TConsumer : class, IConsumer<Batch<TEvent>>
        => AddEventConsumerAndSubscriptionEndpointConfiguration<TEvent, TConsumer>(ConfigureBatchConsumer<TConsumer>, _configuration.GetBatchConsumerConfiguration<TConsumer>());

    public IMassTransitConfigurationBuilder AddSaga<TSagaStateMachineInstance>(
        Action<IBusRegistrationConfigurator> busConfigurator,
        Action<ISagaConfigurator<TSagaStateMachineInstance>> sagaConfigurator,
        Action<IMassTransitSagaConfigurationBuilder> sagaEventConfigurator)
        where TSagaStateMachineInstance : class, SagaStateMachineInstance
        => AddSagaAndReceiveEndpointConfiguration(busConfigurator, sagaConfigurator, sagaEventConfigurator, _configuration.GetSagaConsumerConfiguration<TSagaStateMachineInstance>());

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
                busConfigurator.AddServiceBusMessageScheduler();
            }

            foreach (var sagaRegistration in _sagaRegistrations)
            {
                sagaRegistration?.Invoke(busConfigurator);
            }

            foreach (var consumerRegistration in _consumerRegistrations)
            {
                consumerRegistration?.Invoke(busConfigurator);
            }

            busConfigurator.UsingAzureServiceBus((context, serviceBusBusFactoryConfigurator) =>
            {
                var azureStorageUri = _configuration.GetAzureStorageUri();
                var messageDataContainerName = _configuration.GetMessageDataStorageContainerName();
                if (azureStorageUri != null && !string.IsNullOrEmpty(messageDataContainerName))
                {
                    var client = new BlobServiceClient(azureStorageUri, new ManagedIdentityCredential());
                    var azureMessageDataRepository = client.CreateMessageDataRepository(messageDataContainerName);
                    serviceBusBusFactoryConfigurator.UseMessageData(azureMessageDataRepository);
                }

                if (_configuration.EnablePrometheus() ?? true)
                {
                    serviceBusBusFactoryConfigurator.UsePrometheusMetrics();
                }

                serviceBusBusFactoryConfigurator.Host(_configuration.GetConnectionString());

                serviceBusBusFactoryConfigurator.ConfigureJsonSerializerOptions(options =>
                {
                    options.Converters.Insert(0, new JsonStringEnumConverter());
                    return options;
                });

                if (_useMessageScheduler)
                {
                    serviceBusBusFactoryConfigurator.UseDelayedMessageScheduler();
                }

                // Configure receive and subscription endpoints (for queues and topics) if there are any
                // Configure subscription endpoints (for topics) if there are any
                foreach (var receiveEndpointConfigurator in _receiveEndpointConfigurators)
                {
                    receiveEndpointConfigurator?.Invoke(context, serviceBusBusFactoryConfigurator);
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
        IConsumerConfiguration configuration
    )
        where TCommand : class, ICommand
        where TConsumer : class, IConsumer
    {
        var queueName = typeof(TCommand).GetCustomAttribute<EntityNameAttribute>()?.EntityName;
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentException($"{typeof(TCommand).Name} is missing the [EntityName] attribute");
        }

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

    private IMassTransitConfigurationBuilder AddEventConsumerAndSubscriptionEndpointConfiguration<TEvent, TConsumer>(
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

        if (subscriptionName.Length > MaxSubscriptionNameLength)
        {
            throw new ArgumentException($"Invalid {nameof(SubscriptionNameAttribute)}. {subscriptionName} is longer than {MaxSubscriptionNameLength} chars.");
        }

        _consumerRegistrations.Add(x => x.AddConsumer<TConsumer>());
        _receiveEndpointConfigurators.Add((context, factoryConfigurator) =>
            factoryConfigurator.SubscriptionEndpoint<TEvent>(
                subscriptionName,
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
        Action<IMassTransitSagaConfigurationBuilder> sagaEventConfigurator,
        IConsumerConfiguration configuration
    )
        where TSagaStateMachineInstance : class, SagaStateMachineInstance
    {
        var queueName = typeof(TSagaStateMachineInstance).GetCustomAttribute<EntityNameAttribute>()?.EntityName;
        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentException($"{typeof(TSagaStateMachineInstance).Name} is missing the [EntityName] attribute");
        }

        _sagaRegistrations.Add(busConfigurator);

        // This receive endpoint correlates to the "main" queue of the saga.
        // This is where the command that creates the saga is sent.
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

        // We need to setup subscription endpoints for the events that the saga is interested in.
        // These subscription endpoints should have the same configuration as the "main" queue endpoint.
        var sagaBuilder = new MassTransitAzureServiceBusSagaConfigurationBuilder(queueName, context => ConfigureSagaReceiveEndpoint(context, sagaConfigurator, configuration));
        sagaEventConfigurator(sagaBuilder);
        var subscriptionEndpointConfigurators = sagaBuilder.Build();
        foreach (var subscriptionEndpointConfigurator in subscriptionEndpointConfigurators)
        {
            _receiveEndpointConfigurators.Add(subscriptionEndpointConfigurator);
        }

        return this;
    }

    private static Action<IReceiveEndpointConfigurator> ConfigureReceiveEndpoint<TConsumer>(
        IRegistrationContext context,
        Action<IConsumerConfigurator<TConsumer>> consumerConfigurator,
        IConsumerConfiguration configuration
    )
        where TConsumer : class, IConsumer
    {
        return receiveEndpointConfigurator =>
        {
            // Do not publish faults since we do not subscribe to them (they will end up in a new topic + subscription)
            receiveEndpointConfigurator.PublishFaults = false;

            // Receive endpoint (queues) and subscription endpoints (subscriptions on topics) are mostly the same
            // but they differ in some configuration, so switch based on what it is.
            switch (receiveEndpointConfigurator)
            {
                case IServiceBusSubscriptionEndpointConfigurator serviceBusSubscriptionEndpointConfigurator:
                    ConfigureSubscriptionEndpoint(serviceBusSubscriptionEndpointConfigurator, configuration);
                    break;
                case IServiceBusReceiveEndpointConfigurator serviceBusReceiveEndpointConfigurator:
                    ConfigureReceiveEndpoint(serviceBusReceiveEndpointConfigurator, configuration);
                    break;
            }

            // Retries are done in memory, so they cannot bee too long (or Lock would timeout)
            // If longer retries are needed, use redelivery https://masstransit-project.com/usage/exceptions.html#redelivery.
            // I think that retries are best on the endpoint level, instead of on the consumer level. If retried on the consumer
            // level, I think the same consumer instance would be used for all retries, which might not be desired.
            receiveEndpointConfigurator.UseMessageRetry(r => r.Intervals(configuration.RetryIntervals));

            receiveEndpointConfigurator.PrefetchCount = configuration.PrefetchCount;

            receiveEndpointConfigurator.ConfigureConsumer(context, consumerConfigurator);
        };
    }

    /// <summary>
    /// Configure a receive/subscription endpoint for a saga. It should have different defaults than a "normal" receive endpoint,
    /// so make this a separate method.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="sagaConfigurator"></param>
    /// <param name="configuration"></param>
    /// <typeparam name="TSagaStateMachineInstance"></typeparam>
    /// <returns></returns>
    private static Action<IReceiveEndpointConfigurator> ConfigureSagaReceiveEndpoint<TSagaStateMachineInstance>(
        IRegistrationContext context,
        Action<ISagaConfigurator<TSagaStateMachineInstance>> sagaConfigurator,
        IConsumerConfiguration configuration
    )
        where TSagaStateMachineInstance : class, SagaStateMachineInstance
    {
        return receiveEndpointConfigurator =>
        {
            // Do not publish faults since we do not subscribe to them (they will end up in a new topic + subscription)
            receiveEndpointConfigurator.PublishFaults = false;

            // Receive endpoint (queues) and subscription endpoints (subscriptions on topics) are mostly the same,
            // but they differ in some configuration.
            switch (receiveEndpointConfigurator)
            {
                case IServiceBusSubscriptionEndpointConfigurator serviceBusSubscriptionEndpointConfigurator:
                    ConfigureSubscriptionEndpoint(serviceBusSubscriptionEndpointConfigurator, configuration);
                    break;
                case IServiceBusReceiveEndpointConfigurator serviceBusReceiveEndpointConfigurator:
                    ConfigureReceiveEndpoint(serviceBusReceiveEndpointConfigurator, configuration);
                    break;
            }

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

    private static void ConfigureReceiveEndpoint(IServiceBusReceiveEndpointConfigurator receiveEndpointConfigurator, IConsumerConfiguration consumerConfiguration)
    {
        // Put commands that can't be delivered on the DLQ (instead of a new _skipped queue)
        // Example: Sending a message of wrong type to a queue
        receiveEndpointConfigurator.ConfigureDeadLetterQueueDeadLetterTransport();

        // Put commands that have failed on the DLQ (instead of a new _error queue)
        // Example: The consumer has thrown an exception for all retries
        receiveEndpointConfigurator.ConfigureDeadLetterQueueErrorTransport();

        // Commands should have one queue and one consumer. No topic/exchanges.
        receiveEndpointConfigurator.ConfigureConsumeTopology = false;

        receiveEndpointConfigurator.MaxAutoRenewDuration = TimeSpan.FromMinutes(consumerConfiguration.MaxAutoRenewDuration);
    }

    private static void ConfigureSubscriptionEndpoint(IServiceBusSubscriptionEndpointConfigurator subscriptionEndpointConfigurator, IConsumerConfiguration consumerConfiguration)
    {
        subscriptionEndpointConfigurator.MaxAutoRenewDuration = TimeSpan.FromMinutes(consumerConfiguration.MaxAutoRenewDuration);
    }
}