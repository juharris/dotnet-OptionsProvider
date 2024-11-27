namespace OptionsProvider.Tests.TestConfigs;

internal sealed class EntireConfig
{
	public MyConfiguration? Config { get; set; }
	public NonCachedConfiguration? NonCachedConfig { get; set; }
}