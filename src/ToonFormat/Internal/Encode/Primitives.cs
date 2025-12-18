#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Primitive value encoding, key encoding, and header formatting utilities.
    /// Aligned with TypeScript encode/primitives.ts
    /// Optimized with caching and Span-based operations.
    /// </summary>
    internal static class Primitives
    {
        // #region Cached strings for common values
        
        // Cache for small integers (-100 to 1000) - covers most common cases
        private static readonly string[] SmallIntCache = InitSmallIntCache();
        private const int SmallIntMin = -100;
        private const int SmallIntMax = 1000;
        
        private static string[] InitSmallIntCache()
        {
            var cache = new string[SmallIntMax - SmallIntMin + 1];
            for (int i = SmallIntMin; i <= SmallIntMax; i++)
            {
                cache[i - SmallIntMin] = i.ToString(CultureInfo.InvariantCulture);
            }
            return cache;
        }

        // Cache for common double values
        private static readonly Dictionary<double, string> CommonDoubleCache = new()
        {
            { 0.0, "0" },
            { 1.0, "1" },
            { -1.0, "-1" },
            { 0.5, "0.5" },
            { 0.25, "0.25" },
            { 0.1, "0.1" },
            { 0.01, "0.01" },
        };
        
        // #endregion

        // #region Primitive encoding

        /// <summary>
        /// Encodes a primitive JSON value (null, boolean, number, or string) to its TOON representation.
        /// Optimized with caching for common values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EncodePrimitive(JsonNode? value, char delimiter = Constants.COMMA)
        {
            if (value == null)
                return Constants.NULL_LITERAL;

            if (value is JsonValue jsonValue)
            {
                // Boolean - fastest path
                if (jsonValue.TryGetValue<bool>(out var boolVal))
                    return boolVal ? Constants.TRUE_LITERAL : Constants.FALSE_LITERAL;

                // Integer - use cache for small values
                if (jsonValue.TryGetValue<int>(out var intVal))
                {
                    if (intVal >= SmallIntMin && intVal <= SmallIntMax)
                        return SmallIntCache[intVal - SmallIntMin];
                    return intVal.ToString(CultureInfo.InvariantCulture);
                }

                if (jsonValue.TryGetValue<long>(out var longVal))
                {
                    if (longVal >= SmallIntMin && longVal <= SmallIntMax)
                        return SmallIntCache[(int)longVal - SmallIntMin];
                    return longVal.ToString(CultureInfo.InvariantCulture);
                }

                // Double - check cache first
                if (jsonValue.TryGetValue<double>(out var doubleVal))
                {
                    if (CommonDoubleCache.TryGetValue(doubleVal, out var cached))
                        return cached;
                    return FormatCanonicalNumber(doubleVal);
                }

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
        /// Optimized to start at higher precision for faster convergence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatCanonicalNumber(double value)
        {
            // Handle special cases
            if (double.IsNaN(value) || double.IsInfinity(value))
                return Constants.NULL_LITERAL;

            // Check if value is an integer - use cache for small integers
            if (value == Math.Truncate(value) && Math.Abs(value) < 1e15)
            {
                var longVal = (long)value;
                if (longVal >= SmallIntMin && longVal <= SmallIntMax)
                    return SmallIntCache[(int)longVal - SmallIntMin];
                return longVal.ToString(CultureInfo.InvariantCulture);
            }

            // For most practical values, G15 is sufficient and round-trips correctly
            // Start there instead of G1 to avoid unnecessary iterations
            var formatted = value.ToString("G15", CultureInfo.InvariantCulture);
            
            // Check for exponent notation
            bool hasExponent = formatted.AsSpan().IndexOfAny('E', 'e') >= 0;
            
            if (hasExponent)
            {
                // Parse back to check round-trip
                if (double.TryParse(formatted, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed == value)
                {
                    formatted = ExpandExponent(value, formatted);
                    return RemoveTrailingZerosOptimized(formatted);
                }
            }
            else
            {
                // Check if G15 round-trips correctly
                if (double.TryParse(formatted, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue) && parsedValue == value)
                {
                    return RemoveTrailingZerosOptimized(formatted);
                }
            }

            // Fallback to G17 for full precision
            var fullPrecision = value.ToString("G17", CultureInfo.InvariantCulture);
            if (fullPrecision.AsSpan().IndexOfAny('E', 'e') >= 0)
            {
                fullPrecision = ExpandExponent(value, fullPrecision);
            }
            return RemoveTrailingZerosOptimized(fullPrecision);
        }
        
        /// <summary>
        /// Optimized trailing zeros removal using Span operations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string RemoveTrailingZerosOptimized(string formatted)
        {
            var span = formatted.AsSpan();
            int dotIndex = span.IndexOf('.');
            
            if (dotIndex < 0)
                return formatted;

            int endIndex = span.Length;
            
            // Find last non-zero character after decimal point
            while (endIndex > dotIndex + 1 && span[endIndex - 1] == '0')
            {
                endIndex--;
            }
            
            // If we removed all fractional digits, remove the decimal point too
            if (endIndex == dotIndex + 1)
            {
                endIndex = dotIndex;
            }
            
            if (endIndex == span.Length)
                return formatted;
                
            return new string(span.Slice(0, endIndex));
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
        /// Optimized to use StringBuilder and avoid LINQ allocations.
        /// </summary>
        public static string EncodeAndJoinPrimitives(IEnumerable<JsonNode?> values, char delimiter = Constants.COMMA)
        {
            var sb = new StringBuilder();
            bool first = true;

            foreach (var value in values)
            {
                if (!first)
                {
                    sb.Append(delimiter);
                }
                first = false;
                sb.Append(EncodePrimitive(value, delimiter));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encodes and joins primitive values from a Span (zero-allocation version).
        /// Uses thread-static StringBuilder for reduced allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EncodeAndJoinPrimitivesSpan(ReadOnlySpan<JsonNode?> values, char delimiter = Constants.COMMA)
        {
            if (values.Length == 0)
                return string.Empty;

            // Use a rough estimate for capacity: average 10 chars per value
            var sb = new StringBuilder(values.Length * 12);

            sb.Append(EncodePrimitive(values[0], delimiter));
            
            for (int i = 1; i < values.Length; i++)
            {
                sb.Append(delimiter);
                sb.Append(EncodePrimitive(values[i], delimiter));
            }

            return sb.ToString();
        }

        // #endregion

        // #region Header formatters

        /// <summary>
        /// Formats an array header with optional key, length marker, delimiter, and field names.
        /// Optimized with StringBuilder to avoid string concatenation allocations.
        /// Examples:
        /// - "[3]:" for unnamed array of 3 items
        /// - "items[5]:" for named array
        /// - "users[#2]{name,age}:" for tabular format with length marker
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatHeader(
            int length,
            string? key = null,
            IReadOnlyList<string>? fields = null,
            char? delimiter = null)
        {
            var delimiterChar = delimiter ?? Constants.DEFAULT_DELIMITER_CHAR;
            
            // Estimate capacity: key + brackets + length + fields
            int estimatedLength = (key?.Length ?? 0) + 10 + (fields?.Count ?? 0) * 15;
            var sb = new StringBuilder(estimatedLength);

            // Add key if present
            if (!string.IsNullOrEmpty(key))
            {
                sb.Append(EncodeKey(key));
            }

            // Add array length with optional delimiter suffix
            sb.Append(Constants.OPEN_BRACKET);
            
            // Use cached string for small lengths
            if (length >= SmallIntMin && length <= SmallIntMax)
                sb.Append(SmallIntCache[length - SmallIntMin]);
            else
                sb.Append(length);
            
            if (delimiterChar != Constants.DEFAULT_DELIMITER_CHAR)
            {
                sb.Append(delimiterChar);
            }
            
            sb.Append(Constants.CLOSE_BRACKET);

            // Add field names for tabular format
            if (fields != null && fields.Count > 0)
            {
                sb.Append(Constants.OPEN_BRACE);
                
                for (int i = 0; i < fields.Count; i++)
                {
                    if (i > 0)
                        sb.Append(delimiterChar);
                    sb.Append(EncodeKey(fields[i]));
                }
                
                sb.Append(Constants.CLOSE_BRACE);
            }

            sb.Append(Constants.COLON);

            return sb.ToString();
        }

        // #endregion
    }
}
