#nullable enable
using System.Globalization;

namespace ToonFormat.Internal.Shared
{
    /// <summary>
    /// Literal judgment utilities, aligned with TypeScript version shared/literal-utils.ts.
    /// - IsBooleanOrNullLiteral: Determines if it is true/false/null
    /// - IsNumericLiteral: Determines if it is a numeric literal, rejecting invalid leading zero forms
    /// </summary>
    internal static class LiteralUtils
    {
        /// <summary>
        /// Checks if the token is a boolean or null literal: true, false, null.
        /// Equivalent to TS: isBooleanOrNullLiteral
        /// </summary>
        internal static bool IsBooleanOrNullLiteral(string token)
        {
            return string.Equals(token, Constants.TRUE_LITERAL, StringComparison.Ordinal)
                || string.Equals(token, Constants.FALSE_LITERAL, StringComparison.Ordinal)
                || string.Equals(token, Constants.NULL_LITERAL, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the token is a valid numeric literal.
        /// Rules aligned with TOON Spec §2 and §4:
        /// - Rejects forbidden leading zeros (e.g., "05", "0001", "-05", "-0001")
        /// - Allows "0" itself, decimals like "0.5", "-0.5", and exponent forms like "0e1"
        /// - Parses successfully and is a finite number (not NaN/Infinity)
        /// </summary>
        internal static bool IsNumericLiteral(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // Handle negative numbers
            var checkToken = token;
            if (token.StartsWith("-") && token.Length > 1)
            {
                checkToken = token.Substring(1);
            }

            // Check for forbidden leading zeros in integer part
            // Forbidden: "05", "0001" (leading zeros followed by more digits)
            // Allowed: "0", "0.5", "0e1" (single zero or zero followed by . or e/E)
            if (checkToken.Length > 1 && checkToken[0] == '0')
            {
                var secondChar = checkToken[1];
                // Only allow if second char is '.' or 'e' or 'E'
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                    return false;
            }

            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                return false;

            return !double.IsNaN(num) && !double.IsInfinity(num);
        }
    }
}