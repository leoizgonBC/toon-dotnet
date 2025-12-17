#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Primitive value encoding, key encoding, and header formatting utilities.
    /// Aligned with TypeScript encode/primitives.ts
    /// </summary>
    internal static class Primitives
    {
        // #region Primitive encoding

        /// <summary>
        /// Encodes a primitive JSON value (null, boolean, number, or string) to its TOON representation.
        /// </summary>
        public static string EncodePrimitive(JsonNode? value, char delimiter = Constants.COMMA)
        {
            if (value == null)
                return Constants.NULL_LITERAL;

            if (value is JsonValue jsonValue)
            {
                // Boolean
                if (jsonValue.TryGetValue<bool>(out var boolVal))
                    return boolVal ? Constants.TRUE_LITERAL : Constants.FALSE_LITERAL;

                // Number - use canonical formatting per TOON Spec §2
                if (jsonValue.TryGetValue<int>(out var intVal))
                    return intVal.ToString(CultureInfo.InvariantCulture);

                if (jsonValue.TryGetValue<long>(out var longVal))
                    return longVal.ToString(CultureInfo.InvariantCulture);

                if (jsonValue.TryGetValue<double>(out var doubleVal))
                    return FormatCanonicalNumber(doubleVal);

                if (jsonValue.TryGetValue<decimal>(out var decimalVal))
                    return FormatCanonicalDecimal(decimalVal);

                // String
                if (jsonValue.TryGetValue<string>(out var strVal))
                    return EncodeStringLiteral(strVal ?? string.Empty, delimiter);
            }

            return Constants.NULL_LITERAL;
        }

        /// <summary>
        /// Formats a double value in canonical TOON format (TOON Spec §2):
        /// - No exponent notation
        /// - No trailing zeros in fractional part
        /// - If fractional part is zero, emit as integer
        /// - -0 normalized to 0 (handled by Normalize.cs)
        /// </summary>
        private static string FormatCanonicalNumber(double value)
        {
            // Handle special cases
            if (double.IsNaN(value) || double.IsInfinity(value))
                return Constants.NULL_LITERAL;

            // Check if value is an integer
            if (value == Math.Truncate(value) && Math.Abs(value) < 1e15)
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }

            // Try to find the shortest representation that round-trips
            // Start with fewer digits and increase until round-trip works
            for (int precision = 1; precision <= 17; precision++)
            {
                var formatted = value.ToString("G" + precision, CultureInfo.InvariantCulture);
                
                // If it contains exponent, we need to expand it
                if (formatted.Contains('E') || formatted.Contains('e'))
                {
                    // Try parsing back to check round-trip
                    if (double.TryParse(formatted, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed == value)
                    {
                        formatted = ExpandExponent(value, formatted);
                        formatted = RemoveTrailingZeros(formatted);
                        return formatted;
                    }
                    continue;
                }

                // Check if this precision round-trips correctly
                if (double.TryParse(formatted, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue) && parsedValue == value)
                {
                    formatted = RemoveTrailingZeros(formatted);
                    return formatted;
                }
            }

            // Fallback to G17 for full precision
            var fullPrecision = value.ToString("G17", CultureInfo.InvariantCulture);
            if (fullPrecision.Contains('E') || fullPrecision.Contains('e'))
            {
                fullPrecision = ExpandExponent(value, fullPrecision);
            }
            return RemoveTrailingZeros(fullPrecision);
        }

        /// <summary>
        /// Formats a decimal value in canonical TOON format.
        /// </summary>
        private static string FormatCanonicalDecimal(decimal value)
        {
            // Check if value is an integer
            if (value == Math.Truncate(value))
            {
                return ((long)value).ToString(CultureInfo.InvariantCulture);
            }

            var formatted = value.ToString(CultureInfo.InvariantCulture);

            // Remove trailing zeros from fractional part
            formatted = RemoveTrailingZeros(formatted);

            return formatted;
        }

        /// <summary>
        /// Expands exponential notation to decimal form.
        /// </summary>
        private static string ExpandExponent(double value, string? formattedStr = null)
        {
            // Use provided string or format with G17
            var str = formattedStr ?? value.ToString("G17", CultureInfo.InvariantCulture);

            var eIndex = str.IndexOf('E');
            if (eIndex == -1)
                eIndex = str.IndexOf('e');

            if (eIndex == -1)
                return str;

            var mantissa = str.Substring(0, eIndex);
            var exponent = int.Parse(str.Substring(eIndex + 1), CultureInfo.InvariantCulture);

            var isNegative = mantissa.StartsWith("-");
            if (isNegative)
                mantissa = mantissa.Substring(1);

            var dotIndex = mantissa.IndexOf('.');
            string intPart, fracPart;

            if (dotIndex >= 0)
            {
                intPart = mantissa.Substring(0, dotIndex);
                fracPart = mantissa.Substring(dotIndex + 1);
            }
            else
            {
                intPart = mantissa;
                fracPart = "";
            }

            // Combine digits
            var allDigits = intPart + fracPart;

            // Calculate new decimal position
            var newDecimalPos = intPart.Length + exponent;

            string result;
            if (newDecimalPos <= 0)
            {
                // Need leading zeros after decimal point
                result = "0." + new string('0', -newDecimalPos) + allDigits;
            }
            else if (newDecimalPos >= allDigits.Length)
            {
                // Integer result - pad with trailing zeros
                result = allDigits + new string('0', newDecimalPos - allDigits.Length);
            }
            else
            {
                // Insert decimal point
                result = allDigits.Substring(0, newDecimalPos) + "." + allDigits.Substring(newDecimalPos);
            }

            // Remove leading zeros from integer part (except single zero before decimal)
            if (result.Contains('.'))
            {
                var parts = result.Split('.');
                var intPartResult = parts[0].TrimStart('0');
                if (string.IsNullOrEmpty(intPartResult))
                    intPartResult = "0";
                result = intPartResult + "." + parts[1];
            }
            else
            {
                result = result.TrimStart('0');
                if (string.IsNullOrEmpty(result))
                    result = "0";
            }

            if (isNegative)
                result = "-" + result;

            return result;
        }

        /// <summary>
        /// Removes trailing zeros from the fractional part of a number string.
        /// </summary>
        private static string RemoveTrailingZeros(string formatted)
        {
            if (!formatted.Contains('.'))
                return formatted;

            // Remove trailing zeros
            formatted = formatted.TrimEnd('0');

            // If we removed all fractional digits, remove the decimal point too
            if (formatted.EndsWith("."))
                formatted = formatted.Substring(0, formatted.Length - 1);

            return formatted;
        }

        /// <summary>
        /// Encodes a string literal, adding quotes if necessary.
        /// </summary>
        public static string EncodeStringLiteral(string value, char delimiter = Constants.COMMA)
        {
            var delimiterEnum = Constants.FromDelimiterChar(delimiter);

            if (ValidationShared.IsSafeUnquoted(value, delimiterEnum))
            {
                return value;
            }

            var escaped = StringUtils.EscapeString(value);
            return $"{Constants.DOUBLE_QUOTE}{escaped}{Constants.DOUBLE_QUOTE}";
        }

        // #endregion

        // #region Key encoding

        /// <summary>
        /// Encodes a key, adding quotes if necessary.
        /// </summary>
        public static string EncodeKey(string key)
        {
            if (ValidationShared.IsValidUnquotedKey(key))
            {
                return key;
            }

            var escaped = StringUtils.EscapeString(key);
            return $"{Constants.DOUBLE_QUOTE}{escaped}{Constants.DOUBLE_QUOTE}";
        }

        // #endregion

        // #region Value joining

        /// <summary>
        /// Encodes and joins an array of primitive values with the specified delimiter.
        /// </summary>
        public static string EncodeAndJoinPrimitives(IEnumerable<JsonNode?> values, char delimiter = Constants.COMMA)
        {
            var encoded = values.Select(v => EncodePrimitive(v, delimiter));
            return string.Join(delimiter.ToString(), encoded);
        }

        // #endregion

        // #region Header formatters

        /// <summary>
        /// Formats an array header with optional key, length marker, delimiter, and field names.
        /// Examples:
        /// - "[3]:" for unnamed array of 3 items
        /// - "items[5]:" for named array
        /// - "users[#2]{name,age}:" for tabular format with length marker
        /// </summary>
        public static string FormatHeader(
            int length,
            string? key = null,
            IReadOnlyList<string>? fields = null,
            char? delimiter = null)
        {
            var delimiterChar = delimiter ?? Constants.DEFAULT_DELIMITER_CHAR;
            var header = string.Empty;

            // Add key if present
            if (!string.IsNullOrEmpty(key))
            {
                header += EncodeKey(key);
            }

            // Add array length with optional marker and delimiter
            var delimiterSuffix = delimiterChar != Constants.DEFAULT_DELIMITER_CHAR
                ? delimiterChar.ToString()
                : string.Empty;

            header += $"{Constants.OPEN_BRACKET}{length}{delimiterSuffix}{Constants.CLOSE_BRACKET}";

            // Add field names for tabular format
            if (fields != null && fields.Count > 0)
            {
                var quotedFields = fields.Select(EncodeKey);
                var fieldsStr = string.Join(delimiterChar.ToString(), quotedFields);
                header += $"{Constants.OPEN_BRACE}{fieldsStr}{Constants.CLOSE_BRACE}";
            }

            header += Constants.COLON;

            return header;
        }

        // #endregion
    }
}
