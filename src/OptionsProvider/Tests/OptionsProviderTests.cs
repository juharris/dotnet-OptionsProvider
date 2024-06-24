using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OptionsProvider.Tests;

[TestClass]
public class OptionsProviderTests
{
	private static readonly MyConfiguration DefaultMyConfiguration = new()
	{
		Array = ["item 1"],
		Object = new MyObject { One = 1, Two = 2.0 },
	};

	private static readonly MyConfiguration ExampleMyConfiguration = new()
	{
		Array = ["example item 1"],
		Object = new MyObject { One = 1, Two = 2.0 },
	};

	private static readonly MyConfiguration SubExampleMyConfiguration = new()
	{
		Array = ["sub_example item 1", "sub_example item 2"],
		Object = new MyObject { One = 11, Two = 22, Three = 3, },
	};

	[TestMethod]
	public void Test_GetOptions_No_Config()
	{
		var config = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("does not exist");
		Assert.IsNull(config);
	}

	[TestMethod]
	public void Test_GetOptions_without_Features()
	{
		var config = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config");
		Assert.IsNotNull(config);
		config.Should().BeEquivalentTo(DefaultMyConfiguration);
	}

	[TestMethod]
	public void Test_GetOptions_with_Features()
	{
		var config = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["example"]);
		Assert.IsNotNull(config);
		config.Should().BeEquivalentTo(ExampleMyConfiguration);

		config = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["sub_example"]);
		Assert.IsNotNull(config);
		config.Should().BeEquivalentTo(SubExampleMyConfiguration);
	}

	[TestMethod]
	public void Test_GetOptions_For_Deep_Key()
	{
		var array = OptionsLoaderTests.OptionsProvider.GetOptions<string[]>("config:array", ["example"]);
		Assert.IsNotNull(array);
		array.Should().Equal(ExampleMyConfiguration.Array);

		var one = OptionsLoaderTests.OptionsProvider.GetOptions<int>("config:object:one", ["sub_example"]);
		one.Should().Be(SubExampleMyConfiguration.Object!.One);
	}

	[TestMethod]
	public void Test_GetOptions_with_Unknown_Feature()
	{
		var action = () => OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["unknown"]);
		action.Should().Throw<InvalidOperationException>()
			.WithMessage("The given feature name \"unknown\" is not a known feature.");
	}

	[TestMethod]
	public void Test_GetOptions_Same_Instance_without_Features()
	{
		var config1 = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config");
		var config2 = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config");
		Assert.AreSame(config1, config2);
	}

	[TestMethod]
	public void Test_GetOptions_Same_Instance()
	{
		var config1 = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["example"]);
		var config2 = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["eXamPlE"]);
		Assert.AreSame(config1, config2);

		// Test with IOptionsSnapshot.
		// Putting all tests for "example" in one method to avoid concurrency issues.
		{
			using var scope = OptionsLoaderTests.ServiceProvider.CreateScope();
			var featuresContext = scope.ServiceProvider.GetRequiredService<IFeaturesContext>();
			featuresContext.FeatureNames = ["example"];
			var config = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MyConfiguration>>().Value;
			config.Should().BeEquivalentTo(ExampleMyConfiguration);
		}
	}

	[TestMethod]
	public void Test_GetOptions_Same_Instance_With_AlternativeName()
	{
		var config1 = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["subdir/example"]);
		var config2 = OptionsLoaderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["sub_eXaMpLe"]);
		Assert.AreSame(config1, config2);
	}
}