namespace OptionsProvider;

/// <summary>
/// The configuration for an existing feature.
/// </summary>
/// <typeparam name="T">The type that holds all options. The type of the root of all options.</typeparam>
/// <remarks>
/// Mainly for <see cref="IOptionsProvider.GetAllOptionsForAllFeatures{T}"/>
/// </remarks>
public sealed class FeatureInfo<T>
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