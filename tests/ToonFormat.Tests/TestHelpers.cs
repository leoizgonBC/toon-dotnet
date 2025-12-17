#nullable enable

namespace ToonFormat.Tests
{
    /// <summary>
    /// Test helper utilities for TOON format tests.
    /// </summary>
    internal static class TestHelpers
    {
        /// <summary>
        /// Normalizes line endings to LF (\n) as per TOON specification §1.2.
        /// TOON encoders MUST use LF for line endings.
        /// </summary>
        /// <param name="value">The string to normalize.</param>
        /// <returns>The string with all line endings converted to LF.</returns>
        public static string NormalizeLF(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Replace CRLF with LF, then any remaining CR with LF
            return value.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}

