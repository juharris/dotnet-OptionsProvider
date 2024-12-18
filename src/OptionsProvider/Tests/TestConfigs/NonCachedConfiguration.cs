namespace OptionsProvider.Tests.TestConfigs;

/// <summary>
/// An example configuration for tests that looks the same as <see cref="MyConfiguration"/> but is not cached.
/// </summary>
/// <remarks>
/// Intentionally not inheriting from <see cref="MyConfiguration"/> to avoid any confusion and to show they are not related as classes,
/// but just happen to have the same properties.
/// </remarks>
internal sealed class NonCachedConfiguration
{
	public string[]? Array { get; set; }
	public MyObject? Object { get; set; }
	public MyDeeperObject? DeeperObject { get; set; }
	public MyDeeperObject[]? DeeperObjects { get; set; }
	public int? OptionalNumber { get; set; }
	public ConfigurableString? MyConfigurableString { get; set; }
}