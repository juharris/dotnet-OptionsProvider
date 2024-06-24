﻿namespace OptionsProvider.Tests;

/// <summary>
/// An example configuration for tests.
/// </summary>
internal class MyConfiguration
{
	public string[]? Array { get; set; }
	public MyObject? Object { get; set; }
}

internal class MyObject
{
	public int One { get; init; }
	public double Two { get; init; }
	public uint? Three { get; init; }
}