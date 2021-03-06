﻿namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Defines a collation to use for a database field.
    /// </summary>
    public enum FieldCollationType
    {
        /// <summary>
        /// Use default database collation.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use ASCII case insensitive collation, e.g. ENUM, code field
        /// </summary>
        ASCII_CaseInsensitive = 1,

        /// <summary>
        /// Use ASCII binary (case sensitive) collation, e.g. GUID
        /// </summary>
        ASCII_Binary = 2
    }
}
