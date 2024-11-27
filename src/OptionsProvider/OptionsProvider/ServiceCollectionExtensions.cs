using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace OptionsProvider;

/// <summary>
/// Utilities for using an <see cref="IOptionsProvider"/> with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Sets up <see cref="IOptionsProviderBuilder"/> to be ready for injecting as a dependency.
	/// </summary>
	/// <param name="services">The current service collection for dependency injection.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining calls.</returns>
	public static IServiceCollection AddOptionsProviderBuilder(
		this IServiceCollection services)
	{
		services.AddMemoryCache();
		services.TryAddTransient<IOptionsProviderBuilder, OptionsProviderBuilder>();
		return services;
	}

	/// <summary>
	/// Sets up <see cref="IOptionsProvider"/> to be ready for injecting as a dependency.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="AddOptionsProviderBuilder(IServiceCollection)"/>.
	/// </remarks>
	/// <param name="services">The current service collection for dependency injection.</param>
	/// <param name="path">The base directory to start searching for files that represent features.</param>
	/// <param name="featureConfigurations">(optional) Additional custom configurations for features.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining calls.</returns>
	public static IServiceCollection AddOptionsProvider(
		this IServiceCollection services,
		string path,
		IEnumerable<FeatureConfiguration>? featureConfigurations = null)
	{
		services.AddOptionsProviderBuilder();
		services.TryAddSingleton<IOptionsProvider>(serviceProvider =>
		{
			var builder = serviceProvider.GetRequiredService<IOptionsProviderBuilder>();
			builder.AddDirectoryAsync(path).Wait();
			if (featureConfigurations is not null)
			{
				foreach (var featureConfiguration in featureConfigurations)
				{
					builder.AddConfigurationSource(featureConfiguration);
				}
			}
			return builder.Build();
		});

		services.TryAddScoped<IFeaturesContext, FeaturesContext>();

		return services;
	}

	/// <summary>
	/// Enables providing a configuration object with <see cref="IOptionsSnapshot{TOptions}"/> for dependency injection into scoped classes.
	/// </summary>
	/// <typeparam name="TOptions">The type of options to provide.</typeparam>
	/// <param name="services">The current service collection for dependency injection.</param>
	/// <param name="optionsKey">The path to the options in the configurations.</param>
	/// <param name="cacheOptionsBuilder">(optional) Creates options to pass to the configuration cache so that configurations do not need to be rebuilt.</param>
	/// <returns><paramref name="services"/></returns>
	/// <remarks>
	/// Requires <see cref="AddOptionsProvider"/> to be used.
	/// </remarks>
	public static IServiceCollection ConfigureOptions<TOptions>(
		this IServiceCollection services,
		string optionsKey,
		Func<CacheOptionsBuilderInput, MemoryCacheEntryOptions?>? cacheOptionsBuilder = null)
		where TOptions : class
	{
		services
			.AddOptions<TOptions>()
			.Configure<IOptionsProvider, IFeaturesContext>((options, optionsProvider, featuresContext) =>
		{
			MemoryCacheEntryOptions? cacheOptions = null;
			if (cacheOptionsBuilder is not null)
			{
				var input = new CacheOptionsBuilderInput
				{
					OptionsKey = optionsKey,
					FeaturesContext = featuresContext,
				};
				cacheOptions = cacheOptionsBuilder.Invoke(input);
			}

			var optionsForFeatures = optionsProvider.GetOptions<TOptions>(optionsKey, featuresContext.FeatureNames, cacheOptions)!;
			foreach (var property in typeof(TOptions).GetProperties())
			{
				var value = property.GetValue(optionsForFeatures);
				property.SetValue(options, value);
			}
		});
		return services;
	}
}