namespace OptionsProvider.Tests;

internal sealed class EntireConfig
{
	public MyConfiguration? Config { get; set; }
	public NonCachedConfiguration? NonCachedConfig { get; set; }
}