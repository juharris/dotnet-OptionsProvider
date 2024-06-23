using Microsoft.Extensions.Configuration;

namespace OptionsProvider.Tests;

[TestClass]
public class OptionsProviderTests
{
	[TestMethod]
	public async Task Test_OptionsLoader_Async()
	{
		// TODO Make once for all tests.
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();

		var loader = new OptionsLoader(configuration);
		var optionsProvider = await loader.LoadAsync("Configurations");

		var config = optionsProvider.GetOptions<MyConfiguration>("config");
		Assert.IsNotNull(config);
	}
}