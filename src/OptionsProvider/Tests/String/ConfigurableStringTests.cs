using OptionsProvider.String;

namespace OptionsProvider.Tests.String;

[TestClass]
public sealed class ConfigurableStringTests
{
	[DataRow(null, null)]
	[DataRow("", "")]
	[DataRow("Hello World", "Hello World")]
	[DataRow("Hello World", "{{1}} {{2}}")]
	[DataRow("Hello World {{3}}", "{{1}} {{2}} {{3}}")]
	[DataRow("{{3}} Hello World", "{{3}} {{1}} {{2}}")]
	[DataRow("Hello World", "{{1 and 2}}")]
	[DataRow("Hello World Hello World", "{{1 and 2}} {{1 and 2}}")]
	[DataRow("|Hello World| {{1}", "|{{1 and 2}}| {{1}")]
	[DataRow("Hello}", "{{1}}}")]
	// TODO Think about escaping or how to get raw braces.
	[DataRow("{{{1}}}", "{{{1}}}")]
	[TestMethod]
	public void ConfigurableString_Test(string expected, string template)
	{
		var configurableString = new ConfigurableString(template, new Dictionary<string, string>
		{
			["1"] = "Hello",
			["2"] = "World",
			["1 and 2"] = "{{1}} {{2}}",
		});

		Assert.AreEqual(expected, configurableString.Value);
	}

	[DataRow("Hello World", "|1| |2|")]
	[TestMethod]
	public void ConfigurableString_CustomDelimiters_Test(string expected, string template)
	{
		var configurableString = new ConfigurableString(
			template, new Dictionary<string, string>
			{
				["1"] = "Hello",
				["2"] = "World",
				["1 and 2"] = "|1| |2|",
			},
			"|",
			"|");

		Assert.AreEqual(expected, configurableString.Value);
	}

	[TestMethod]
	public void ConfigurableString_TooManyIterations_Test()
	{
		var configurableString = new ConfigurableString("{{1}} {{2}}", new Dictionary<string, string>
		{
			["1"] = "{{2}}",
			["2"] = "{{1}}",
		});
		var exception = Assert.ThrowsException<InvalidOperationException>(() => configurableString.Value);
		Assert.AreEqual("The replacement loop count exceeded the maximum allowed iterations (10000). There was likely a recursive loop using the template and values.", exception.Message);
	}
}
