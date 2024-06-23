using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

internal sealed record class CacheKey(
	string ConfigKey,
	List<string>? FeatureNames)
{
	public bool Equals(CacheKey? other)
	{
		// Assume other is not `null` and they will not be the same reference.
		return this.ConfigKey == other!.ConfigKey
			// Assume there will usually be features and check for equality first.
			&& ((this.FeatureNames is not null && other.FeatureNames is not null
				&& this.FeatureNames.SequenceEqual(other.FeatureNames))
				// Assume it is rare for there to be no features.
				|| (this.FeatureNames is null && other.FeatureNames is null));
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(this.ConfigKey);
		if (this.FeatureNames is not null)
		{
			foreach (var featureName in this.FeatureNames)
			{
				hash.Add(featureName);
			}
		}
		return hash.ToHashCode();
	}
}

internal sealed class OptionsProviderWithDefaults(
	IConfiguration baseConfiguration,
	IMemoryCache cache,
	IDictionary<string, IConfigurationSource> sources,
	Dictionary<string, string> altNameMapping) : IOptionsProvider
{
	public T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames = null)
	{
		// Valid the feature names and map their canonical names.
		List<string>? mappedFeatureNames = null;
		if (featureNames is not null)
		{
			mappedFeatureNames = new List<string>(featureNames.Count);
			foreach (var featureName in featureNames)
			{
				if (!altNameMapping.TryGetValue(featureName, out string? canonicalFeatureName))
				{
					throw new InvalidOperationException($"The given feature name \"{featureName}\" is not a known feature.");
				}
				mappedFeatureNames.Add(canonicalFeatureName);
			}
		}

		var cacheKey = new CacheKey(key, mappedFeatureNames);
		return (T?)cache.GetOrCreate(cacheKey, entry => {
			// TODO entry.SetOptions(options);
			return this.GetOptionsInternal<T>(key, mappedFeatureNames);
		});
	}

	private T? GetOptionsInternal<T>(string key, IReadOnlyCollection<string>? featureNames = null)
	{
		if (featureNames is null || featureNames.Count == 0)
		{
			return baseConfiguration.GetSection(key).Get<T>();
		}

		var configurationBuilder = new ConfigurationBuilder()
			// Fallback to the base default configuration.
			.AddConfiguration(baseConfiguration);

		// Apply the features in order so that the last ones takes precedence, just like how when multiple appsettings.json files are used.
		foreach (var featureName in featureNames)
		{
			var source = sources[featureName];
			configurationBuilder.Add(source);
		}

		var configuration = configurationBuilder.Build();
		var result = configuration.GetSection(key).Get<T>();

		return result;
	}
}