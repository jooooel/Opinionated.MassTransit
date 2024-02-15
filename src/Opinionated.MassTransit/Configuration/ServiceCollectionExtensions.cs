using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opinionated.MassTransit.Configuration.AzureServiceBus;
using Opinionated.MassTransit.Configuration.RabbitMq;

namespace Opinionated.MassTransit.Configuration;

public static class ServiceCollectionExtensions
{
    public static IMassTransitConfigurationBuilder AddMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        var transportType = configuration.GetTransportType();

        return transportType switch
        {
            TransportType.AzureServiceBus => new MassTransitAzureServiceBusConfigurationBuilder(services, configuration),
            TransportType.RabbitMq => new MassTransitRabbitMqConfigurationBuilder(services, configuration),
            _ => throw new ArgumentOutOfRangeException($"Unknown {nameof(transportType)} {transportType}")
        };
    }
}