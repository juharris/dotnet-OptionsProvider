using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OptionsProvider;

/// <summary>
/// Utilities for using an <see cref="IOptionsProvider"/> with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Sets up the <see cref="IOptionsProvider"/> for dependency injection.
	/// </summary>
	/// <param name="services">The current service collection for dependency injection.</param>
	/// <param name="path">The base directory to start searching for files.</param>
	/// <returns><paramref name="services"/></returns>
	public static IServiceCollection AddOptionsProvider(
		this IServiceCollection services,
		string path)
	{
		services.AddMemoryCache();
		services.AddSingleton<IOptionsLoader, OptionsLoader>();
		services.AddSingleton(serviceProvider =>
		{
			var loader = serviceProvider.GetRequiredService<IOptionsLoader>();
			return loader.LoadAsync(path).Result;
		});

		services.AddScoped<IFeaturesContext, FeaturesContext>();

		return services;
	}

	/// <summary>
	/// Enables providing a configuration object with <see cref="IOptionsSnapshot{TOptions}"/> for dependency injection.
	/// </summary>
	/// <typeparam name="TOptions">The type of options to provide.</typeparam>
	/// <param name="services">The current service collection for dependency injection.</param>
	/// <param name="optionsKey">The key to the options in the configurations.</param>
	/// <returns><paramref name="services"/></returns>
	/// <remarks>
	/// Requires <see cref="AddOptionsProvider(IServiceCollection, string)"/> to be used.
	/// </remarks>
	public static IServiceCollection ConfigureOptions<TOptions>(
		this IServiceCollection services,
		string optionsKey)
		where TOptions : class
	{
		services
			.AddOptions<TOptions>()
			.Configure<IOptionsProvider, IFeaturesContext>((options, optionsProvider, featuresContext) =>
		{
			var optionsForFeatures = optionsProvider.GetOptions<TOptions>(optionsKey, featuresContext.FeatureNames)!;
			foreach (var property in typeof(TOptions).GetProperties())
			{
				var value = property.GetValue(optionsForFeatures);
				if (value is not null)
				{
					property.SetValue(options, value);
				}
			}
		});
		return services;
	}
}