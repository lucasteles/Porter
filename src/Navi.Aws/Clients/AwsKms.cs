using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Microsoft.Extensions.Options;
using Navi.Models;

namespace Navi.Clients;

sealed class AwsKms
{
    readonly NaviConfig config;
    readonly IAmazonKeyManagementService kms;
    KeyId? keyCache;

    public AwsKms(IAmazonKeyManagementService kms, IOptions<NaviConfig> config)
    {
        this.kms = kms;
        this.config = config.Value;
    }

    public async ValueTask<KeyId?> GetKey(CancellationToken ctx)
    {
        if (keyCache is not null)
            return keyCache;

        var aliases = await kms.ListAliasesAsync(new ListAliasesRequest { Limit = 100 }, ctx);
        var key = aliases.Aliases.Find(x => x.AliasName == config.PubKey)?.TargetKeyId;

        if (string.IsNullOrWhiteSpace(key))
            return null;

        keyCache = new(key);
        return keyCache;
    }

    public async Task CreteKey()
    {
        var key = await kms.CreateKeyAsync(new() { Description = "Test key" });
        await kms.CreateAliasAsync(new CreateAliasRequest
        {
            AliasName = config.PubKey,
            TargetKeyId = key.KeyMetadata.KeyId,
        });
    }

    public record struct KeyId(string Value);
}
