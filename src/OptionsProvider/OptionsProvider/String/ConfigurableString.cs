using System.Diagnostics;

namespace OptionsProvider.String;

/// <summary>
/// Helps configure a long string that many features may need to customize.
/// Only simple string searching operations are used to replace the values.
/// A template is assumed to use slots like &quot;{{key}}&quot; by default, by this can be customized.
/// </summary>
/// <remarks>
/// This implementation uses simple string operations to build the value because it should be sufficient for most cases.
/// More sophisticated implementations can use libraries like Fluid, Handlebars, Scriban, etc.
/// We do not want to add such dependencies by default to this mostly simple project.
/// This implementation is not meant to handle every type of edge case with every possible delimiter.
/// We may change and optimization the logic to suit typical cases, but anyone relying on odd behavior such as the delimiters within the delimiters (<tt>&quot;{{slotA{{slotB}}}</tt>}&quot;) may not get consistent results in a backwards compatible way after library updates.
/// In some cases we may add options to configure the replacement logic so that the old behavior can be enabled again.
/// </remarks>
[DebuggerDisplay("{Value,nq}")]
public sealed class ConfigurableString
{
	/// <summary>
	/// The template string that contains slots like &quot;{{key}}&quot;.
	/// </summary>
	public required string? Template { get; init; }

	/// <summary>
	/// Values to use to replace the slots in the template.
	/// <para>
	/// The keys should not contain the delimiter, i.e. for the default delimiter, keys should not contain &quot;{{&quot; and &quot;}}&quot;.
	/// </para>
	/// <para>
	/// Values may contain slots that will be replaced recursively.
	/// </para>
	/// <para>
	/// To &quot;remove&quot; a key from the values, set the value to <see langword="null"/> and the slot will not be replaced.
	/// I.e., if the template is &quot;{{key}}&quot; and the value for &quot;key&quot; is <see langword="null"/>, then the result for <see cref="Value"/> will be &quot;{{key}}&quot;.
	/// </para>
	/// Use an empty string to replace a slot with an empty string.
	/// </summary>
	public required IReadOnlyDictionary<string, string?>? Values { get; init; }

	/// <summary>
	/// A custom start delimiter for the slots to indentify the start of a slot to fill.
	/// </summary>
	public string StartDelimiter { get; init; } = "{{";

	/// <summary>
	/// A custom end delimiter for the slots to indentify the end of a slot to fill.
	/// </summary>
	public string EndDelimiter { get; init; } = "}}";

	/// <summary>
	/// Initializes an instance of <see cref="ConfigurableString"/>.
	/// </summary>
	public ConfigurableString()
	{
		this._value = new(() => this.BuildValue());
	}

	/// <summary>
	/// The built value of the configurable string.
	/// </summary>
	public string? Value => this._value.Value;

	// Only build the value only when it is needed assuming that we do not want to eagerly build the string
	// because it may only be needed later when the application is running and not when combining configurations, possibly at the start of a request.
	private readonly Lazy<string?> _value;

	private string? BuildValue()
	{
		if (this.Values is null)
		{
			return this.Template;
		}

		if (this.Template is null)
		{
			return null;
		}

		const int maxLoopCount = 10_000;

		string result = this.Template;

		var slotStartIndex = 0;
		var loopIndex = 0;
		for (; loopIndex < maxLoopCount; ++loopIndex)
		{
			slotStartIndex = result.IndexOf(this.StartDelimiter, slotStartIndex, StringComparison.Ordinal);
			if (slotStartIndex == -1)
			{
				break;
			}

			var keyStart = slotStartIndex + this.StartDelimiter.Length;
			var slotEndIndex = result.IndexOf(this.EndDelimiter, keyStart, StringComparison.Ordinal);
			if (slotEndIndex == -1)
			{
				break;
			}

			var key = result[keyStart..slotEndIndex];
			// A `null` value means the key was "removed" from the values and that it should not be replaced.
			// If it was desired to replace it with an empty string, then the value should be an empty string.
			if (this.Values.TryGetValue(key, out var value) && value is not null)
			{
				// Only replace it if the value is found, otherwise assume that the slot is part of the string.
				result = $"{result.AsSpan(0, slotStartIndex)}{value}{result.AsSpan(slotEndIndex + this.EndDelimiter.Length)}";

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

	/// <summary>
	/// Implicitly converts a string to a <see cref="ConfigurableString"/>.
	/// </summary>
	/// <param name="value">
	/// The value to return for <see cref="Value"/>.
	/// </param>
	public static implicit operator ConfigurableString(string value)
	{
		return new ConfigurableString
		{
			Template = value,
			Values = null,
		};
	}
}