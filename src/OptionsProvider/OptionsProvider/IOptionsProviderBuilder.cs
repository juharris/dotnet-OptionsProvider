using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

/// <summary>
/// Loads options from files.
/// </summary>
public interface IOptionsProviderBuilder
{
	/// <summary>
	/// Loads and options from files in parallel.
	/// </summary>
	/// <param name="rootPath">The base directory to find configuration files.</param>
	/// <returns>The loaded options.</returns>
	/// <remarks>
	/// Currently "*.json", "*.yaml", and "*.yml" files are supported.
	/// </remarks>
	/// <returns>The current builder.</returns>
	Task<IOptionsProviderBuilder> AddDirectoryAsync(string rootPath);

	/// <summary>
	/// Finishes building the provider.
	/// </summary>
	/// <returns>The built provider.</returns>
	IOptionsProvider Build();

	/// <summary>
	/// Add an alternative name for a feature.
	/// </summary>
	/// <param name="alias">An alternative name for <paramref name="featureName"/>.</param>
	/// <param name="featureName">The name of a feature.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>No extra validation is done to check for conflicts with existing aliases or feature names.</remarks>
	IOptionsProviderBuilder SetAlias(string alias, string featureName);

	/// <summary>
	/// Adds a configuration for a feature.
	/// </summary>
	/// <param name="metadata">Metadata about the feature. The <see cref="OptionsMetadata.Name"/> must be set.</param>
	/// <param name="configurationSource">The configuration for the feature.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>No extra validation is done to check for conflicts with existing feature names.</remarks>
	IOptionsProviderBuilder SetConfigurationSource(OptionsMetadata metadata, IConfigurationSource configurationSource);
}