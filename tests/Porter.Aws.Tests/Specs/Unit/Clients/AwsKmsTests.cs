using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Microsoft.Extensions.Options;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Clients;
using Porter.Models;

namespace Porter.Aws.Tests.Specs.Unit.Clients;

public class AwsKmsTests : BaseTest
{
    [Test]
    public async Task ShouldGetKey()
    {
        var keyName = faker.Random.Word();
        var keyId = faker.Random.Guid().ToString();

        mocker.Provide(Options.Create(new PorterConfig { PubKey = keyName }));
        var aws = mocker.Generate<AwsKms>();

        var request = new ListAliasesRequest { Limit = 100 };
        var response = new ListAliasesResponse
        {
            Aliases = new List<AliasListEntry> { new() { AliasName = keyName, TargetKeyId = keyId } },
        };

        A.CallTo(() => mocker.Resolve<IAmazonKeyManagementService>()
                .ListAliasesAsync(A<ListAliasesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .Returns(response);

        var result = await aws.GetKey(default);

        result.Should().Be(new AwsKms.KeyId(keyId));
    }

    [Test]
    public async Task ShouldUseCacheAtSecondCall()
    {
        var keyName = faker.Random.Word();
        var keyId = faker.Random.Guid().ToString();

        mocker.Provide(Options.Create(new PorterConfig { PubKey = keyName }));
        var aws = mocker.Generate<AwsKms>();

        var request = new ListAliasesRequest { Limit = 100 };
        var response = new ListAliasesResponse
        {
            Aliases = new List<AliasListEntry> { new() { AliasName = keyName, TargetKeyId = keyId } },
        };

        var service = mocker.Resolve<IAmazonKeyManagementService>();
        A.CallTo(() =>
                service.ListAliasesAsync(A<ListAliasesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .Returns(response);

        await aws.GetKey(default);
        await aws.GetKey(default);

        A.CallTo(() =>
                service.ListAliasesAsync(A<ListAliasesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task CacheShouldBeSameAsTheFirstCall()
    {
        var keyName = faker.Random.Word();
        var keyId = faker.Random.Guid().ToString();

        mocker.Provide(Options.Create(new PorterConfig { PubKey = keyName }));
        var aws = mocker.Generate<AwsKms>();

        var request = new ListAliasesRequest { Limit = 100 };
        var response = new ListAliasesResponse
        {
            Aliases = new List<AliasListEntry> { new() { AliasName = keyName, TargetKeyId = keyId } },
        };

        var service = mocker.Resolve<IAmazonKeyManagementService>();
        A.CallTo(() =>
                service.ListAliasesAsync(A<ListAliasesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .Returns(response);

        var first = await aws.GetKey(default);
        var second = await aws.GetKey(default);

        first.Should().Be(second);
    }
}
