namespace OptionsProvider;

public interface IOptionsProvider
{
	T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames = null);
}