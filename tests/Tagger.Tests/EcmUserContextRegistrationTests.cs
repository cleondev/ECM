using System.Collections.Generic;

using Ecm.Sdk.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Tagger.Configuration;

using Xunit;

namespace Tagger.Tests;

public class EcmUserContextRegistrationTests
{
    [Fact]
    public void AddConfiguredEcmUserContext_SetsManualContextFromConfiguration()
    {
        ManualEcmUserContext.Clear();

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{EcmUserOptions.SectionName}:{nameof(EcmUserOptions.UserKey)}"] = "tagger-user@example.com",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddConfiguredEcmUserContext(configuration);

            using var provider = services.BuildServiceProvider();
            var userContext = provider.GetRequiredService<IEcmUserContext>();

            Assert.True(ManualEcmUserContext.HasUserKey);
            Assert.Equal("tagger-user@example.com", userContext.GetUserKey());
        }
        finally
        {
            ManualEcmUserContext.Clear();
        }
    }

    [Fact]
    public void AddConfiguredEcmUserContext_DefaultsWhenNotConfigured()
    {
        ManualEcmUserContext.Clear();

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();

            var services = new ServiceCollection();
            services.AddConfiguredEcmUserContext(configuration);

            using var provider = services.BuildServiceProvider();
            var userContext = provider.GetRequiredService<IEcmUserContext>();

            Assert.True(ManualEcmUserContext.HasUserKey);
            Assert.Equal(new EcmUserOptions().UserKey, userContext.GetUserKey());
        }
        finally
        {
            ManualEcmUserContext.Clear();
        }
    }
}
