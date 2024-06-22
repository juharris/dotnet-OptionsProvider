namespace OptionsProvider;

/// <summary>
/// Extra information about the group of options.
/// </summary>
public sealed class OptionsMetadata
{
    /// <summary>
    /// The name of the group of options.
    /// </summary>
    /// <remarks>
    /// This may be derived from the file name including subfolders.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Alternative names for the group of options.
    /// </summary>
    /// <remarks>
    /// This is helpful for using custom short names for the group of options.
    /// </remarks>
    public string[]? AlternativeNames { get; set; }

    /// <summary>
    /// The creators or maintainers of this group of options.
    /// </summary>
    /// <remarks>
    /// For example, emails separated by ";".
    /// </remarks>
    public required string Owners { get; set; }

    /// <summary>
    /// A date before which the options can be used.
    /// After this date, this group of options might not be supported and should be considered for deletion.
    /// </summary>
    public DateTime? BestBeforeDate { get; set; }

    /// <summary>
    /// Indicates if this group of options is expected to be supported for a long time.
    /// </summary>
    public bool IsPersistent { get; set; }
}