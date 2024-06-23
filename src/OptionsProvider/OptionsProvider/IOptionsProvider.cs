namespace OptionsProvider;

/// <summary>
/// Provides options given enabled feature names.
/// </summary>
public interface IOptionsProvider
{
	/// <summary>
	/// Get the options for the specified key when the specified features are enabled.
	/// </summary>
	/// <typeparam name="T">The type of the options at the key in the configurations.</typeparam>
	/// <param name="key">The location of the options in the configurations.</param>
	/// <param name="featureNames">The abstract names of the scenarios to enable.</param>
	/// <returns>The configuration.</returns>
	T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames = null);
}