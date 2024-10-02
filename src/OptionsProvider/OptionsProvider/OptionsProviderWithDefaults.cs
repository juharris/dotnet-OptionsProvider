using System.Collections.Immutable;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

internal sealed record class CacheKey(
	string? OptionsKey,
	List<string>? FeatureNames)
{
	public bool Equals(CacheKey? other)
	{
		if (ReferenceEquals(this, other))
		{
			return true;
		}

		if (other is null)
		{
			return false;
		}


		return this.OptionsKey == other.OptionsKey
			// Assume there will usually be features and check for equality first.
			&& ((this.FeatureNames is not null && other.FeatureNames is not null
				&& this.FeatureNames.SequenceEqual(other.FeatureNames))
				// Assume it is rare for there to be no features.
				|| (this.FeatureNames is null && other.FeatureNames is null));
	}

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(this.OptionsKey);
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
	IDictionary<string, string> aliasMapping,
	IDictionary<string, OptionsMetadata> metadataMapping,
	IDictionary<string, IConfigurationSource> sources)
	: IOptionsProvider
{
	public IDictionary<string, string> GetAliasMapping()
	{
		return aliasMapping.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
	}

	public IReadOnlyCollection<string> GetFeatureNames()
	{
		return [.. sources.Keys];
	}

	public IDictionary<string, OptionsMetadata> GetMetadataMapping()
	{
		return metadataMapping.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
	}

	public ICollection<FeatureConfigurationView<T>> GetAllConfigurations<T>()
	{
		return metadataMapping
			.Values
			.Select(metadata=> new FeatureConfigurationView<T>
			{
				Metadata = metadata,
				Configuration = this.GetAllOptions<T>([metadata.Name]),
			})
			.ToArray();
	}

	public T? GetOptions<T>(
		string key,
		IReadOnlyCollection<string>? featureNames = null,
		MemoryCacheEntryOptions? cacheOptions = null)
	{
		var mappedFeatureNames = GetMappedFeatures(aliasMapping, featureNames);

		var cacheKey = new CacheKey(key, mappedFeatureNames);
		return cache.GetOrCreate(cacheKey, entry =>
		{
			if (cacheOptions is not null)
			{
				entry.SetOptions(cacheOptions);
			}

			return this.GetOptionsInternal<T>(key, mappedFeatureNames);
		});
	}

	public T? GetAllOptions<T>(
		IReadOnlyCollection<string>? featureNames = null,
		MemoryCacheEntryOptions? cacheOptions = null)
	{
		var mappedFeatureNames = GetMappedFeatures(aliasMapping, featureNames);

		var cacheKey = new CacheKey(null, mappedFeatureNames);
		return cache.GetOrCreate(cacheKey, entry =>
		{
			if (cacheOptions is not null)
			{
				entry.SetOptions(cacheOptions);
			}

			return this.GetAllOptionsInternal<T>(mappedFeatureNames);
		});
	}

	private static List<string>? GetMappedFeatures(IDictionary<string, string> aliasMapping, IReadOnlyCollection<string>? featureNames)
	{
		// Validate the feature names and map their canonical names.
		List<string>? result = null;
		if (featureNames is not null)
		{
			result = new List<string>(featureNames.Count);
			foreach (var featureName in featureNames)
			{
				if (!aliasMapping.TryGetValue(featureName, out var canonicalFeatureName))
				{
					throw new InvalidOperationException($"The given feature name \"{featureName}\" is not a known feature.");
				}
				result.Add(canonicalFeatureName);
			}
		}

		return result;
	}

	private IConfiguration GetConfig(IReadOnlyCollection<string>? featureNames)
	{
		IConfiguration config = baseConfiguration;
		if (featureNames is not null && featureNames.Count > 0)
		{
			var configurationBuilder = new ConfigurationBuilder()
				// Fallback to the base default configuration.
				.AddConfiguration(baseConfiguration);

			// Apply the features in order so that the last ones takes precedence, just like how when multiple appsettings.json files are used.
			foreach (var featureName in featureNames)
			{
				var source = sources[featureName];
				configurationBuilder.Add(source);
			}

			config = configurationBuilder.Build();
		}

		return config;
	}

	private T? GetAllOptionsInternal<T>(IReadOnlyCollection<string>? featureNames = null)
	{
		var config = this.GetConfig(featureNames);
		return config.Get<T>();
	}

	private T? GetOptionsInternal<T>(string key, IReadOnlyCollection<string>? featureNames = null)
	{
		var config = this.GetConfig(featureNames);
		return config.GetSection(key).Get<T>();
	}
}