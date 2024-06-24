using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace OptionsProvider;

/// <summary>
/// Simple options loader.
/// </summary>
public sealed class OptionsLoader(
	IConfiguration baseConfiguration)
	: IOptionsLoader
{
	private static readonly JsonSerializerOptions DeserializationOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};

	/// <inheritdoc/>
	public async Task<IOptionsProvider> LoadAsync(
		string rootPath,
		IMemoryCache cache)
	{
		var paths = Directory.EnumerateFiles(rootPath, "*.json", SearchOption.AllDirectories)
			// Ensure that the files are loaded in a consistent order so that errors are consistent on different machines.
			.Order();

		// Use async tasks to load files in parallel.
		var fileConfigs = await Task.WhenAll(paths
			.Select(filePath => LoadFileAsync(rootPath, filePath))
			.ToArray());
		var altNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var sourcesMapping = new Dictionary<string, IConfigurationSource>(StringComparer.OrdinalIgnoreCase);
		foreach (var (configPath, fileConfig) in paths.Zip(fileConfigs))
		{
			var name = fileConfig.Metadata.Name!;

			// Provide a canonical case-insensitive name for the configuration which also simplifies mapping alternative names to the canonical name.
			if (!altNameMapping.TryAdd(name, name))
			{
				throw new InvalidOperationException($"The name \"{name}\" for the configuration file \"{configPath}\" is already used.");
			}

			sourcesMapping[name] = fileConfig.Source;

			if (fileConfig.Metadata.AlternativeNames is not null)
			{
				foreach (var alternativeName in fileConfig.Metadata.AlternativeNames)
				{
					if (!altNameMapping.TryAdd(alternativeName, name))
					{
						throw new InvalidOperationException($"The name \"{name}\" for the configuration file \"{configPath}\" is already used.");
					}
				}
			}
		}

		return new OptionsProviderWithDefaults(baseConfiguration, cache, sourcesMapping, altNameMapping);
	}

	private static async Task<FileConfig> LoadFileAsync(string rootPath, string path)
	{
		using var stream = File.OpenRead(path);
		var parsedContents = (await JsonSerializer.DeserializeAsync<OptionsFileSchema>(stream, DeserializationOptions))!;
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
					RecurseJsonElement(item, $"{keyPrefix}{index}:", mapping);
					++index;
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