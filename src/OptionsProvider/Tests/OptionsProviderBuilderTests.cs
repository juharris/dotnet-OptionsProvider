using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace OptionsProvider.Tests;

[TestClass]
public sealed class OptionsProviderBuilderTests
{
	// Won't be `null` when running tests.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CA2211 // Non-constant fields should not be visible
	public static IServiceProvider ServiceProvider;
	public static IOptionsProvider OptionsProvider;
#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	[AssemblyInitialize]
	public static void ConfigureOptionsProvider(TestContext _)
	{
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();

		ServiceProvider = new ServiceCollection()
			.AddSingleton<IConfiguration>(configuration)
			.AddOptionsProvider("Configurations")
			.ConfigureOptions<MyConfiguration>("config")
			.ConfigureOptions<NonCachedConfiguration>(
			"nonCachedConfig",
			_ => new MemoryCacheEntryOptions
			{
				AbsoluteExpiration = DateTime.MinValue,
			})
			.BuildServiceProvider(new ServiceProviderOptions()
			{
				ValidateOnBuild = true,
				ValidateScopes = true,
			});
		OptionsProvider = ServiceProvider.GetRequiredService<IOptionsProvider>();
	}

	[AssemblyCleanup]
	public static void CleanupOptionsProvider()
	{
		if (ServiceProvider is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	[TestMethod]
	public async Task Test_AddDirectoryAsync_with_Existing_Name()
	{
		var builder = ServiceProvider.GetRequiredService<IOptionsProviderBuilder>();
		var action = () => builder.AddDirectoryAsync("InvalidConfigurations");
		await action.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage($"Error loading the configuration file at \"{Path.Combine("InvalidConfigurations", "other example.json")}\".")
			.WithInnerException<InvalidOperationException, InvalidOperationException>()
			.WithMessage($"The alias \"other example\" for \"other example\" is already used as an alias or feature name.");
	}

	[TestMethod]
	public void Test_SetConfigurationSource_with_Existing_Name()
	{
		var metadata = new OptionsMetadata
		{
			Name = "name",
			Aliases = ["alias"],
			Owners = "owner",
		};
		var builder = ServiceProvider.GetRequiredService<IOptionsProviderBuilder>();
		var configSource = new MemoryConfigurationSource
		{
			InitialData = new Dictionary<string, string?>
			{
				["key"] = "value",
			},
		};

		var featureConfig = new FeatureConfiguration
		{
			Metadata = metadata,
			Source = configSource,
		};
		var provider = builder
			.SetConfigurationSource(featureConfig)
			.Build();
		var value = provider.GetOptions<string?>("key", ["name"]);
		value.Should().Be("value");
		var value2 = provider.GetOptions<string?>("key", ["alias"]);
		value2.Should().BeSameAs(value);
	}
}