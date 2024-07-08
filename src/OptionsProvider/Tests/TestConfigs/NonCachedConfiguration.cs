namespace OptionsProvider.Tests;

/// <summary>
/// An example configuration for tests.
/// </summary>
internal class NonCachedConfiguration
{
	public string[]? Array { get; set; }
	public MyObject? Object { get; set; }
	public MyDeeperObject? DeeperObject { get; set; }
	public MyDeeperObject[]? DeeperObjects { get; set; }
}