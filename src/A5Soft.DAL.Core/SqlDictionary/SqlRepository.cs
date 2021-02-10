using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace A5Soft.DAL.Core.SqlDictionary
{
    /// <summary>
    /// Represents an SQL repository and contains SQL queries for a particular SQL implementation 
    /// identified by unique tokens. Used to provide SQL queries for a particular SQL implementation
    /// in a transparant way, i.e. business class only needs to know the token, not a specific (raw)
    /// SQL query.
    /// </summary>
    [Serializable]
    public sealed class SqlRepository
    {

        #region Private Fields

        private string _application = string.Empty;
        private string _description = string.Empty;
        private string _extension = string.Empty;
        private string _extensionGuid = string.Empty;
        private string _sqlImplementation = string.Empty;
        private List<SqlRepositoryItem> _items = new List<SqlRepositoryItem>();

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets or sets a name of the application that the repository is meant for.
        /// </summary>
        public string Application { get => _application ?? string.Empty; set => _application = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets a description of the repository (if any).
        /// </summary>
        public string Description { get => _description ?? string.Empty; set => _description = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets a name of the application extension if the repository belongs to one. 
        /// Empty string otherwise.
        /// </summary>
        public string Extension { get => _extension ?? string.Empty; set => _extension = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets an extension Guid if the repository belongs to the application extension. Empty string otherwise.
        /// </summary>
        public string ExtensionGuid { get => _extensionGuid ?? string.Empty; set => _extensionGuid = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets a SQL implementation code, e.g. MySQL, SQLite etc.
        /// </summary>
        public string SqlImplementation { get => _sqlImplementation ?? string.Empty; set => _sqlImplementation = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets a list of the repository entries.
        /// </summary>
        public List<SqlRepositoryItem> Items { get => _items; set => _items = value ?? new List<SqlRepositoryItem>(); } 
        
        #endregion


        /// <summary>
        /// Creates a new empty SQL repository.
        /// </summary>
        public SqlRepository() { }


        /// <summary>
        /// Loads a collection of SqlRepositoryItem from the repository specified.
        /// </summary>
        /// <param name="repository">a repository to load the data from</param>
        /// <param name="clearCurrentItems">whether to clear current collection of SQL statements 
        /// before loading new ones</param>
        public void Load(SqlRepository repository, bool clearCurrentItems = false)
        {
            if (repository.IsNull()) throw new ArgumentNullException(nameof(repository));

            if (clearCurrentItems)
            {
                _items.Clear();
                _application = repository._application ?? string.Empty;
                _description = repository._description ?? string.Empty;
                _extension = repository._extension ?? string.Empty;
                _extensionGuid = repository._extensionGuid ?? string.Empty;
                _sqlImplementation = repository._sqlImplementation ?? string.Empty;
            }
            if (null != repository._items && repository._items.Count > 0) 
                _items.AddRange(repository._items);
        }
        

        /// <summary>
        /// Gets a token - query dictionary for the repository.
        /// </summary>
        internal Dictionary<string, string> GetDictionary()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in _items)
            {
                if (!item.Token.IsNullOrWhiteSpace())
                {
                    if (result.ContainsKey(item.Token.Trim())) throw new InvalidDataException(
                        string.Format(Properties.Resources.CannotConvertToDictionaryException, item.Token));
                    result.Add(item.Token.Trim(), item.Query);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets a token - query dictionary for the repository and merges it into the base dictionary.
        /// </summary>
        /// <param name="baseDictionary">a dictionary to merge the data into</param>
        internal void MergeIntoDictionary(Dictionary<string, string> baseDictionary)
        {
            foreach (var item in GetDictionary())
            {
                if (baseDictionary.ContainsKey(item.Key)) throw new InvalidDataException(
                    string.Format(Properties.Resources.CannotMergeToDictionaryException, item.Key));
                baseDictionary.Add(item.Key, item.Value);
            }
        }


        /// <summary>
        /// Gets a list of namespaces that use the repository.
        /// </summary>
        public List<string> GetNamespaces()
        {
            var result = new List<string>();

            foreach (var item in _items
                .Where(entry => !string.IsNullOrWhiteSpace(entry.UsedByTypes))
                .SelectMany(entry => entry.UsedByTypes.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)))
            {
                if (item.Contains("."))
                {
                    var nameSpace = item.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    if (!result.Contains(nameSpace, StringComparer.OrdinalIgnoreCase))
                        result.Add(nameSpace);
                }
            }

            if (!result.Contains(string.Empty)) result.Add(string.Empty);

            result.Sort();

            return result;
        }

        /// <summary>
        /// Gets a list of (business) classes that use the repository.
        /// </summary>
        public List<string> GetTypes()
        {
            return _items
                .Where(entry => !string.IsNullOrWhiteSpace(entry.UsedByTypes))
                .SelectMany(entry => entry.UsedByTypes.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(val => val)
                .ToList();
        }

        /// <summary>
        /// Gets a list of (business) classes that use the repository for the namespace specified.
        /// </summary>
        /// <param name="nameSpace">a namespace to filter the result by</param>
        public List<string> GetTypes(string nameSpace)
        {
            if (nameSpace.IsNullOrWhiteSpace())
                return GetTypes();

            nameSpace = nameSpace.Trim() + ".";

            return GetTypes()
                .Where(type => type.Trim().StartsWith(nameSpace, StringComparison.OrdinalIgnoreCase))
                .Select(type => type.Trim().Substring(nameSpace.Length))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(val => val)
                .ToList();
        }

        /// <summary>
        /// Gets a value indicating that the SqlRepository contains (invalid) items with null tokens.
        /// </summary>
        public bool ContainsEmptyTokens()
        {
            return _items.Any(entry => entry.Token.IsNullOrWhiteSpace());
        }

        /// <summary>
        /// Gets a list of Tokens that are not assigned to any (business) classes.
        /// Returns an empty list if no such items found.
        /// </summary>
        public List<string> GetNotUsedTokens()
        {
            return (from entry in _items where entry.UsedByTypes.IsNullOrWhiteSpace() select entry.Token).ToList();
        }

        /// <summary>
        /// Gets a list of duplicate (invalid) Tokens. Returns an empty list if no duplicate Tokens found.
        /// </summary>
        public List<string> GetDuplicateTokens()
        {
            return _items.GroupBy(item => item.Token.Trim(), StringComparer.OrdinalIgnoreCase)
              .Where(g => g.Count() > 1)
              .Select(g => g.Key)
              .ToList();
        }

    }
}
