using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace OptionsProvider;

/// <summary>
/// Simple options loader.
/// </summary>
public sealed class OptionsProviderBuilder(
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

	private readonly Dictionary<string, string> altNameMapping = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, IConfigurationSource> sourcesMapping = new(StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc/>
	public async Task<IOptionsProviderBuilder> AddDirectoryAsync(string rootPath)
	{
		var paths = Directory.EnumerateFiles(rootPath, "*.json", SearchOption.AllDirectories)
			.Concat(Directory.EnumerateFiles(rootPath, "*.yaml", SearchOption.AllDirectories))
			.Concat(Directory.EnumerateFiles(rootPath, "*.yml", SearchOption.AllDirectories))
			// Ensure that the files are loaded in a consistent order so that errors are consistent on different machines.
			.Order();

		// Use async tasks to load files in parallel.
		var fileConfigs = await Task.WhenAll(paths
			.Select(filePath => LoadFileAsync(rootPath, filePath))
			.ToArray());
		foreach (var (configPath, fileConfig) in paths.Zip(fileConfigs))
		{
			var name = fileConfig.Metadata.Name!;

			// Provide a canonical case-insensitive name for the configuration which also simplifies mapping alternative names to the canonical name.
			if (!this.altNameMapping.TryAdd(name, name))
			{
				throw new InvalidOperationException($"The name \"{name}\" for the configuration file \"{configPath}\" is already used.");
			}

			this.sourcesMapping[name] = fileConfig.Source;

			if (fileConfig.Metadata.Aliases is not null)
			{
				foreach (var alias in fileConfig.Metadata.Aliases)
				{
					if (!this.altNameMapping.TryAdd(alias, name))
					{
						throw new InvalidOperationException($"The alias name \"{alias}\" for the configuration file \"{configPath}\" is already used.");
					}
				}
			}
		}

		return this;
	}

	/// <inheritdoc/>
	public IOptionsProvider Build()
	{
		return new OptionsProviderWithDefaults(baseConfiguration, cache, this.sourcesMapping, this.altNameMapping);
	}

	private static async Task<FileConfig> LoadFileAsync(string rootPath, string path)
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
			return new FileConfig
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
	/// The converted version of a loaded configuration file.
	/// </summary>
	private class FileConfig
	{
		public required OptionsMetadata Metadata { get; init; }
		public required IConfigurationSource Source { get; init; }
	}

	/// <summary>
	/// The schema for a configuration file.
	/// </summary>
	private class OptionsFileSchema
	{
		public required OptionsMetadata Metadata { get; set; }
		public required JsonElement Options { get; set; }
	}
}