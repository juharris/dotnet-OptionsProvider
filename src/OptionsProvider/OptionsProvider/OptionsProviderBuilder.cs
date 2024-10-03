using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace OptionsProvider;

/// <summary>
/// Simple options loader.
/// </summary>
internal sealed class OptionsProviderBuilder(
	IConfiguration baseConfiguration,
	IMemoryCache cache)
	: IOptionsProviderBuilder
{
	private static readonly JsonSerializerOptions DeserializationOptions = new()
	{
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};

	private static readonly YamlDotNet.Serialization.IDeserializer YamlDeserializer
		= new YamlDotNet.Serialization.DeserializerBuilder()
					.WithAttemptingUnquotedStringTypeDeserialization()
					.Build();
	private static readonly YamlDotNet.Serialization.ISerializer YamlToJsonSerializer
		= new YamlDotNet.Serialization.SerializerBuilder()
					.JsonCompatible()
					.Build();

	/// <summary>
	/// A mapping from alias to feature name.
	/// </summary>
	private readonly Dictionary<string, string> aliasMapping = new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, OptionsMetadata> metadataMapping = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// A mapping from feature name to the configuration for the feature.
	/// </summary>
	private readonly Dictionary<string, IConfigurationSource> sourcesMapping = new(StringComparer.OrdinalIgnoreCase);

	public IOptionsProviderBuilder AddAlias(string alias, string featureName)
	{
		if (!this.aliasMapping.TryAdd(alias, featureName))
		{
			throw new InvalidOperationException($"The alias \"{alias}\" for \"{featureName}\" is already used as an alias or feature name.");
		}

		// TODO Update metadata? It would make `AddConfigurationSource` less efficient.

		return this;
	}

	public IOptionsProviderBuilder AddConfigurationSource(FeatureConfiguration featureConfiguration)
	{
		var metadata = featureConfiguration.Metadata;
		var featureName = metadata.Name;

		// Provide a canonical case-insensitive name for the configuration which also simplifies mapping alternative names to the canonical name.
		this.AddAlias(featureName, featureName);
		if (metadata.Aliases is not null)
		{
			foreach (var alias in metadata.Aliases)
			{
				this.AddAlias(alias, featureName);
			}
		}

		var configurationSource = featureConfiguration.Source;
		if (!this.sourcesMapping.TryAdd(featureName, configurationSource))
		{
			throw new InvalidOperationException($"The feature name \"{featureName}\" already has a mapped configuration.");
		}

		this.metadataMapping[featureName] = metadata;

		return this;
	}

	public async Task<IOptionsProviderBuilder> AddDirectoryAsync(string rootPath)
	{
		var paths = Directory.EnumerateFiles(rootPath, "*.json", SearchOption.AllDirectories)
			.Concat(Directory.EnumerateFiles(rootPath, "*.yaml", SearchOption.AllDirectories))
			.Concat(Directory.EnumerateFiles(rootPath, "*.yml", SearchOption.AllDirectories))
			// Ensure that the files are loaded in a consistent order so that errors are consistent on different machines.
			.Order(StringComparer.Ordinal);

		// Use async tasks to load files in parallel.
		var fileConfigs = await Task.WhenAll(paths
			.Select(filePath => LoadFileAsync(rootPath, filePath))
			.ToArray());
		foreach (var (configPath, fileConfig) in paths.Zip(fileConfigs))
		{
			try
			{
				this.AddConfigurationSource(fileConfig);
			}
			catch (InvalidOperationException exc)
			{
				throw new InvalidOperationException($"Error loading the configuration file at \"{configPath}\".", exc);
			}
		}

		return this;
	}

	public IOptionsProvider Build()
	{
		return new OptionsProviderWithDefaults(baseConfiguration, cache, this.aliasMapping, this.metadataMapping, this.sourcesMapping);
	}

	public IOptionsProviderBuilder SetAlias(string alias, string featureName)
	{
		this.aliasMapping[alias] = featureName;
		return this;
	}

	public IOptionsProviderBuilder SetConfigurationSource(FeatureConfiguration featureConfiguration)
	{
		var metadata = featureConfiguration.Metadata;
		var featureName = metadata.Name;

		// Provide a canonical case-insensitive name for the configuration which also simplifies mapping alternative names to the canonical name.
		this.SetAlias(featureName, featureName);
		if (metadata.Aliases is not null)
		{
			foreach (var alias in metadata.Aliases)
			{
				this.SetAlias(alias, featureName);
			}
		}

		var configurationSource = featureConfiguration.Source;
		this.sourcesMapping[featureName] = configurationSource;
		this.metadataMapping[featureName] = metadata;
		return this;
	}

	private static async Task<FeatureConfiguration> LoadFileAsync(string rootPath, string path)
	{
		try
		{
			OptionsFileSchema parsedContents;
			using var stream = File.OpenRead(path);
			if (path.EndsWith(".json"))
			{
				parsedContents = (await JsonSerializer.DeserializeAsync<OptionsFileSchema>(stream, DeserializationOptions))!;
			}
			else
			{
				// Assume YAML.
				// Convert to JSON so that we can use `JsonElement` just like with JSON files.
				// Maybe some parts of this should be optimized and tweaked to handle other cases, but it seems fine for now.
				using var reader = new StreamReader(stream);
				var yamlObject = YamlDeserializer.Deserialize(reader);
				var contents = YamlToJsonSerializer.Serialize(yamlObject);
				parsedContents = JsonSerializer.Deserialize<OptionsFileSchema>(contents, DeserializationOptions)!;
			}

			// Remove the extensions and convert the path to a relative path from `rootPath`.
			parsedContents.Metadata.Name = Path
					.ChangeExtension(Path.GetRelativePath(rootPath, path), null)
					.Replace(Path.DirectorySeparatorChar, '/');
			return new()
			{
				Metadata = parsedContents.Metadata,

				Source = new MemoryConfigurationSource
				{
					InitialData = BuildOptionsData(parsedContents.Options),
				}
			};
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to load configuration file \"{path}\".", ex);
		}
	}

	private static Dictionary<string, string?> BuildOptionsData(JsonElement options)
	{
		var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		RecurseJsonElement(options, string.Empty, result);
		return result;
	}

	private static void RecurseJsonElement(JsonElement element, string keyPrefix, Dictionary<string, string?> mapping)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				foreach (var property in element.EnumerateObject())
				{
					RecurseJsonElement(property.Value, $"{keyPrefix}{property.Name}:", mapping);
				}
				break;
			case JsonValueKind.Array:
				int index = 0;
				foreach (var item in element.EnumerateArray())
				{
					RecurseJsonElement(item, $"{keyPrefix}{index++}:", mapping);
				}
				break;
			case JsonValueKind.String:
				// Remove the trailing colon from the prefix.
				mapping[keyPrefix[..^1]] = element.GetString();
				break;
			case JsonValueKind.Null:
				// Remove the trailing colon from the prefix.
				mapping[keyPrefix[..^1]] = null;
				break;
			default:
				// Remove the trailing colon from the prefix.
				mapping[keyPrefix[..^1]] = element.GetRawText();
				break;
		}
	}

	/// <summary>
	/// The schema for a configuration file.
	/// </summary>
	private sealed class OptionsFileSchema
	{
		public required OptionsMetadata Metadata { get; init; }
		public required JsonElement Options { get; init; }
	}
}