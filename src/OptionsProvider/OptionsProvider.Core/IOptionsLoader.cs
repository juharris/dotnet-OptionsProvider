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
	/// Currently only "*.json" files are supported, but more types of files may be supported in the future such as yaml files.
	/// </remarks>
	Task<IOptionsProvider> LoadAsync(
		string rootPath);
}