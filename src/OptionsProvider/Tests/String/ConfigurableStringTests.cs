using OptionsProvider.String;

namespace OptionsProvider.Tests.String;

[TestClass]
public sealed class ConfigurableStringTests
{

	[DataRow("Hello World", "|1| |2|")]
	[DataRow("Hello World", "|1 and 2|")]
	[DataRow("Hello2|", "|1|2|")]
	[TestMethod]
	public void Test_ConfigurableString_CustomDelimiters(string expected, string template)
	{
		var configurableString = new ConfigurableString
		{
			Template = template,
			Values = new Dictionary<string, string?>
			{
				["1"] = "Hello",
				["2"] = "World",
				["1 and 2"] = "|1| |2|",
			},
			StartDelimiter = "|",
			EndDelimiter = "|",
		};

		Assert.AreEqual(expected, configurableString.Value);
	}

	[TestMethod]
	public void Test_ConfigurableString_FromString()
	{
		const string expected = "wtv {{slot}} hey";
		ConfigurableString configurableString = expected;
		Assert.AreEqual(expected, configurableString.Value);
	}

	[TestMethod]
	public void Test_ConfigurableString_NullValues()
	{
		var configurableString = new ConfigurableString
		{
			Template = "{{1}} {{2}}",
			Values = null,
		};
		Assert.AreEqual("{{1}} {{2}}", configurableString.Value);
	}

	[TestMethod]
	public void Test_ConfigurableString_TooManyIterations()
	{
		var configurableString = new ConfigurableString
		{
			Template = "{{1}} {{2}}",
			Values = new Dictionary<string, string?>
			{
				["1"] = "{{2}}",
				["2"] = "{{1}}",
			},
		};
		var exception = Assert.ThrowsException<InvalidOperationException>(() => configurableString.Value);
		Assert.AreEqual("The replacement loop count exceeded the maximum allowed iterations (10000). There was likely a recursive loop using the template and values.", exception.Message);
	}

	[DataRow(null, null)]
	[DataRow("", "")]
	[DataRow("Hello World", "Hello World")]
	[DataRow("Hello World", "{{1}} {{2}}")]
	[DataRow("Hello World {{3}}", "{{1}} {{2}} {{3}}")]
	[DataRow("{{3}} Hello World", "{{3}} {{1}} {{2}}")]
	[DataRow("{{no mapping}} World", "{{no mapping}} {{2}}")]
	[DataRow("{{null}} World", "{{null}} {{2}}")]
	[DataRow("Hello World", "{{1 and 2}}")]
	[DataRow("Hello World Hello World", "{{1 and 2}} {{1 and 2}}")]
	[DataRow("|Hello World| {{1}", "|{{1 and 2}}| {{1}")]
	[DataRow("Hello}", "{{1}}}")]
	[DataRow("{Hello}", "{{{1}}}")]
	[DataRow("{{BRACES}}", "{{braces}}")]
	[DataRow("{{wtvWorld}}", "{{wtv{{2}}}}")]
	[DataRow("One World", "{{1{{2}}}}")]
	[DataRow("This is a long string that someone might build in a typical example for displaying in their application or logging.", "{{empty}}{{first}}{{}}{{rest}}{{}}{{.}}{{}}")]
	[TestMethod]
	public void Test_ConfigurableString(string expected, string template)
	{
		var configurableString = new ConfigurableString
		{
			Template = template,
			Values = new Dictionary<string, string?>
			{
				["1"] = "Hello",
				["2"] = "World",
				["1World"] = "One World",
				["1 and 2"] = "{{1}} {{2}}",
				["braces"] = "{{BRACES}}",
				["1{{2}}"] = "{{1{{2}}}}",
				["verb"] = "build",
				["noun "] = "long string ",
				[string.Empty] = string.Empty,
				["empty"] = string.Empty,
				[" "] = " ",
				["space"] = " ",
				["null"] = null,
				["first"] = "This is a {{noun }}that someone might {{verb}} in a {{empty}}typical{{space}}example",
				["rest"] = " for{{ }}displaying in their appli{{}}cation or logging",
				["."] = ".",
			},
		};

		Assert.AreEqual(expected, configurableString.Value);
	}
}