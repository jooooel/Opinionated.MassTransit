using System;
using System.Linq;
using MassTransit;
using Opinionated.MassTransit.Framework.Commands;

namespace Opinionated.MassTransit.Configuration;

public static class MassTransitBuilderConfigurationExtensions
{
    public static IMassTransitConfigurationBuilder MapEndpointConvention<TCommand>(this IMassTransitConfigurationBuilder builder) where TCommand : class, ICommand
    {
        var queueName = typeof(TCommand)
            .CustomAttributes.FirstOrDefault(attribute => attribute.AttributeType == typeof(EntityNameAttribute))?
            .ConstructorArguments.FirstOrDefault()
            .Value as string;

        if (string.IsNullOrEmpty(queueName))
        {
            throw new ArgumentException($"{typeof(TCommand).Name} is missing the [EntityName] attribute");
        }

        switch(builder.TransportType)
        {
            case TransportType.AzureServiceBus:
                EndpointConvention.Map<TCommand>(new Uri($"queue:{queueName}"));
                break;
            case TransportType.RabbitMq:
                // RabbitMQ doesn't support folders (slashes in the name for queues) like ASB to.
                EndpointConvention.Map<TCommand>(new Uri($"queue:{queueName.Replace("/", "-")}"));
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unknown {nameof(builder.TransportType)} {builder.TransportType}");
        }

        return builder;
    }
}