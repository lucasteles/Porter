using Amazon.KeyManagementService;
using Microsoft.Extensions.Options;
using Porter.Models;

namespace Porter.Clients;

sealed class AwsKms
{
    readonly PorterConfig config;
    readonly IAmazonKeyManagementService kms;
    KeyId? keyCache;

    public AwsKms(IAmazonKeyManagementService kms, IOptions<PorterConfig> config)
    {
        this.kms = kms;
        this.config = config.Value;
    }

    public async ValueTask<KeyId?> GetKey(CancellationToken ct)
    {
        if (keyCache is not null)
            return keyCache;

        var aliases = await kms.ListAliasesAsync(new() { Limit = 100 }, ct);
        var key = aliases.Aliases.Find(x => x.AliasName == config.PubKey)?.TargetKeyId;

        if (string.IsNullOrWhiteSpace(key))
            return null;

        keyCache = new(key);
        return keyCache;
    }

    public async Task CreteKey()
    {
        var key = await kms.CreateKeyAsync(new() { Description = "Test key" });
        await kms.CreateAliasAsync(new()
        {
            AliasName = config.PubKey,
            TargetKeyId = key.KeyMetadata.KeyId,
        });
    }

    public record struct KeyId(string Value);
}
