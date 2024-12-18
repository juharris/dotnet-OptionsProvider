namespace OptionsProvider.String;

/// <summary>
/// Helps configure a long string that many features may need to customize.
/// Only simple string searching operations are used to replace the values.
/// A template is assumed to use slots like &quot;{{key}}&quot;.
/// </summary>
public sealed class ConfigurableString
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigurableString"/> class.
	/// </summary>
	/// <param name="template">
	/// The template string that contains slots like &quot;{{key}}&quot;.
	/// </param>
	/// <param name="values">
	/// Values to use to replace the slots in the template.
	/// The keys should not contain the &quot;{{&quot; and &quot;}}&quot;.
	/// Values may contain slots that will be replaced recursively.
	/// </param>
	public ConfigurableString(
		string? template,
		IReadOnlyDictionary<string, string> values)
	{
		this.Value = new(() => BuildValue(template, values));
	}

	/// <summary>
	/// The built value of the configurable string.
	/// </summary>
	// TODO RENAME
	public Lazy<string?> Value { get; private set; }

	private static string? BuildValue(
		string? template,
		IReadOnlyDictionary<string, string> values)
	{
		if (template is null)
		{
			return null;
		}

		// Use simple string operations to build the value.
		// More sophisticated implementations can use libraries like Fluid, Handlebars, Scriban, etc.
		// We do not want to add such dependencies to this mostly simple project.
		string result = template;
		var start = 0;
		while (true)
		{
			start = result.IndexOf("{{", start, StringComparison.Ordinal);
			if (start == -1)
			{
				break;
			}

			var keyStart = start + 2;
			var end = result.IndexOf("}}", keyStart, StringComparison.Ordinal);
			if (end == -1)
			{
				break;
			}

			var key = result[keyStart..end];
			if (values.TryGetValue(key, out var value))
			{
				// Only replace it if the value is found, otherwise assume that the {{key}} is part of the string.
				result = $"{result.AsSpan(0, start)}{value}{result.AsSpan(end + 2)}";
				// Do not update the start index, because the replaced value may contain a slot to replace.
			}
			else
			{
				start = end + 2;
			}
		}

		return result;
	}
}
