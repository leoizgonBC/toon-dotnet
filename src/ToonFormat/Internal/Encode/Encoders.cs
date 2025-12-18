#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace ToonFormat.Internal.Encode
{
    /// <summary>
    /// Options for encoding TOON format, aligned with TypeScript ResolvedEncodeOptions.
    /// </summary>
    internal class ResolvedEncodeOptions
    {
        public int Indent { get; set; } = 2;
        public char Delimiter { get; set; } = Constants.COMMA;
        public ToonKeyFolding KeyFolding { get; set; } = ToonKeyFolding.Off;
        public int FlattenDepth { get; set; } = int.MaxValue;
    }

    /// <summary>
    /// Main encoding functions for converting normalized JsonNode values to TOON format.
    /// Aligned with TypeScript encode/encoders.ts
    /// </summary>
    internal static class Encoders
    {
        // #region Encode normalized JsonValue

        /// <summary>
        /// Encodes a normalized JsonNode value to TOON format string.
        /// </summary>
        public static string EncodeValue(JsonNode? value, ResolvedEncodeOptions options)
        {
            if (Normalize.IsJsonPrimitive(value))
            {
                return Primitives.EncodePrimitive(value, options.Delimiter);
            }

            var writer = new LineWriter(options.Indent);

            if (Normalize.IsJsonArray(value))
            {
                EncodeArray(null, (JsonArray)value!, writer, 0, options);
            }
            else if (Normalize.IsJsonObject(value))
            {
                EncodeObject((JsonObject)value!, writer, 0, options);
            }

            return writer.ToString();
        }

        // #endregion

        // #region Object encoding

        /// <summary>
        /// Encodes a JsonObject as key-value pairs.
        /// Optimized to avoid repeated allocations.
        /// </summary>
        public static void EncodeObject(JsonObject value, LineWriter writer, int depth, ResolvedEncodeOptions options, IReadOnlySet<string>? rootLiteralKeys = null,
            string? pathPrefix = null, int? remainingDepth = null)
        {
            var dict = (IDictionary<string, JsonNode?>)value;
            var keys = dict.Keys;

            // At root level (depth 0), collect all literal dotted keys for collision checking
            if (depth == 0 && rootLiteralKeys == null)
            {
                HashSet<string>? dottedKeys = null;
                foreach (var k in keys)
                {
                    if (k.Contains('.'))
                    {
                        dottedKeys ??= new HashSet<string>();
                        dottedKeys.Add(k);
                    }
                }
                rootLiteralKeys = dottedKeys ?? (IReadOnlySet<string>)Array.Empty<string>().ToHashSet();
            }

            var effectiveFlattenDepth = remainingDepth ?? options.FlattenDepth;

            // Cache keys as array to avoid repeated ToImmutableArray calls
            var keysArray = keys as IReadOnlyCollection<string> ?? keys.ToArray();

            foreach (var kvp in value)
            {
                EncodeKeyValuePair(
                    kvp.Key,
                    kvp.Value,
                    writer,
                    depth,
                    options,
                    keysArray,
                    rootLiteralKeys,
                    pathPrefix,
                    effectiveFlattenDepth
                );
            }
        }

        /// <summary>
        /// Encodes a single key-value pair.
        /// </summary>
        public static void EncodeKeyValuePair(
            string key,
            JsonNode? value,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options,
            IReadOnlyCollection<string>? siblings = null,
            IReadOnlySet<string>? rootLiteralKeys = null,
            string? pathPrefix = null,
            int? flattenDepth = null)
        {
            var currentPath = pathPrefix != null ? $"{pathPrefix}{Constants.DOT}{key}" : key;
            var effectiveFlattenDepth = flattenDepth ?? options.FlattenDepth;

            if (options.KeyFolding == ToonKeyFolding.Safe && siblings != null)
            {
                var foldResult = Folding.TryFoldKeyChain(key, value, siblings, options, rootLiteralKeys, pathPrefix, effectiveFlattenDepth);

                if (foldResult is not null)
                {
                    var foldedKey = foldResult.FoldedKey;
                    var remainder = foldResult.Remainder;
                    var leafValue = foldResult.LeafValue;
                    var segmentCount = foldResult.SegmentCount;

                    var encodedFoldedKey = Primitives.EncodeKey(foldedKey);

                    // Case 1: Fully folded to a leaf value
                    if (remainder is null)
                    {
                        // The folded chain ended at a leaf (primitive, array, or empty object)
                        if (Normalize.IsJsonPrimitive(leafValue))
                        {
                            writer.Push(depth, $"{encodedFoldedKey}: {Primitives.EncodePrimitive(leafValue, options.Delimiter)}");
                            return;
                        }
                        else if (Normalize.IsJsonArray(leafValue))
                        {
                            EncodeArray(foldedKey, leafValue.AsArray(), writer, depth, options);
                            return;
                        }
                        else if (Normalize.IsEmptyObject(leafValue))
                        {
                            writer.Push(depth, $"{encodedFoldedKey}:");
                            return;
                        }
                    }

                    // Case 2: Partially folded with a tail object
                    if (Normalize.IsJsonObject(remainder))
                    {
                        writer.Push(depth, $"{encodedFoldedKey}:");
                        // Calculate remaining depth budget (subtract segments already folded)
                        var remainingDepth = effectiveFlattenDepth - segmentCount;
                        var foldedPath = pathPrefix != null ? $"{pathPrefix}{Constants.DOT}{foldedKey}" : foldedKey;

                        EncodeObject(remainder!.AsObject(), writer, depth + 1, options, rootLiteralKeys, foldedPath, remainingDepth);

                        return;
                    }
                }
            }

            // No folding applied  use standard encoding
            var encodedKey = Primitives.EncodeKey(key);

            if (Normalize.IsJsonPrimitive(value))
            {
                writer.Push(depth, $"{encodedKey}{Constants.COLON} {Primitives.EncodePrimitive(value, options.Delimiter)}");
            }
            else if (Normalize.IsJsonArray(value))
            {
                EncodeArray(key, (JsonArray)value!, writer, depth, options);
            }
            else if (Normalize.IsJsonObject(value))
            {
                writer.Push(depth, $"{encodedKey}{Constants.COLON}");
                var obj = value!.AsObject();
                if (!Normalize.IsEmptyObject(obj))
                {
                    EncodeObject(obj, writer, depth + 1, options, rootLiteralKeys, currentPath, effectiveFlattenDepth);
                }
            }
        }

        // #endregion

        // #region Array encoding

        /// <summary>
        /// Encodes a JsonArray with appropriate formatting (inline, tabular, or expanded).
        /// Optimized to avoid LINQ allocations.
        /// </summary>
        public static void EncodeArray(
            string? key,
            JsonArray value,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            if (value.Count == 0)
            {
                var header = Primitives.FormatHeader(0, key, null, options.Delimiter);
                writer.Push(depth, header);
                return;
            }

            // Primitive array
            if (Normalize.IsArrayOfPrimitives(value))
            {
                var formatted = EncodeInlineArrayLine(value, options.Delimiter, key);
                writer.Push(depth, formatted);
                return;
            }

            // Array of arrays (all primitives)
            if (Normalize.IsArrayOfArrays(value))
            {
                // Check all are primitive arrays without LINQ
                bool allPrimitiveArrays = true;
                for (int i = 0; i < value.Count; i++)
                {
                    if (!(value[i] is JsonArray arr && Normalize.IsArrayOfPrimitives(arr)))
                    {
                        allPrimitiveArrays = false;
                        break;
                    }
                }

                if (allPrimitiveArrays)
                {
                    EncodeArrayOfArraysAsListItemsDirect(key, value, writer, depth, options);
                    return;
                }
            }

            // Array of objects
            if (Normalize.IsArrayOfObjects(value))
            {
                var header = ExtractTabularHeaderDirect(value);
                if (header != null)
                {
                    EncodeArrayOfObjectsAsTabularDirect(key, value, header, writer, depth, options);
                }
                else
                {
                    EncodeMixedArrayAsListItems(key, value, writer, depth, options);
                }
                return;
            }

            // Mixed array: fallback to expanded format
            EncodeMixedArrayAsListItems(key, value, writer, depth, options);
        }

        // #endregion

        // #region Array of arrays (expanded format)

        /// <summary>
        /// Encodes an array of arrays as list items.
        /// </summary>
        public static void EncodeArrayOfArraysAsListItems(
            string? prefix,
            IReadOnlyList<JsonArray> values,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var header = Primitives.FormatHeader(values.Count, prefix, null, options.Delimiter);
            writer.Push(depth, header);

            foreach (var arr in values)
            {
                if (Normalize.IsArrayOfPrimitives(arr))
                {
                    var inline = EncodeInlineArrayLine(arr, options.Delimiter, null);
                    writer.PushListItem(depth + 1, inline);
                }
            }
        }

        /// <summary>
        /// Encodes an array of arrays as list items directly from JsonArray (no allocation).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeArrayOfArraysAsListItemsDirect(
            string? prefix,
            JsonArray values,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var header = Primitives.FormatHeader(values.Count, prefix, null, options.Delimiter);
            writer.Push(depth, header);

            for (int i = 0; i < values.Count; i++)
            {
                var arr = (JsonArray)values[i]!;
                if (Normalize.IsArrayOfPrimitives(arr))
                {
                    var inline = EncodeInlineArrayLine(arr, options.Delimiter, null);
                    writer.PushListItem(depth + 1, inline);
                }
            }
        }

        /// <summary>
        /// Encodes an array as a single inline line with header.
        /// </summary>
        public static string EncodeInlineArrayLine(
            JsonArray values,
            char delimiter,
            string? prefix = null)
        {
            var header = Primitives.FormatHeader(values.Count, prefix, null, delimiter);

            if (values.Count == 0)
            {
                return header;
            }

            var joinedValue = Primitives.EncodeAndJoinPrimitives(values, delimiter);
            return $"{header} {joinedValue}";
        }

        // #endregion

        // #region Array of objects (tabular format)

        /// <summary>
        /// Encodes an array of objects in tabular format.
        /// </summary>
        public static void EncodeArrayOfObjectsAsTabular(
            string? prefix,
            IReadOnlyList<JsonObject> rows,
            IReadOnlyList<string> header,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var formattedHeader = Primitives.FormatHeader(rows.Count, prefix, header, options.Delimiter);
            writer.Push(depth, formattedHeader);

            WriteTabularRows(rows, header, writer, depth + 1, options);
        }

        /// <summary>
        /// Extracts a uniform header from an array of objects if all objects have the same keys.
        /// Returns null if the array cannot be represented in tabular format.
        /// </summary>
        public static IReadOnlyList<string>? ExtractTabularHeader(IReadOnlyList<JsonObject> rows)
        {
            if (rows.Count == 0)
                return null;

            var firstRow = rows[0];
            var firstKeys = firstRow.Select(kvp => kvp.Key).ToList();

            if (firstKeys.Count == 0)
                return null;

            if (IsTabularArray(rows, firstKeys))
            {
                return firstKeys;
            }

            return null;
        }

        /// <summary>
        /// Extracts a uniform header directly from JsonArray without intermediate allocations.
        /// Returns null if the array cannot be represented in tabular format.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string[]? ExtractTabularHeaderDirect(JsonArray rows)
        {
            if (rows.Count == 0)
                return null;

            var firstRow = rows[0] as JsonObject;
            if (firstRow == null || firstRow.Count == 0)
                return null;

            // Extract keys from first row without LINQ
            var keyCount = firstRow.Count;
            var firstKeys = new string[keyCount];
            int idx = 0;
            foreach (var kvp in firstRow)
            {
                firstKeys[idx++] = kvp.Key;
            }

            // Check if tabular format is possible
            if (IsTabularArrayDirect(rows, firstKeys))
            {
                return firstKeys;
            }

            return null;
        }

        /// <summary>
        /// Checks if an array of objects can be represented in tabular format.
        /// All objects must have the same keys and all values must be primitives.
        /// </summary>
        public static bool IsTabularArray(
            IReadOnlyList<JsonObject> rows,
            IReadOnlyList<string> header)
        {
            foreach (var row in rows)
            {
                var keys = row.Select(kvp => kvp.Key).ToList();

                // All objects must have the same keys (but order can differ)
                if (keys.Count != header.Count)
                    return false;

                // Check that all header keys exist in the row and all values are primitives
                foreach (var key in header)
                {
                    if (!row.ContainsKey(key))
                        return false;

                    if (!Normalize.IsJsonPrimitive(row[key]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Optimized version that works directly with JsonArray without LINQ allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTabularArrayDirect(JsonArray rows, string[] header)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i] as JsonObject;
                if (row == null)
                    return false;

                // All objects must have the same number of keys
                if (row.Count != header.Length)
                    return false;

                // Check that all header keys exist in the row and all values are primitives
                for (int j = 0; j < header.Length; j++)
                {
                    if (!row.TryGetPropertyValue(header[j], out var value))
                        return false;

                    if (!Normalize.IsJsonPrimitive(value))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Writes tabular rows to the writer.
        /// Optimized to avoid LINQ allocations.
        /// </summary>
        private static void WriteTabularRows(
            IReadOnlyList<JsonObject> rows,
            IReadOnlyList<string> header,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            // Pre-allocate list for row values to avoid repeated allocations
            var rowValues = new List<JsonNode?>(header.Count);

            foreach (var row in rows)
            {
                rowValues.Clear();
                for (int i = 0; i < header.Count; i++)
                {
                    rowValues.Add(row[header[i]]);
                }
                var joinedValue = Primitives.EncodeAndJoinPrimitives(rowValues, options.Delimiter);
                writer.Push(depth, joinedValue);
            }
        }

        /// <summary>
        /// Encodes an array of objects in tabular format directly from JsonArray.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeArrayOfObjectsAsTabularDirect(
            string? prefix,
            JsonArray rows,
            string[] header,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var formattedHeader = Primitives.FormatHeader(rows.Count, prefix, header, options.Delimiter);
            writer.Push(depth, formattedHeader);

            WriteTabularRowsDirect(rows, header, writer, depth + 1, options);
        }

        /// <summary>
        /// Writes tabular rows directly from JsonArray.
        /// Uses ArrayPool for temporary buffer to avoid allocations.
        /// </summary>
        private static void WriteTabularRowsDirect(
            JsonArray rows,
            string[] header,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            // Rent a buffer for row values
            var rowValues = ArrayPool<JsonNode?>.Shared.Rent(header.Length);
            try
            {
                for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
                {
                    var row = (JsonObject)rows[rowIdx]!;
                    for (int i = 0; i < header.Length; i++)
                    {
                        rowValues[i] = row[header[i]];
                    }
                    var joinedValue = Primitives.EncodeAndJoinPrimitivesSpan(
                        new ReadOnlySpan<JsonNode?>(rowValues, 0, header.Length), 
                        options.Delimiter);
                    writer.Push(depth, joinedValue);
                }
            }
            finally
            {
                ArrayPool<JsonNode?>.Shared.Return(rowValues, clearArray: true);
            }
        }

        // #endregion

        // #region Array of objects (expanded format)

        /// <summary>
        /// Encodes a mixed array as list items (expanded format).
        /// </summary>
        public static void EncodeMixedArrayAsListItems(
            string? prefix,
            JsonArray items,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            var header = Primitives.FormatHeader(items.Count, prefix, null, options.Delimiter);
            writer.Push(depth, header);

            foreach (var item in items)
            {
                EncodeListItemValue(item, writer, depth + 1, options);
            }
        }

        /// <summary>
        /// Encodes an object as a list item with special formatting for the first property.
        /// </summary>
        public static void EncodeObjectAsListItem(JsonObject obj, LineWriter writer, int depth, ResolvedEncodeOptions options)
        {
            var keys = obj.Select(kvp => kvp.Key).ToList();

            if (keys.Count == 0)
            {
                writer.Push(depth, Constants.LIST_ITEM_MARKER.ToString());
                return;
            }

            // First key-value on the same line as "- "
            var firstKey = keys[0];
            var encodedKey = Primitives.EncodeKey(firstKey);
            var firstValue = obj[firstKey];

            if (Normalize.IsJsonPrimitive(firstValue))
            {
                writer.PushListItem(depth, $"{encodedKey}{Constants.COLON} {Primitives.EncodePrimitive(firstValue, options.Delimiter)}");
            }
            else if (Normalize.IsJsonArray(firstValue))
            {
                var arr = (JsonArray)firstValue!;

                if (Normalize.IsArrayOfPrimitives(arr))
                {
                    // Inline format for primitive arrays
                    var formatted = EncodeInlineArrayLine(arr, options.Delimiter, firstKey);
                    writer.PushListItem(depth, formatted);
                }
                else if (Normalize.IsArrayOfObjects(arr))
                {
                    // Check if array of objects can use tabular format
                    var objects = arr.Cast<JsonObject>().ToList();
                    var header = ExtractTabularHeader(objects);

                    if (header != null)
                    {
                        // Tabular format for uniform arrays of objects
                        var formattedHeader = Primitives.FormatHeader(arr.Count, firstKey, header, options.Delimiter);
                        writer.PushListItem(depth, formattedHeader);
                        WriteTabularRows(objects, header, writer, depth + 1, options);
                    }
                    else
                    {
                        // Fall back to list format for non-uniform arrays of objects
                        writer.PushListItem(depth, $"{encodedKey}{Constants.OPEN_BRACKET}{arr.Count}{Constants.CLOSE_BRACKET}{Constants.COLON}");
                        foreach (var itemObj in arr.OfType<JsonObject>())
                        {
                            EncodeObjectAsListItem(itemObj, writer, depth + 1, options);
                        }
                    }
                }
                else
                {
                    // Complex arrays on separate lines (array of arrays, etc.)
                    writer.PushListItem(depth, $"{encodedKey}{Constants.OPEN_BRACKET}{arr.Count}{Constants.CLOSE_BRACKET}{Constants.COLON}");

                    // Encode array contents at depth + 1
                    foreach (var item in arr)
                    {
                        EncodeListItemValue(item, writer, depth + 1, options);
                    }
                }
            }
            else if (Normalize.IsJsonObject(firstValue))
            {
                var nestedObj = (JsonObject)firstValue!;

                if (nestedObj.Count == 0)
                {
                    writer.PushListItem(depth, $"{encodedKey}{Constants.COLON}");
                }
                else
                {
                    writer.PushListItem(depth, $"{encodedKey}{Constants.COLON}");
                    EncodeObject(nestedObj, writer, depth + 2, options);
                }
            }

            // Remaining keys on indented lines
            for (int i = 1; i < keys.Count; i++)
            {
                var key = keys[i];
                EncodeKeyValuePair(key, obj[key], writer, depth + 1, options);
            }
        }

        // #endregion

        // #region List item encoding helpers

        /// <summary>
        /// Encodes a value as a list item.
        /// </summary>
        private static void EncodeListItemValue(
            JsonNode? value,
            LineWriter writer,
            int depth,
            ResolvedEncodeOptions options)
        {
            if (Normalize.IsJsonPrimitive(value))
            {
                writer.PushListItem(depth, Primitives.EncodePrimitive(value, options.Delimiter));
            }
            else if (Normalize.IsJsonArray(value) && Normalize.IsArrayOfPrimitives((JsonArray)value!))
            {
                var arr = (JsonArray)value!;
                var inline = EncodeInlineArrayLine(arr, options.Delimiter, null);
                writer.PushListItem(depth, inline);
            }
            else if (Normalize.IsJsonObject(value))
            {
                EncodeObjectAsListItem((JsonObject)value!, writer, depth, options);
            }
        }

        // #endregion
    }
}
