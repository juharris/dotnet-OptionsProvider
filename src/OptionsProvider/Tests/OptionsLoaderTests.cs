using Microsoft.Extensions.Configuration;

namespace OptionsProvider.Tests;

[TestClass]
public class OptionsLoaderTests
{
	// Won't be `null` when running tests.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CA2211 // Non-constant fields should not be visible
	public static IOptionsProvider OptionsProvider;
#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


	[AssemblyInitialize]
	public static async Task ConfigureOptionsProvider(TestContext _)
	{
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();

		var loader = new OptionsLoader(configuration);
		OptionsProvider = await loader.LoadAsync("Configurations");
	}

	[TestMethod]
	public async Task Test_LoadAsync_with_Existing_Name()
	{
		var loader = new OptionsLoader(new ConfigurationBuilder().Build());
		var action = () => loader.LoadAsync("InvalidConfigurations");
		await action.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage($"The name \"other example\" for the configuration file \"{Path.Combine("InvalidConfigurations", "other example.json")}\" is already used.");
	}
}