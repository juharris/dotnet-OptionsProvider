namespace OptionsProvider;

/// <summary>
/// Holds information about the features that are enabled.
/// </summary>
public interface IFeaturesContext
{
	/// <summary>
	/// The names of the currently enabled features.
	/// </summary>
	IReadOnlyList<string>? FeatureNames { get; set; }
}

internal sealed class FeaturesContext : IFeaturesContext
{
	public IReadOnlyList<string>? FeatureNames { get; set; }
}