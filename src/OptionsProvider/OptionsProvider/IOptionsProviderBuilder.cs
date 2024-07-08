using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

/// <summary>
/// Loads options from files.
/// </summary>
public interface IOptionsProviderBuilder
{
	/// <summary>
	/// Adds a configuration for a feature.
	/// </summary>
	/// <param name="featureName">The name of the feature.</param>
	/// <param name="configurationSource">The configuration for the feature.</param>
	/// <returns>The current builder.</returns>
	IOptionsProviderBuilder AddConfigurationSource(string featureName, IConfigurationSource configurationSource);

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
}