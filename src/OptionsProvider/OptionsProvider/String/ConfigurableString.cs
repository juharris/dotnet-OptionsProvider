namespace OptionsProvider.String;

/// <summary>
/// Helps configure a long string that many features may need to customize.
/// Only simple string searching operations are used to replace the values.
/// A template is assumed to use slots like &quot;{{key}}&quot; by default, by this can be customized.
/// </summary>
/// <remarks>
/// This implementation uses simple string operations to build the value because this should work for most cases.
/// More sophisticated implementations can use libraries like Fluid, Handlebars, Scriban, etc.
/// We do not want to add such dependencies by default to this mostly simple project.
/// </remarks>
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
	/// <param name="startDelimiter">
	/// A custom start delimiter for the slots to indentify the start of a slot to fill.
	/// </param>
	/// <param name="endDelimiter">
	/// A custom end delimiter for the slots to indentify the end of a slot to fill.
	/// </param>
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

	// Only build the value only when it is needed assuming that we do not want to eagerly build the string
	// because it may only be needed later when the application is running and not when combining configurations, possibly at the start of a request.
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
				// Only replace it if the value is found, otherwise assume that the slot is part of the string.
				result = $"{result.AsSpan(0, slotStartIndex)}{value}{result.AsSpan(slotEndIndex + endDelimiter.Length)}";

				// We need to restart from the beginning because the string has changed so we need to check for the first instance of a start delimiter since we might have been building a key.
				slotStartIndex = 0;
			}
			else
			{
				// The value at this start index was not found, so we will continue searching from the next character since we may have consecutive or overlapping start delimiters.
				slotStartIndex += 1;
			}
		}

		if (loopIndex == maxLoopCount)
		{
			throw new InvalidOperationException($"The replacement loop count exceeded the maximum allowed iterations ({maxLoopCount}). There was likely a recursive loop using the template and values.");
		}

		return result;
	}
}
