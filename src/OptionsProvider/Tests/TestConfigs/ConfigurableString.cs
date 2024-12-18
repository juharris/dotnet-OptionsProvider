namespace OptionsProvider.Tests.TestConfigs;
internal sealed class ConfigurableString
{
	public ConfigurableString(
		string template,
		IReadOnlyDictionary<string, string> values)
	{
		// TODO Use a library to replace the values.
		this.Value = template + values.ToString();
	}

	// TODO Use Lazy?
	public string? Value { get; set; }
}
