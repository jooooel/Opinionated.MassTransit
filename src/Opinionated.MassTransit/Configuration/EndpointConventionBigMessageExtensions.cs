using System;
using System.Threading.Tasks;
using System.Threading;
using Azure.Identity;
using Azure.Storage.Blobs;
using MassTransit;

namespace Opinionated.MassTransit.Configuration;

public static class EndpointConventionBigMessageExtensions
{
    public static async Task Send<T>(this ISendEndpointProvider provider, string storageAccountName, string storageContainerName, T message, string bigPayload, CancellationToken cancellationToken = default)
        where T : class, IMessageDataContainer
    {
        if (string.IsNullOrEmpty(storageContainerName))
        {
            throw new ArgumentNullException(nameof(storageContainerName));
        }

        var azureStorageUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");

        var client = new BlobServiceClient(azureStorageUri, new ManagedIdentityCredential());
        var messageDataRepository = client.CreateMessageDataRepository(storageContainerName);
        message.Data = await messageDataRepository.PutString(bigPayload, TimeSpan.FromDays(1));

        await provider.Send<T>(message, cancellationToken).ConfigureAwait(false);
    }
}