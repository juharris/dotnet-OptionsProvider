using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

internal sealed record class CacheKey(
	string ConfigKey,
	IReadOnlyCollection<string>? FeatureNames)
{
	public bool Equals(CacheKey? other)
	{
		// Assume other is not `null` and they will not be the same reference.
		return this.ConfigKey == other!.ConfigKey
			&& (this.FeatureNames is null && other.FeatureNames is null
			|| (this.FeatureNames is not null && other.FeatureNames is not null
				&& Enumerable.SequenceEqual(this.FeatureNames, other.FeatureNames)));
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
	IDictionary<string, IConfigurationSource> sources) : IOptionsProvider
{
	// TODO Support configuring the cache with a memory cache or providing an option to disable the cache.
	private readonly ConcurrentDictionary<CacheKey, object?> cache = new();

	public T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames = null)
	{
		// TODO Map feature names so that the cache works with alt names.
		var cacheKey = new CacheKey(key, featureNames);
		return (T?)this.cache.GetOrAdd(cacheKey, _ => this.GetOptionsInternal<T>(key, featureNames));
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
			if (sources.TryGetValue(featureName, out var source))
			{
				configurationBuilder.Add(source);
			}
			else
			{
				throw new InvalidOperationException($"Feature '{featureName}' is not a known feature.");
			}
		}

		var configuration = configurationBuilder.Build();
		var result = configuration.GetSection(key).Get<T>();

		return result;
	}
}