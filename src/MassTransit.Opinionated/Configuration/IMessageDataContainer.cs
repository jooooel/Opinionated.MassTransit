namespace MassTransit.Opinionated.Configuration;

public interface IMessageDataContainer
{
    MessageData<string> Data { get; set; }
}