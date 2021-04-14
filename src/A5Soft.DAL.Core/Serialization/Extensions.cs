using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using A5Soft.DAL.Core.DbSchema;
using A5Soft.DAL.Core.SqlDictionary;

namespace A5Soft.DAL.Core.Serialization
{
    public static class Extensions
    {

        /// <summary>
        /// Serializes a <see cref="LightDataTable"/> instance to string
        /// using the <see cref="ISerializer"/> implementation specified.
        /// </summary>
        /// <param name="table">a <see cref="LightDataTable"/> instance to serialize</param>
        /// <param name="serializer"><see cref="ISerializer"/> implementation to use for serialization</param>
        /// <returns>serialized string that contains the table's data</returns>
        /// <remarks><see cref="LightDataTable"/> is not serializable by all of the serializers
        /// as it has readonly properties. Therefore a POCO proxy is used.</remarks>
        public static string Serialize(this LightDataTable table, ISerializer serializer)
        {
            if (table.IsNull()) throw new ArgumentNullException(nameof(table));
            if (serializer.IsNull()) throw new ArgumentNullException(nameof(serializer));

            return serializer.Serialize(table.GetLightDataTableProxy());
        }

        /// <summary>
        /// Serializes a <see cref="LightDataTable"/> instance to string
        /// using the <see cref="XmlSerializer"/> (as a default implementation of <see cref="ISerializer"/>).
        /// </summary>
        /// <param name="table">a <see cref="LightDataTable"/> instance to serialize</param>
        /// <returns>serialized XML string that contains the table's data</returns>
        /// <remarks><see cref="LightDataTable"/> is not serializable by all of the serializers
        /// as it has readonly properties. Therefore a POCO proxy is used.</remarks>
        public static string Serialize(this LightDataTable table)
        {
            if (table.IsNull()) throw new ArgumentNullException(nameof(table));

            var serializer = new XmlSerializer();

            return serializer.Serialize(table.GetLightDataTableProxy());
        }

        /// <summary>
        /// Deserializes a string to a <see cref="LightDataTable"/> instance 
        /// using the <see cref="ISerializer"/> implementation specified.
        /// </summary>
        /// <param name="serializedData">serialized string that contains the table's data</param>
        /// <param name="serializer"><see cref="ISerializer"/> implementation to use for serialization</param>
        /// <returns>a deserialized <see cref="LightDataTable"/> instance</returns>
        /// <remarks><see cref="LightDataTable"/> is not serializable by all of the serializers
        /// as it has readonly properties. Therefore a POCO proxy is used.</remarks>
        public static LightDataTable Deserialize(string serializedData, ISerializer serializer)
        {
            if (serializedData.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(serializedData));
            if (serializer.IsNull()) throw new ArgumentNullException(nameof(serializer));

            var proxy = serializer.Deserialize<LightDataTableProxy>(serializedData);

            return new LightDataTable(proxy);
        }

        /// <summary>
        /// Deserializes a string to a <see cref="LightDataTable"/> instance 
        /// using the <see cref="XmlSerializer"/> (as a default implementation of <see cref="ISerializer"/>).
        /// </summary>
        /// <param name="serializedData">serialized string that contains the table's data</param>
        /// <returns>a deserialized <see cref="LightDataTable"/> instance</returns>
        /// <remarks><see cref="LightDataTable"/> is not serializable by all of the serializers
        /// as it has readonly properties. Therefore a POCO proxy is used.</remarks>
        public static LightDataTable Deserialize(string serializedData)
        {
            if (serializedData.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(serializedData));

            var serializer = new XmlSerializer();

            var proxy = serializer.Deserialize<LightDataTableProxy>(serializedData);

            return new LightDataTable(proxy);
        }

        /// <summary>
        /// Saves <see cref="SqlRepository"/> instance (serialized) data to the XML file specified.
        /// </summary>
        /// <param name="repository">a <see cref="SqlRepository"/> instance to save</param>
        /// <param name="filePath">a path to the file</param>
        public static void SaveToXmlFile(this SqlRepository repository, string filePath)
        {
            if (repository.IsNull()) throw new ArgumentNullException(nameof(repository));
            if (filePath.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filePath));

            var serializer = new XmlSerializer();

            File.WriteAllText(filePath, serializer.Serialize(repository), Constants.DefaultXmlFileEncoding);
        }

