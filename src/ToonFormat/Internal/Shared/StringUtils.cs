#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ToonFormat.Internal.Shared
{
    /// <summary>
    /// String utilities, aligned with TypeScript version shared/string-utils.ts:
    /// - EscapeString: Escapes special characters during encoding
    /// - UnescapeString: Restores escape sequences during decoding
    /// - FindClosingQuote: Finds the position of the matching closing quote, considering escapes
    /// - FindUnquotedChar: Finds the position of the target character not inside quotes
    /// Optimized with Span-based operations where possible.
    /// </summary>
    internal static class StringUtils
    {
        /// <summary>
        /// Escapes special characters: backslash, quotes, newlines, carriage returns, tabs.
        /// Optimized to scan first and only allocate if needed.
        /// Equivalent to TS escapeString.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            // Fast path: check if escaping is needed at all
            var span = value.AsSpan();
            bool needsEscaping = false;
            for (int i = 0; i < span.Length; i++)
            {
                var ch = span[i];
                if (ch == '\\' || ch == '"' || ch == '\n' || ch == '\r' || ch == '\t')
                {
                    needsEscaping = true;
                    break;
                }
            }

            if (!needsEscaping)
                return value;

            // Slow path: build escaped string
            var sb = new StringBuilder(value.Length + 8);
            for (int i = 0; i < span.Length; i++)
            {
                var ch = span[i];
                switch (ch)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        // Normalize \r\n to \n, but standalone \r stays as \r
                        if (i + 1 < span.Length && span[i + 1] == '\n')
                        {
                            i++; // Skip the \n, we'll output \n for the whole \r\n sequence
                            sb.Append("\\n");
                        }
                        else
                        {
                            sb.Append("\\r");
                        }
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Unescapes the string, supporting \n, \t, \r, \\, \". Invalid sequences throw <see cref="ToonFormatException"/>.
        /// Optimized to check for escapes first and avoid allocation when not needed.
        /// Equivalent to TS unescapeString.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string UnescapeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            // Fast path: check if unescaping is needed at all
            if (value.AsSpan().IndexOf('\\') < 0)
                return value;

            var sb = new StringBuilder(value.Length);
            var span = value.AsSpan();
            int i = 0;
            
            while (i < span.Length)
            {
                var ch = span[i];
                if (ch == Constants.BACKSLASH)
                {
                    if (i + 1 >= span.Length)
                        throw ToonFormatException.Syntax("Invalid escape sequence: backslash at end of string");

                    var next = span[i + 1];
                    switch (next)
                    {
                        case 'n':
                            sb.Append(Constants.NEWLINE);
                            i += 2;
                            continue;
                        case 't':
                            sb.Append(Constants.TAB);
                            i += 2;
                            continue;
                        case 'r':
                            sb.Append(Constants.CARRIAGE_RETURN);
                            i += 2;
                            continue;
                        case '\\':
                            sb.Append(Constants.BACKSLASH);
                            i += 2;
                            continue;
                        case '"':
                            sb.Append(Constants.DOUBLE_QUOTE);
                            i += 2;
                            continue;
                        default:
                            throw ToonFormatException.Syntax($"Invalid escape sequence: \\{next}");
                    }
                }

                sb.Append(ch);
                i++;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Finds the position of the next double quote in the string starting from 'start', considering escapes.
        /// Returns -1 if not found. Equivalent to TS findClosingQuote.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FindClosingQuote(string content, int start)
        {
            var span = content.AsSpan();
            int i = start + 1;
            while (i < span.Length)
            {
                var ch = span[i];
                
                // Skip the next character when encountering an escape inside quotes
                if (ch == Constants.BACKSLASH && i + 1 < span.Length)
                {
                    i += 2;
                    continue;
                }

                if (ch == Constants.DOUBLE_QUOTE)
                    return i;

                i++;
            }
            return -1;
        }

        /// <summary>
        /// Finds the position of the target character not inside quotes; returns -1 if not found.
        /// Escape sequences inside quotes are skipped. Equivalent to TS findUnquotedChar.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FindUnquotedChar(string content, char target, int start = 0)
        {
            var span = content.AsSpan();
            bool inQuotes = false;
            int i = start;

            while (i < span.Length)
            {
                var ch = span[i];
                
                if (inQuotes && ch == Constants.BACKSLASH && i + 1 < span.Length)
                {
                    // Skip the next character for escape sequences inside quotes
                    i += 2;
                    continue;
                }

                if (ch == Constants.DOUBLE_QUOTE)
                {
                    inQuotes = !inQuotes;
                    i++;
                    continue;
                }

                if (!inQuotes && ch == target)
                    return i;

                i++;
            }

            return -1;
        }

        /// <summary>
        /// Generates a quoted string literal, escaping internal characters as necessary.
        /// Note: Whether quotes are needed should be determined by the caller based on ValidationShared rules.
        /// </summary>
        internal static string Quote(string value)
        {
            return $"\"{EscapeString(value)}\"";
        }
    }
}