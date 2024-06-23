﻿using System.Collections.Concurrent;
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
	IDictionary<string, IConfigurationSource> sources,
	Dictionary<string, string> altNameMapping) : IOptionsProvider
{
	// TODO Support configuring the cache with a memory cache or providing an option to disable the cache.
	private readonly ConcurrentDictionary<CacheKey, object?> cache = new();

	public T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames = null)
	{
		// Valid the feature names.
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
		return (T?)this.cache.GetOrAdd(cacheKey, _ => this.GetOptionsInternal<T>(key, mappedFeatureNames));
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