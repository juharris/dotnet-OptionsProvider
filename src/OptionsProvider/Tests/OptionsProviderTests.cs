using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OptionsProvider.Tests;

[TestClass]
public sealed class OptionsProviderTests
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
	public void Test_GetAliasMapping()
	{
		var aliases = OptionsProviderBuilderTests.OptionsProvider.GetAliasMapping();
		aliases.Should().BeAssignableTo<ImmutableDictionary<string, string>>();
		aliases.Should().BeEquivalentTo(new Dictionary<string, string>
		{
			["deeper"] = "deeper_example",
			["deeper_example"] = "deeper_example",
			["deeper_example2"] = "deeper_example2",
			["deeper2"] = "deeper_example2",
			["example"] = "example",
			["sub_example"] = "subdir/example",
			["subdir/example"] = "subdir/example",
		});

		aliases.Should().ContainKey("DeePeR");
	}

	[TestMethod]
	public void Test_GetFeatureNames()
	{
		var featureNames = OptionsProviderBuilderTests.OptionsProvider.GetFeatureNames();
		featureNames.Should().BeAssignableTo<ImmutableArray<string>>();
		featureNames.Should().BeEquivalentTo(["deeper_example", "deeper_example2", "example", "subdir/example"]);
	}

	[TestMethod]
	public void Test_GetMetadata()
	{
		var metadatas = OptionsProviderBuilderTests.OptionsProvider.GetMetadataMapping();
		metadatas.Should().BeAssignableTo<ImmutableDictionary<string, OptionsMetadata>>();
		metadatas.Should().ContainKey("deeper_example");
		metadatas.Should().ContainKey("subdir/example");
		var metadata = metadatas["subdir/example"];
		metadata.BestBeforeDate.Should().Be(new DateTime(2029, 11, 29));
		metadata.OtherInfo.ToString().Should().BeEquivalentTo(@"{""custom"": ""info""}");
	}

	[TestMethod]
	public void Test_GetOptions_No_Config()
	{
		var config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("does not exist");
		Assert.IsNull(config);
	}

	[TestMethod]
	public void Test_GetOptions_without_Features()
	{
		var config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config");
		Assert.IsNotNull(config);
		config.Should().BeEquivalentTo(DefaultMyConfiguration);
	}

	[TestMethod]
	public void Test_GetOptions_with_Features()
	{
		var config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["example"]);
		Assert.IsNotNull(config);
		config.Should().BeEquivalentTo(ExampleMyConfiguration);

		config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["sub_example"]);
		Assert.IsNotNull(config);
		config.Should().BeEquivalentTo(SubExampleMyConfiguration);
	}

	[TestMethod]
	public void Test_GetOptions_For_Deep_Key()
	{
		var array = OptionsProviderBuilderTests.OptionsProvider.GetOptions<string[]>("config:array", ["example"]);
		Assert.IsNotNull(array);
		array.Should().Equal(ExampleMyConfiguration.Array);

		var one = OptionsProviderBuilderTests.OptionsProvider.GetOptions<int>("config:object:one", ["sub_example"]);
		one.Should().Be(SubExampleMyConfiguration.Object!.One);
	}

	[TestMethod]
	public void Test_GetOptions_with_Unknown_Feature()
	{
		var action = () => OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["unknown"]);
		action.Should().Throw<InvalidOperationException>()
			.WithMessage("The given feature name \"unknown\" is not a known feature.");
	}

	[TestMethod]
	public void Test_GetOptions_Same_Instance_without_Features()
	{
		var config1 = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config");
		var config2 = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config");
		Assert.AreSame(config1, config2);
	}

	[TestMethod]
	public void Test_GetOptions_Same_Instance()
	{
		var config1 = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["example"]);
		var config2 = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["eXamPlE"]);
		Assert.AreSame(config1, config2);

		// Test with IOptionsSnapshot.
		// Putting all tests for "example" in one method to avoid concurrency issues.
		MyConfiguration scope1Config, scope2Config;
		{
			using var scope = OptionsProviderBuilderTests.ServiceProvider.CreateScope();
			var featuresContext = scope.ServiceProvider.GetRequiredService<IFeaturesContext>();
			featuresContext.FeatureNames = ["example"];
			scope1Config = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MyConfiguration>>().Value;
			scope1Config.Should().BeEquivalentTo(ExampleMyConfiguration);
		}

		{
			using var scope = OptionsProviderBuilderTests.ServiceProvider.CreateScope();
			var featuresContext = scope.ServiceProvider.GetRequiredService<IFeaturesContext>();
			featuresContext.FeatureNames = ["eXamplE"];
			scope2Config = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<MyConfiguration>>().Value;
			scope2Config.Should().BeEquivalentTo(ExampleMyConfiguration);
		}

		// scope1Config and scope2Config won't be the same instance because the Options pattern .NET logic creates a new instance for each scope.
		Assert.AreNotSame(scope1Config, scope2Config);
		foreach (var prop in scope1Config.GetType().GetProperties())
		{
			Assert.AreSame(prop.GetValue(scope1Config), prop.GetValue(scope2Config));
		}
	}

	[TestMethod]
	public void Test_GetOptions_Same_Instance_With_AlternativeName()
	{
		var config1 = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["subdir/example"]);
		var config2 = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["sub_eXaMpLe"]);
		Assert.AreSame(config1, config2);
	}

	[TestMethod]
	public void Test_GetOptions_Example_Sub_Example()
	{
		var config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["example", "subdir/example"]);
		config.Should().BeEquivalentTo(SubExampleMyConfiguration);
	}

	[TestMethod]
	public void Test_GetOptions_Sub_Example_Example()
	{
		var expected = new MyConfiguration()
		{
			Array = ["example item 1", "sub_example item 2"],
			Object = new MyObject { One = 11, Two = 22, Three = 3 },
		};
		var config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["sub_example", "example"]);
		config.Should().BeEquivalentTo(expected);
	}

	[TestMethod]
	public void Test_GetOptions_Not_Cached()
	{
		// Test with IOptionsSnapshot.
		NonCachedConfiguration scope1Config, scope2Config;
		{
			using var scope = OptionsProviderBuilderTests.ServiceProvider.CreateScope();
			scope1Config = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<NonCachedConfiguration>>().Value;
			scope1Config.Should().BeEquivalentTo(DefaultMyConfiguration);
		}

		{
			using var scope = OptionsProviderBuilderTests.ServiceProvider.CreateScope();
			scope2Config = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<NonCachedConfiguration>>().Value;
			scope2Config.Should().BeEquivalentTo(DefaultMyConfiguration);
		}

		// scope1Config and scope2Config won't be the same instance because the Options pattern .NET logic creates a new instance for each scope.
		Assert.AreNotSame(scope1Config, scope2Config);
		foreach (var prop in scope1Config.GetType().GetProperties())
		{
			var val1 = prop.GetValue(scope1Config);
			var val2 = prop.GetValue(scope2Config);
			if (val1 is not null)
			{
				Assert.IsNotNull(val2);
				Assert.AreNotSame(val1, val2, "Values for `{0}` were not the same instance.", prop.Name);
			}
			else
			{
				Assert.IsNull(val2);
			}

		}
	}

	[TestMethod]
	public void Test_GetOptions_Deeper()
	{
		var expectedDeeper = new MyConfiguration()
		{
			Array = ["item 1"],
			Object = new MyObject { One = 1, Two = 2.0 },
			DeeperObject = new()
			{
				Name = "wtv",
				IsEnabled = true,
				Object = new MyObject { One = 1, Three = 3 },
				Objects =
				[
					new()
					{
						Name = "obj1",
						IsEnabled = true,
						Object = new MyObject { One = 1, Two = 2.0 },
					},
					new()
					{
						Name = "obj2",
						IsEnabled = false,
						Object = new MyObject { One = 1, Three = 3 },
					},
				]
			},
			DeeperObjects = [
				new()
				{
					Name = "obj A",
					IsEnabled = true,
					Object = new MyObject { One = 11, Two = 22 },
				},
				new()
				{
					Name = "obj B",
					IsEnabled = false,
					Object = new MyObject { One = 1, Three = 3 },
				},
			],
		};
		var config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["deePer"]);
		config.Should().BeEquivalentTo(expectedDeeper);

		var expectedDeeper2 = new MyConfiguration()
		{
			Array = ["item 1"],
			Object = new MyObject { One = 1, Two = 2.0 },
			DeeperObject = new()
			{
				Name = "wtv",
				IsEnabled = true,
				Object = new MyObject { One = 1, Three = 33 },
				Objects =
				[
					new()
					{
						Name = "obj1",
						IsEnabled = false,
						Object = new MyObject { One = 11, Two = 2.0 },
					},
					new()
					{
						Name = "obj 2 2",
						IsEnabled = false,
						Object = new MyObject { One = 1, Three = 3 },
					},
				]
			},
			DeeperObjects = [
				new()
				{
					Name = "obj A 2",
					IsEnabled = false,
					Object = new MyObject { One = 111, Two = 22, Three = 333 },
				},
				new()
				{
					Name = "obj B",
					IsEnabled = false,
					Object = new MyObject { One = 1, Three = 3 },
				},
			],
		};
		config = OptionsProviderBuilderTests.OptionsProvider.GetOptions<MyConfiguration>("config", ["deePer", "DEEPER2"]);
		config.Should().BeEquivalentTo(expectedDeeper2);
	}
}