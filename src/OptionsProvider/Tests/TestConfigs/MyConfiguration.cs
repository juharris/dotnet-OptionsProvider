namespace OptionsProvider.Tests;

/// <summary>
/// An example configuration for tests.
/// </summary>
internal sealed class MyConfiguration
{
	public string[]? Array { get; set; }
	public MyObject? Object { get; set; }
	public MyDeeperObject? DeeperObject { get; set; }
	public MyDeeperObject[]? DeeperObjects { get; set; }
}

internal sealed class MyObject
{
	public int One { get; init; }
	public double Two { get; init; }
	public uint? Three { get; init; }
}

internal sealed class MyDeeperObject
{
	public string? Name { get; set; }
	public bool IsEnabled { get; set; }
	public MyObject? Object { get; set; }
	public MyDeeperObject[]? Objects { get; set; }
}