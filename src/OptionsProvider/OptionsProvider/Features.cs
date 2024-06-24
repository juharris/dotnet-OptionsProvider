namespace OptionsProvider;

/// <summary>
/// Holds information about the features that are enabled.
/// </summary>
public interface IFeaturesContext
{
	/// <summary>
	/// The enabled feature names.
	/// </summary>
	string[]? FeatureNames { get; set; }
}

internal class FeaturesContext : IFeaturesContext
{
	public string[]? FeatureNames { get; set; }
}