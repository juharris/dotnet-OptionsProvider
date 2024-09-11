using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

/// <summary>
/// The converted version of a loaded configuration file.
/// </summary>
public sealed class FeatureConfiguration
{
	/// <summary>
	/// Information about the options for this feature.
	/// </summary>
	public required OptionsMetadata Metadata { get; init; }

	/// <summary>
	/// The configuration.
	/// </summary>
	public required IConfigurationSource Source { get; init; }
}