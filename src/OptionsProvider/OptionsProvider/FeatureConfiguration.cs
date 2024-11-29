using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

/// <summary>
/// The information about a feature to load.
/// </summary>
public sealed class FeatureConfiguration
{
	/// <summary>
	/// General information about the options for this feature.
	/// </summary>
	public required OptionsMetadata Metadata { get; init; }

	/// <summary>
	/// The configuration for the feature.
	/// </summary>
	public required IConfigurationSource Source { get; init; }
}