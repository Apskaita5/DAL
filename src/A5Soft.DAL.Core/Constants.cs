using System.Text;
using A5Soft.DAL.Core.DbSchema;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// Represents a common space for all the constants used in the assembly.
    /// </summary>
    public static class Constants
    {

        /// <summary>
        /// Gets a default encoding used when reading or writing xml files.
        /// </summary>
        public static readonly Encoding DefaultXmlFileEncoding = new UTF8Encoding(false);

        /// <summary>
        /// an extension for <see cref="SqlDictionary.SqlRepository">SqlRepository</see> files
        /// </summary>
        public const string SqlRepositoryFileExtension = ".xml";

        /// <summary>
        /// an extension for <see cref="Schema">DbSchema</see> files
        /// </summary>
        public const string DbSchemaFileExtension = ".xml";

    }
}
