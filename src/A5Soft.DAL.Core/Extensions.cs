using System;
using System.Diagnostics;
using System.Linq;

namespace A5Soft.DAL.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Returns true if the string value is null or empty or consists from whitespaces only.
        /// </summary>
        /// <param name="value">a string value to evaluate</param>
        [DebuggerHidden]
        [DebuggerStepThrough]
        internal static bool IsNullOrWhiteSpace(this string value)
        {
            return null == value || string.IsNullOrEmpty(value.Trim());
        }

        /// <summary>
        /// Returns a value indicating that the object (value) is null. Required due to potential operator overloads
        /// that cause unpredictable behaviour of standard null == value test.
        /// </summary>
        /// <typeparam name="T">a type of the object to test</typeparam>
        /// <param name="value">an object to test against null</param>
        [DebuggerHidden]
        [DebuggerStepThrough]
        internal static bool IsNull<T>(this T value) where T : class
        {
            return ReferenceEquals(value, null) || DBNull.Value == value;
        }

        /// <summary>
        /// Gets a description of SQL statement/query parameters.
        /// </summary>
        /// <param name="parameters">the SQL statement/query parameters to get a description for</param>
        [DebuggerHidden]
        [DebuggerStepThrough]
        public static string GetDescription(this SqlParam[] parameters)
        {
            if (null == parameters || parameters.Length < 1) return "null";
            return string.Join("; ", parameters.Select(p => p.ToString()).ToArray());
        }
    }
}
