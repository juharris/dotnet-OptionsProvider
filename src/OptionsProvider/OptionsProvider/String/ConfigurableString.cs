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
	/// <param name="startDelimiter"></param>
	/// <param name="endDelimiter"></param>
	public ConfigurableString(
		string? template,
		IReadOnlyDictionary<string, string> values,
		string startDelimiter = "{{",
		string endDelimiter = "}}")
	{
		this._value = new(() => BuildValue(template, values, startDelimiter, endDelimiter));
	}

	/// <summary>
	/// The built value of the configurable string.
	/// </summary>
	public string? Value => this._value.Value;

	private readonly Lazy<string?> _value;

	private static string? BuildValue(
		string? template,
		IReadOnlyDictionary<string, string> values,
		string startDelimiter,
		string endDelimiter)
	{
		const int maxLoopCount = 10_000;
		if (template is null)
		{
			return null;
		}

		// Use simple string operations to build the value.
		// More sophisticated implementations can use libraries like Fluid, Handlebars, Scriban, etc.
		// We do not want to add such dependencies to this mostly simple project.
		string result = template;
		var slotStartIndex = 0;

		var loopIndex = 0;
		for (; loopIndex < maxLoopCount; ++loopIndex)
		{
			slotStartIndex = result.IndexOf(startDelimiter, slotStartIndex, StringComparison.Ordinal);
			if (slotStartIndex == -1)
			{
				break;
			}

			var keyStart = slotStartIndex + startDelimiter.Length;
			var slotEndIndex = result.IndexOf(endDelimiter, keyStart, StringComparison.Ordinal);
			if (slotEndIndex == -1)
			{
				break;
			}

			var key = result[keyStart..slotEndIndex];
			if (values.TryGetValue(key, out var value))
			{
				// Only replace it if the value is found, otherwise assume that the {{key}} is part of the string.
				result = $"{result.AsSpan(0, slotStartIndex)}{value}{result.AsSpan(slotEndIndex + endDelimiter.Length)}";
				// Do not update the start index, because the replaced value may contain a slot to replace.
			}
			else
			{
				// slotStartIndex = slotStartIndex + startDelimiter.Length;
				slotStartIndex = slotEndIndex + endDelimiter.Length;
			}
		}

		if (loopIndex == maxLoopCount)
		{
			throw new InvalidOperationException($"The replacement loop count exceeded the maximum allowed iterations ({maxLoopCount}). There was likely a recursive loop using the template and values.");
		}

		return result;
	}
}
