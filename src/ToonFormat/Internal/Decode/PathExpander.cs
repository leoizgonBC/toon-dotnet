#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using ToonFormat.Internal.Shared;

namespace ToonFormat.Internal.Decode
{
    /// <summary>
    /// Implements path expansion for dotted keys (TOON Spec §13.4).
    /// Expands eligible dotted keys into nested object structures with deep-merge semantics.
    /// </summary>
    internal static class PathExpander
    {
        /// <summary>
        /// Expands dotted keys in a JsonObject into nested object structures.
        /// </summary>
        /// <param name="obj">The object to expand</param>
        /// <param name="strict">If true, errors on conflicts; if false, applies LWW (last-write-wins)</param>
        /// <param name="quotedKeys">Set of keys that were originally quoted and should NOT be expanded</param>
        /// <returns>A new JsonObject with expanded paths</returns>
        public static JsonObject ExpandPaths(JsonObject obj, bool strict, HashSet<string>? quotedKeys = null)
        {
            var result = new JsonObject();
            quotedKeys ??= new HashSet<string>();

            foreach (var kvp in obj)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Check if key should be expanded (not if it was quoted)
                if (!quotedKeys.Contains(key) && ShouldExpand(key))
                {
                    var segments = key.Split(Constants.DOT);
                    SetNestedValue(result, segments, CloneValue(value), strict);
                }
                else
                {
                    // Key is not expandable - check for conflict with existing key
                    if (result.ContainsKey(key))
                    {
                        if (strict)
                        {
                            throw ToonFormatException.Validation(
                                $"Expansion conflict: key '{key}' already exists");
                        }
                        // LWW: overwrite
                        result[key] = CloneValue(value);
                    }
                    else
                    {
                        result[key] = CloneValue(value);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a key should be expanded based on safe mode rules.
        /// Only expands unquoted keys where ALL segments are IdentifierSegments.
        /// </summary>
        private static bool ShouldExpand(string key)
        {
            // Key must contain a dot to be expandable
            if (!key.Contains(Constants.DOT))
                return false;

            var segments = key.Split(Constants.DOT);

            // All segments must be valid IdentifierSegments (§1.9)
            foreach (var segment in segments)
            {
                if (!ValidationShared.IsIdentifierSegment(segment))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sets a nested value in the result object using the path segments.
        /// Implements deep-merge semantics with conflict detection.
        /// </summary>
        private static void SetNestedValue(JsonObject root, string[] segments, JsonNode? value, bool strict)
        {
            var current = root;

            // Navigate/create intermediate objects
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];

                if (current.TryGetPropertyValue(segment, out var existing))
                {
                    if (existing is JsonObject existingObj)
                    {
                        // Continue navigating into existing object
                        current = existingObj;
                    }
                    else
                    {
                        // Conflict: trying to create object where non-object exists
                        var path = string.Join(".", segments, 0, i + 1);
                        if (strict)
                        {
                            throw ToonFormatException.Validation(
                                $"Expansion conflict at path '{path}' (object vs {GetTypeName(existing)})");
                        }
                        // LWW: replace with new object and continue
                        var newObj = new JsonObject();
                        current[segment] = newObj;
                        current = newObj;
                    }
                }
                else
                {
                    // Create new intermediate object
                    var newObj = new JsonObject();
                    current[segment] = newObj;
                    current = newObj;
                }
            }

            // Set the final value
            var finalSegment = segments[segments.Length - 1];

            if (current.TryGetPropertyValue(finalSegment, out var existingFinal))
            {
                // Handle conflict at leaf level
                if (existingFinal is JsonObject existingFinalObj && value is JsonObject valueObj)
                {
                    // Both are objects - deep merge
                    DeepMerge(existingFinalObj, valueObj, strict, string.Join(".", segments));
                }
                else
                {
                    // Conflict: different types or non-objects
                    var path = string.Join(".", segments);
                    if (strict)
                    {
                        throw ToonFormatException.Validation(
                            $"Expansion conflict at path '{path}' ({GetTypeName(existingFinal)} vs {GetTypeName(value)})");
                    }
                    // LWW: overwrite
                    current[finalSegment] = value;
                }
            }
            else
            {
                current[finalSegment] = value;
            }
        }

        /// <summary>
        /// Deep merges source into target with conflict detection.
        /// </summary>
        private static void DeepMerge(JsonObject target, JsonObject source, bool strict, string basePath)
        {
            foreach (var kvp in source)
            {
                var key = kvp.Key;
                var sourceValue = kvp.Value;
                var currentPath = string.IsNullOrEmpty(basePath) ? key : $"{basePath}.{key}";

                if (target.TryGetPropertyValue(key, out var targetValue))
                {
                    if (targetValue is JsonObject targetObj && sourceValue is JsonObject sourceObj)
                    {
                        // Both are objects - recurse
                        DeepMerge(targetObj, sourceObj, strict, currentPath);
                    }
                    else
                    {
                        // Conflict
                        if (strict)
                        {
                            throw ToonFormatException.Validation(
                                $"Expansion conflict at path '{currentPath}' ({GetTypeName(targetValue)} vs {GetTypeName(sourceValue)})");
                        }
                        // LWW: overwrite
                        target[key] = CloneValue(sourceValue);
                    }
                }
                else
                {
                    target[key] = CloneValue(sourceValue);
                }
            }
        }

        /// <summary>
        /// Gets a human-readable type name for error messages.
        /// </summary>
        private static string GetTypeName(JsonNode? node)
        {
            return node switch
            {
                null => "null",
                JsonObject => "object",
                JsonArray => "array",
                JsonValue => "primitive",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Creates a deep clone of a JsonNode since JsonNodes cannot be shared between parents.
        /// </summary>
        private static JsonNode? CloneValue(JsonNode? value)
        {
            if (value is null)
                return null;

            // Use JSON serialization for deep clone
            return JsonNode.Parse(value.ToJsonString());
        }
    }
}

