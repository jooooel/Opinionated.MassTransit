using System;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace Opinionated.MassTransit.Configuration;

public static class ConfigurationExtensions
{
    private const string ConfigBasePath = "MassTransit";

    public static TransportType? GetTransportType(this IConfiguration configuration)
    {
        const string configPath = $"{ConfigBasePath}:Transport";
        var transportType = configuration.GetValue<TransportType?>(configPath);
        if (transportType == null)
        {
            throw new InvalidOperationException($"No transport configuration for '{configPath}' could be found.");
        }

        return transportType;
    }

    public static string GetConnectionString(this IConfiguration configuration)
    {
        const string configPath = $"{ConfigBasePath}:ConnectionString";
        var connectionString = configuration.GetValue<string>(configPath);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"No connection string configuration for '{configPath}' could be found.");
        }

        return connectionString;
    }

    public static Uri GetAzureStorageUri(this IConfiguration configuration)
    {
        const string configPath = $"{ConfigBasePath}:AzureStorage:AccountName";
        var accountName = configuration.GetValue<string>(configPath);
        return string.IsNullOrEmpty(accountName) ? null : new Uri($"https://{accountName}.blob.core.windows.net");
    }

    public static string GetMessageDataStorageContainerName(this IConfiguration configuration)
    {
        const string configPath = $"{ConfigBasePath}:AzureStorage:MessageDataContainerName";
        return configuration.GetValue<string>(configPath);
    }

    public static bool? EnablePrometheus(this IConfiguration configuration)
    {
        const string configPath = $"{ConfigBasePath}:EnablePrometheusMetrics";
        return configuration.GetValue<bool?>(configPath);
    }

    public static bool WaitUntilStarted(this IConfiguration configuration)
    {
        const string configPath = $"{ConfigBasePath}:WaitUntilStarted";
        return configuration.GetValue<bool?>(configPath) ?? true;
    }

    public static ConsumerConfiguration GetConsumerConfiguration<TConsumer>(this IConfiguration configuration) where TConsumer : class, IConsumer
    {
        var configPath = $"{ConfigBasePath}:ConsumerConfiguration:{typeof(TConsumer).Name}";
        var consumerConfiguration = new ConsumerConfiguration();
        configuration.GetSection(configPath).Bind(consumerConfiguration);
        return consumerConfiguration;
    }

    public static BatchConsumerConfiguration GetBatchConsumerConfiguration<TConsumer>(this IConfiguration configuration) where TConsumer : class, IConsumer
    {
        var configPath = $"{ConfigBasePath}:ConsumerConfiguration:{typeof(TConsumer).Name}";
        var batchConsumerConfiguration = new BatchConsumerConfiguration();
        configuration.GetSection(configPath).Bind(batchConsumerConfiguration);
        return batchConsumerConfiguration;
    }

    public static ConsumerConfiguration GetSagaConsumerConfiguration<TSagaStateMachineInstance>(this IConfiguration configuration) where TSagaStateMachineInstance : class, SagaStateMachineInstance
    {
        var configPath = $"{ConfigBasePath}:ConsumerConfiguration:{typeof(TSagaStateMachineInstance).Name}";
        var consumerConfiguration = new ConsumerConfiguration();
        configuration.GetSection(configPath).Bind(consumerConfiguration);
        return consumerConfiguration;
    }
}