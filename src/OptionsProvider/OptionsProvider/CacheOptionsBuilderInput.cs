namespace OptionsProvider;

/// <summary>
/// Input to a function to dynamically create <see cref="Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions"/> for <see cref="ServiceCollectionExtensions.ConfigureOptions"/>.
/// </summary>
public sealed class CacheOptionsBuilderInput
{
	/// <summary>
	/// The path to the options in the configurations.
	/// </summary>
	public string? OptionsKey { get; init; }

	/// <summary>
	/// Indicates the features that are enabled for the current scope.
	/// </summary>
	public required IFeaturesContext FeaturesContext { get; init; }
}
