namespace OptionsProvider;

/// <summary>
/// Loads options from files or custom sources.
/// </summary>
public interface IOptionsProviderBuilder
{
	/// <summary>
	/// Add an alternative name for a feature.
	/// </summary>
	/// <param name="alias">An alternative name for <paramref name="featureName"/>.</param>
	/// <param name="featureName">The name of an existing feature.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>
	/// Validation is done to check for conflicts with existing aliases or feature names.
	/// This method does not update <see cref="OptionsMetadata.Aliases"/> yet, but it may in the future.
	/// </remarks>
	IOptionsProviderBuilder AddAlias(string alias, string featureName);

	/// <summary>
	/// Adds a configuration for a feature.
	/// </summary>
	/// <param name="featureConfiguration">
	/// Metadata about the feature and the configuration for the feature.
	/// The <see cref="OptionsMetadata.Name"/> must be set in <see cref="FeatureConfiguration.Metadata"/>.
	/// </param>
	/// <returns>The current builder.</returns>
	/// <remarks>Validation is done to check for conflicts with existing feature names.</remarks>
	IOptionsProviderBuilder AddConfigurationSource(FeatureConfiguration featureConfiguration);

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
	/// Set an alternative name for a feature.
	/// </summary>
	/// <param name="alias">An alternative name for <paramref name="featureName"/>.</param>
	/// <param name="featureName">The name of a feature.</param>
	/// <returns>The current builder.</returns>
	/// <remarks>
	/// No extra validation is done to check for conflicts with existing aliases or feature names.
	/// This method does not update <see cref="OptionsMetadata.Aliases"/> yet, but it may in the future.
	/// </remarks>
	IOptionsProviderBuilder SetAlias(string alias, string featureName);

	/// <summary>
	/// Sets a configuration for a feature.
	/// </summary>
	/// <param name="featureConfiguration">
	/// Metadata about the feature and the configuration for the feature.
	/// The <see cref="OptionsMetadata.Name"/> must be set in <see cref="FeatureConfiguration.Metadata"/>.
	/// </param>
	/// <returns>The current builder.</returns>
	/// <remarks>No extra validation is done to check for conflicts with existing feature names.</remarks>
	IOptionsProviderBuilder SetConfigurationSource(FeatureConfiguration featureConfiguration);
}