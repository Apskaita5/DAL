using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace A5Soft.DAL.Core.SqlDictionary
{
    /// <summary>
    /// An implementation of <see cref="ISqlDictionary"/> that uses XML files in a dedicated
    /// app folder for data storage.
    /// </summary>
    /// <remarks>Is meant to use as a singleton object.
    /// Possible initialization paths:
    /// - init by constructor;
    /// - init by dedicated method;
    /// - init on first request (not recommended; throws errors too late).</remarks>
    public sealed class XmlFolderSqlDictionary : ISqlDictionary
    {

        private Dictionary<string, Dictionary<string, string>> _tokenDictionary = null;
        private readonly object _dictLock = new object();


        /// <summary>
        /// Gets path to the folders where the relevant SqlRepository files are located
        /// </summary>
        public IEnumerable<string> Folders { get; }


        private XmlFolderSqlDictionary() { }

        /// <summary>
        /// Creates a new instance of SQL dictionary.
        /// </summary>
        /// <param name="folders">path to the folders where the relevant SqlRepository files are located</param>
        public XmlFolderSqlDictionary(IEnumerable<string> folders) : this(folders, false) { }

        /// <summary>
        /// Creates a new instance of SQL dictionary.
        /// </summary>
        /// <param name="folders">path to the folders where the relevant SqlRepository files are located</param>
        /// <param name="init">whether to initialize dictionary, i.e. load data from files</param>
        public XmlFolderSqlDictionary(IEnumerable<string> folders, bool init)
        {
            if (null == folders || !folders.Any()) throw new ArgumentNullException(nameof(folders));
            Folders = folders;
            if (init) Initialize();
        }


        /// <summary>
        /// Gets an SQL query or statement by the token for the SQL agent specified.
        /// </summary>
        /// <param name="token">a token (key, name) of the requested query or statement</param>
        /// <param name="sqlAgent">an SQL agent for which the SQL query or statement is meant for</param>
        /// <exception cref="ArgumentNullException">Parameters token or sqlAgent are not specified.</exception>
        /// <exception cref="FileNotFoundException">No repository files found or they contain no data 
        /// for the SQL agent type specified.</exception>
        /// <exception cref="InvalidDataException">Failed to load SQL repository file due to bad format or duplicate query tokens.</exception>
        /// <exception cref="InvalidOperationException">SQL query token is unknown or SQL query dictionary is not available for the SQL implementation.</exception>
        public string GetSqlQuery(string token, ISqlAgent sqlAgent)
        {
            if (token.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(token));
            if (sqlAgent.IsNull()) throw new ArgumentNullException(nameof(sqlAgent));

            if (_tokenDictionary.IsNull())
            {
                lock (_dictLock)
                {
                    if (_tokenDictionary.IsNull()) Initialize();
                }
            }

            if (!_tokenDictionary.ContainsKey(sqlAgent.SqlImplementationId)) throw new InvalidOperationException(
                string.Format(Properties.Resources.SqlDictionaryNotAvailableException, sqlAgent.Name));

            var dictionaryForSqlAgent = _tokenDictionary[sqlAgent.SqlImplementationId];

            if (!dictionaryForSqlAgent.ContainsKey(token.Trim())) throw new InvalidOperationException(
                string.Format(Properties.Resources.SqlDictionary_UnknownSqlQueryToken, token));

            return dictionaryForSqlAgent[token.Trim()];
        }

        /// <summary>
        /// Initializes SQL dictionary, i.e. loads data from files in the repository folder.
        /// </summary>
        public void Initialize()
        {
            _tokenDictionary = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in GetSqlRepositoryFiles())
            {
                var repository = Serialization.Extensions.ReadSqlRepositoryFromXmlFile(filePath);
                try
                {
                    if (_tokenDictionary.ContainsKey(repository.SqlImplementation.Trim()))
                    {
                        repository.MergeIntoDictionary(_tokenDictionary[repository.SqlImplementation.Trim()]);
                    }
                    else _tokenDictionary.Add(repository.SqlImplementation.Trim(), repository.GetDictionary());
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException(string.Format(Properties.Resources.DuplicateTokensInRepositoryException,
                        filePath, ex.Message));
                }
            }

        }


        private IEnumerable<string> GetSqlRepositoryFiles()
        {
            foreach (var folder in Folders)
            {
                foreach (var file in Directory.GetFiles(folder, "*.*", 
                        SearchOption.AllDirectories)
                    .Where(f => (Path.GetExtension(f) ?? string.Empty).StartsWith(
                        Constants.SqlRepositoryFileExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return file;
                }
            }
        }

    }
}
