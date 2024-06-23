using Microsoft.Extensions.Configuration;

namespace OptionsProvider.Tests;

[TestClass]
public class OptionsProviderTests
{
	// Won't be `null` when running tests.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public static IOptionsProvider OptionsProvider { get; private set; }
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
	public void Test_GetOptions_without_Features()
	{
		var config = OptionsProvider.GetOptions<MyConfiguration>("config");
		Assert.IsNotNull(config);
		config.Array.Should().Equal(["item 1"]);
		config.Object.Should().BeEquivalentTo(new MyObject { One = 1, Two = 2.0 });
	}
}