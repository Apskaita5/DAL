using System.IO;
using System.Xml;

namespace A5Soft.DAL.Core.Serialization
{
    /// <summary>
    /// Serializer implementation for native XML serialization.
    /// </summary>
    public sealed class XmlSerializer : ISerializer
    {

        ///<inheritdoc cref="ISerializer"/>
        public T Deserialize<T>(string serializedString)
        {
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (var textReader = new StringReader(serializedString))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

        ///<inheritdoc cref="ISerializer"/>
        public string Serialize<T>(T objectToSerialize)
        {
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = " ",
                Encoding = Constants.DefaultXmlFileEncoding
            };

            using (var ms = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(ms, settings))
                {
                    xmlSerializer.Serialize(writer, objectToSerialize);
                    return Constants.DefaultXmlFileEncoding.GetString(ms.ToArray());
                }
            }
        }

    }
}
