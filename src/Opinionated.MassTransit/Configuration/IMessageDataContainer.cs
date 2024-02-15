using MassTransit;

namespace Opinionated.MassTransit.Configuration;

public interface IMessageDataContainer
{
    MessageData<string> Data { get; set; }
}