using Microsoft.Extensions.Caching.Memory;

namespace OptionsProvider;

/// <summary>
/// Provides options given enabled feature names.
/// </summary>
public interface IOptionsProvider
{
	/// <returns>
	/// The mapping from alias to feature name.
	/// Keys include the feature names that may be derived from file names.
	/// </returns>
	IDictionary<string, string> GetAliasMapping();

	/// <returns>
	/// The known feature names, but not aliases.
	/// </returns>
	IReadOnlyCollection<string> GetFeatureNames();

	/// <returns>
	/// The metadata for each feature.
	/// </returns>
	IDictionary<string, OptionsMetadata> GetMetadataMapping();

	/// <summary>
	/// Gets the configurations for all features.
	/// </summary>
	/// <typeparam name="T">The type that holds all options. The type of the root of all options.</typeparam>
	/// <returns>Each feature along with its configuration.</returns>
	ICollection<FeatureConfigurationView<T>> GetAllConfigurations<T>();

	/// <summary>
	/// Gets all the options for all keys when the specified features are enabled.
	/// </summary>
	/// <typeparam name="T">The type that holds all options. The type of the root of all options.</typeparam>
	/// <param name="featureNames">
	/// (optional) The abstract names of the scenarios to enable.
	/// Defaults to not using any features, which will yield the default configuration.
	/// </param>
	/// <param name="cacheOptions">
	/// (optional) Options for caching the result given the <paramref name="featureNames"/>.
	/// Defaults to not setting any options for the cache entry.
	/// </param>
	/// <returns>The configuration.</returns>
	T? GetAllOptions<T>(
		IReadOnlyCollection<string>? featureNames = null,
		MemoryCacheEntryOptions? cacheOptions = null);

	/// <summary>
	/// Get the options for a specific key when the specified features are enabled.
	/// </summary>
	/// <typeparam name="T">The type of the options at the key in the configurations.</typeparam>
	/// <param name="key">
	/// The location of the options in the configurations.
	/// </param>
	/// <param name="featureNames">
	/// (optional) The abstract names of the scenarios to enable.
	/// Defaults to not using any features, which will yield the default configuration.
	/// </param>
	/// <param name="cacheOptions">
	/// (optional) Options for caching the result given the <paramref name="key"/> and <paramref name="featureNames"/>.
	/// Defaults to not setting any options for the cache entry.
	/// </param>
	/// <returns>The configuration for the <paramref name="key"/>.</returns>
	T? GetOptions<T>(
		string key,
		IReadOnlyCollection<string>? featureNames = null,
		MemoryCacheEntryOptions? cacheOptions = null);
}