using Microsoft.Extensions.DependencyInjection;

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

		return services;
	}
}