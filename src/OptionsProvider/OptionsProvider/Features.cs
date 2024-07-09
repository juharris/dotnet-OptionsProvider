﻿namespace OptionsProvider;

/// <summary>
/// Holds information about the features that are enabled.
/// </summary>
public interface IFeaturesContext
{
	/// <summary>
	/// The enabled feature names.
	/// </summary>
	IReadOnlyList<string>? FeatureNames { get; set; }
}

internal class FeaturesContext : IFeaturesContext
{
	public IReadOnlyList<string>? FeatureNames { get; set; }
}