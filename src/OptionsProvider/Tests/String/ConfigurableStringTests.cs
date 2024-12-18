using OptionsProvider.String;

namespace OptionsProvider.Tests.String;

[TestClass]
public sealed class ConfigurableStringTests
{
	[TestMethod]
	public void ConfigurableStringTest()
	{
		var configurableString = new ConfigurableString("template", new Dictionary<string, string>());

		var actualValue = configurableString.Value.Value;

		Assert.AreEqual("template", actualValue);
	}

	[TestMethod]
	public void ConfigurableString_ShouldThrowException_WhenNullValue()
	{
		var configurableString = new ConfigurableString(null, new Dictionary<string, string>());
		Assert.IsNull(configurableString.Value.Value);
	}

	[TestMethod]
	public void ConfigurableString_ShouldUpdateValue()
	{
		var configurableString = new ConfigurableString("{{1}} {{2}}", new Dictionary<string, string>
		{
			["1"] = "Hello",
			["2"] = "World",
		});

		Assert.AreEqual("Hello World", configurableString.Value.Value);
	}
}