        /// <summary>
        /// Creates an <see cref="SqlRepository"/> instance using (serialized) data in the XML file specified.
        /// </summary>
        /// <param name="filePath">a path to the file</param>
        public static SqlRepository ReadSqlRepositoryFromXmlFile(string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException(
                string.Format(Properties.Resources.FileNotFound, filePath), filePath);

            var serializer = new XmlSerializer();

            try
            {
                return serializer.Deserialize<SqlRepository>(
                    File.ReadAllText(filePath, new UTF8Encoding(false)));
            }
            catch (Exception)
            {
                try
                {
                    return serializer.Deserialize<SqlRepository>(
                        File.ReadAllText(filePath, new UTF8Encoding(true)));
                }
                catch (Exception)
                {
                    try
                    {
                        return serializer.Deserialize<SqlRepository>(
                            File.ReadAllText(filePath, Encoding.Unicode));
                    }
                    catch (Exception)
                    {
                        try
                        {
                            return serializer.Deserialize<SqlRepository>(
                                File.ReadAllText(filePath, Encoding.ASCII));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException(string.Format(Properties.Resources.InvalidSqlRepositoryFileFormatException,
                                filePath, ex.Message));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves <see cref="Schema"/> instance (serialized) data to the XML file specified.
        /// </summary>
        /// <param name="schema">a <see cref="Schema"/> instance to save</param>
        /// <param name="filePath">a path to the file</param>
        public static void SaveToXmlFile(this DbSchema.Schema schema, string filePath)
        {
            if (schema.IsNull()) throw new ArgumentNullException(nameof(schema));
            if (filePath.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filePath));

            var serializer = new XmlSerializer();

            File.WriteAllText(filePath, serializer.Serialize(schema), Constants.DefaultXmlFileEncoding);
        }

        /// <summary>
        /// Creates a <see cref="Schema"/> instance using (serialized) data in the XML file specified.
        /// </summary>
        /// <param name="filePath">a path to the file</param>
        public static DbSchema.Schema ReadDbSchemaFromXmlFile(string filePath)
        {
            if (filePath.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException(
                string.Format(Properties.Resources.FileNotFound, filePath), filePath);

            var serializer = new XmlSerializer();

            try
            {
                return serializer.Deserialize<DbSchema.Schema>(
                    File.ReadAllText(filePath, new UTF8Encoding(false)));
            }
            catch (Exception)
            {
                try
                {
                    return serializer.Deserialize<DbSchema.Schema>(
                        File.ReadAllText(filePath, new UTF8Encoding(true)));
                }
                catch (Exception)
                {
                    try
                    {
                        return serializer.Deserialize<DbSchema.Schema>(
                            File.ReadAllText(filePath, Encoding.Unicode));
                    }
                    catch (Exception)
                    {
                        try
                        {
                            return serializer.Deserialize<DbSchema.Schema>(
                                File.ReadAllText(filePath, Encoding.ASCII));
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException(string.Format(Properties.Resources.InvalidSqlRepositoryFileFormatException,
                                filePath, ex.Message));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads a collection of <see cref="Schema"/> from the XML files,
        /// that are located in the folderPath specified (including subfolders),
        /// and creates an aggregate schema for the extensions specified
        /// (or all of the schemas if the extensions are not specified (null)).
        /// </summary>
        /// <param name="files">path to the files</param>
        /// <param name="forExtensions">identifiers (guid's) of the extensions to use
        /// (if null, will use all the available schemas)</param>
        public static DbSchema.Schema ReadAggregateDbSchema(IEnumerable<string> files, 
            Guid[] forExtensions = null)
        {
            if (null == files) throw new ArgumentNullException(nameof(files));

            var result = new List<DbSchema.Schema>();

            foreach (var filePath in files)
            {
                var item = ReadDbSchemaFromXmlFile(filePath);
                if (null == forExtensions || item.ExtensionGuid.IsNullOrWhiteSpace() 
                    || forExtensions.Contains(new Guid(item.ExtensionGuid)))
                    result.Add(ReadDbSchemaFromXmlFile(filePath));
            }

            if (result.Count < 1)
                throw new InvalidOperationException("No schemas found.");

            return new DbSchema.Schema(result, forExtensions);
        }

    }
}
