using System;

namespace A5Soft.DAL.Core.SqlDictionary
{
    /// <summary>
    /// Represents an entry in an SQL repository and contains SQL query 
    /// for a particular SQL implementation identified by a unique token.
    /// </summary>
    [Serializable]
    public sealed class SqlRepositoryItem
    {

        private string _token = string.Empty;
        private string _query = string.Empty;
        private string _usedByTypes = string.Empty;


        /// <summary>
        /// Gets or sets the token (unique code) of the SQL query in the SQL repository.
        /// </summary>
        public string Token
        {
            get { return _token ?? string.Empty; }
            set { _token = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets an SQL query for a particular SQL implementation (e.g. MySql, SQLite, etc.)
        /// </summary>
        public string Query
        {
            get { return _query ?? string.Empty; }
            set { _query = value?.Trim() ?? string.Empty; }
        }

        /// <summary>
        /// Gets or sets (business) classes that use the SQL query.
        /// Classes naming format is: Namespace.Class; Namespace.Class; etc.
        /// </summary>
        public string UsedByTypes
        {
            get { return _usedByTypes ?? string.Empty; ; }
            set { _usedByTypes = value?.Trim() ?? string.Empty; }
        }


        public SqlRepositoryItem() { }

    }
}
