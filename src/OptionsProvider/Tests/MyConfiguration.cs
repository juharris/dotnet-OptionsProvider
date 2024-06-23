namespace OptionsProvider.Tests;

/// <summary>
/// An example configuration for tests.
/// </summary>
internal class MyConfiguration
{
	public string[]? Array { get; init; }
	public MyObject? Object { get; init; }
}

internal class MyObject
{
	public int One { get; init; }
	public double Two { get; init; }
	public uint? Three { get; init; }
}