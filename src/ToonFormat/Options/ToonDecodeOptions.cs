#nullable enable
using ToonFormat;

namespace Toon.Format;

/// <summary>
/// Options for decoding TOON format strings.
/// </summary>
public class ToonDecodeOptions
{
    /// <summary>
    /// Number of spaces per indentation level.
    /// Default is 2.
    /// </summary>
    public int Indent { get; set; } = 2;

    /// <summary>
    /// When true, enforce strict validation of array lengths and tabular row counts.
    /// Default is true.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Path expansion mode for dotted keys (TOON Spec §13.4).
    /// When set to <see cref="ToonExpandPaths.Safe"/>, eligible dotted keys are expanded
    /// into nested object structures with deep-merge semantics.
    /// Default is <see cref="ToonExpandPaths.Off"/> (dotted keys treated as literals).
    /// </summary>
    public ToonExpandPaths ExpandPaths { get; set; } = ToonExpandPaths.Off;
}
