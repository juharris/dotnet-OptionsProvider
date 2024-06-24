namespace OptionsProvider;

/// <summary>
/// Loads options from files.
/// </summary>
public interface IOptionsLoader
{
	/// <summary>
	/// Loads and options from files in parallel.
	/// </summary>
	/// <param name="rootPath">The base directory to find configuration files.</param>
	/// <returns>The loaded options.</returns>
	/// <remarks>
	/// Currently "*.json", "*.yaml", and "*.yml" files are supported.
	/// </remarks>
	Task<IOptionsProvider> LoadAsync(
		string rootPath);
}