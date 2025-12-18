#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Helper class for building indented lines of TOON output.
    /// Optimized with pre-cached indentation strings and efficient StringBuilder usage.
    /// </summary>
    internal class LineWriter
    {
        private readonly StringBuilder _sb;
        private readonly int _indentSize;
        private bool _hasContent;
        
        // Cache indentation strings for common depths (0-20)
        private static readonly string[] IndentCache2 = InitIndentCache(2, 21);
        private static readonly string[] IndentCache4 = InitIndentCache(4, 21);
        private readonly string[] _indentCache;

        private static string[] InitIndentCache(int indentSize, int maxDepth)
        {
            var cache = new string[maxDepth];
            for (int i = 0; i < maxDepth; i++)
            {
                cache[i] = new string(' ', i * indentSize);
            }
            return cache;
        }

        /// <summary>
        /// Creates a new LineWriter with the specified indentation size.
        /// </summary>
        /// <param name="indentSize">Number of spaces per indentation level.</param>
        public LineWriter(int indentSize)
        {
            _indentSize = indentSize;
            _sb = new StringBuilder(512); // Pre-allocate reasonable initial capacity
            _hasContent = false;
            
            // Use pre-built cache for common indent sizes
            _indentCache = indentSize switch
            {
                2 => IndentCache2,
                4 => IndentCache4,
                _ => InitIndentCache(indentSize, 21)
            };
        }

        /// <summary>
        /// Gets the cached indentation string for the given depth.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetIndent(int depth)
        {
            if (depth < _indentCache.Length)
                return _indentCache[depth];
            
            // Fallback for very deep nesting
            return new string(' ', depth * _indentSize);
        }

        /// <summary>
        /// Pushes a new line with the specified depth and content.
        /// </summary>
        /// <param name="depth">Indentation depth level.</param>
        /// <param name="content">The content of the line.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(int depth, string content)
        {
            if (_hasContent)
            {
                _sb.Append('\n');
            }
            _hasContent = true;

            // Use cached indentation string
            _sb.Append(GetIndent(depth));
            _sb.Append(content);
        }

        /// <summary>
        /// Pushes a list item (prefixed with "- ") at the specified depth.
        /// </summary>
        /// <param name="depth">Indentation depth level.</param>
        /// <param name="content">The content after the list item marker.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushListItem(int depth, string content)
        {
            if (_hasContent)
            {
                _sb.Append('\n');
            }
            _hasContent = true;

            // Use cached indentation string
            _sb.Append(GetIndent(depth));
            _sb.Append(Constants.LIST_ITEM_PREFIX);
            _sb.Append(content);
        }

        /// <summary>
        /// Returns the complete output as a single string.
        /// </summary>
        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
