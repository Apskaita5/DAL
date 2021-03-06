using System;
using System.Collections.Generic;

namespace A5Soft.DAL.Core.MicroOrm
{
    public static class Extensions
    {

        /// <summary>
        /// Gets a formated name of database object (field, table, index) using formatting
        /// policy defined by an SqlAgent.
        /// </summary>
        /// <param name="unformatedName">unformatted name of the database object</param>
        /// <param name="sqlAgent">an SqlAgent that defines naming convention</param>
        public static string ToConventional(this string unformatedName, ISqlAgent sqlAgent)
        {
            if (sqlAgent.IsNull()) throw new ArgumentNullException(nameof(sqlAgent));
            if (unformatedName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(unformatedName));
            if (sqlAgent.AllSchemaNamesLowerCased) return unformatedName.Trim().ToLower();
            return unformatedName.Trim();
        }

        /// <summary>
        /// Compares database object (field, table, index) names. As the changing object names is
        /// not implemented, it's always case insensitive comparison (Trim + OrdinalIgnoreCase).
        /// </summary>
        /// <param name="source">value to compare</param>
        /// <param name="valueToCompare">value to compare against</param>
        public static bool EqualsByConvention(this string source, string valueToCompare)
        {
            return (source?.Trim() ?? string.Empty).Equals(valueToCompare?.Trim() ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a value indicating whether a specified substring occures within the string, 
        /// it's always case insensitive comparison (Trim + OrdinalIgnoreCase).
        /// </summary>
        /// <param name="source">value to compare</param>
        /// <param name="substring">value to compare against</param>
        public static bool ContainsByConvention(this string source, string substring)
        {
            return (source ?? string.Empty).IndexOf(substring?.Trim() ?? string.Empty,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }


        /// <summary>
        /// Gets a DateTime containing DateTime.Now timestamp with a second precision.
        /// </summary>
        /// <remarks>Required by most SQL engines.</remarks>
        internal static DateTime GetCurrentTimeStamp()
        {
            var result = DateTime.UtcNow;
            result = new DateTime((long)(Math.Floor((double)(result.Ticks / TimeSpan.TicksPerSecond))
                * TimeSpan.TicksPerSecond), DateTimeKind.Utc);
            return result;
        }

        internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : default;
        }

        internal static bool IsInUpdateScope(this int? fieldScope, int? updateScope, bool scopeIsFlag)
        {
            return !updateScope.HasValue || !fieldScope.HasValue 
                || (scopeIsFlag && ((updateScope.Value & fieldScope.Value) != 0))
                || (!scopeIsFlag && updateScope.Value == fieldScope.Value);
        }

    }
}
