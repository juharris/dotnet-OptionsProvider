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
	Task<IOptionsProviderBuilder> AddDirectoryAsync(string rootPath);

	/// <summary>
	/// Finishes building the provider.
	/// </summary>
	/// <returns>The built provider.</returns>
	IOptionsProvider Build();
}