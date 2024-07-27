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

internal enum MyEnum
{
	Default,
	Second,
}

internal sealed class MyObject
{
	public int One { get; init; }
	public double Two { get; init; }
	public uint? Three { get; init; }
	public MyEnum MyEnum { get; init; }
}

internal sealed class MyDeeperObject
{
	public string? Name { get; init; }
	public bool IsEnabled { get; init; }
	public MyObject? Object { get; init; }
	public MyDeeperObject[]? Objects { get; init; }
}