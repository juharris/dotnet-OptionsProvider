namespace OptionsProvider;

/// <summary>
/// Loads options from files.
/// </summary>
public interface IOptionsLoader
{
	/// <summary>
	/// Loads and options from files in parallel.
	/// </summary>
	/// <param name="rootPath">The base directory to start searching for files.</param>
	/// <returns>The loaded options.</returns>
	/// <remarks>
	/// Currently only "*.json" files are supported, but more types of files may be supported in the future.
	/// </remarks>
	Task<IOptionsProvider> LoadAsync(string rootPath);
}