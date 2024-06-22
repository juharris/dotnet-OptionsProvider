using Microsoft.Extensions.Configuration;

namespace OptionsProvider;

public interface IOptionsProvider
{
    T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames);
}

public sealed class OptionsProvider(
    IConfiguration baseConfiguration,
    IDictionary<string, IConfigurationSource> sources) : IOptionsProvider
{
    public T? GetOptions<T>(string key, IReadOnlyCollection<string>? featureNames)
    {
        if (featureNames is null || featureNames.Count == 0)
        {
            return baseConfiguration.GetSection(key).Get<T>();
        }

        var configurationBuilder = new ConfigurationBuilder()
            // Fallback to the base default configuration.
            .AddConfiguration(baseConfiguration);

        // Apply the features in order so that the last ones takes precedence, just like how when multiple appsettings.json files are used.
        foreach (var featureName in featureNames)
        {
            if (sources.TryGetValue(featureName, out var source))
            {
                configurationBuilder.Add(source);
            }
            else
            {
                throw new InvalidOperationException($"Feature '{featureName}' is not a known feature.");
            }
        }

        var configuration = configurationBuilder.Build();
        var result = configuration.GetSection(key).Get<T>();

        // TODO Cache the options for the features for a limited time.
        // Maybe use IMemoryCache.
        return result;
    }
}