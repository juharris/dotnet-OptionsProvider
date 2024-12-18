﻿namespace OptionsProvider;

/// <summary>
/// Input to a function to dynamically create <see cref="Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions"/> for <see cref="ServiceCollectionExtensions.ConfigureOptions"/>.
/// </summary>
public sealed class CacheOptionsBuilderInput
{
	/// <summary>
	/// The path to the options in the configurations.
	/// </summary>
	public required string OptionsKey { get; init; }

	/// <summary>
	/// The features that are enabled for the current scope.
	/// </summary>
	public required IFeaturesContext FeaturesContext { get; init; }
}