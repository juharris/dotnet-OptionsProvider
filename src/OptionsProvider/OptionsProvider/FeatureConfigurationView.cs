namespace OptionsProvider;

/// <summary>
/// A view of the configuration for a feature.
/// </summary>
public sealed class FeatureConfigurationView<T>
{
	/// <summary>
	/// The metadata for the feature.
	/// </summary>
	public required OptionsMetadata Metadata { get; init; }

	/// <summary>
	/// The configuration for the feature.
	/// </summary>
	public required T? Configuration { get; init; }
}