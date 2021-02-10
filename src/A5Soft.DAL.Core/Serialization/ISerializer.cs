namespace A5Soft.DAL.Core.Serialization
{
    /// <summary>
    /// Common interface for serializers that could be used for DAL objects.
    /// </summary>
    public interface ISerializer
    {

        /// <summary>
        /// Serializes any object to (xml, json etc.) string.
        /// </summary>
        /// <typeparam name="T">a type of the object to serialize</typeparam>
        /// <param name="objectToSerialize">an object instance to serialize</param>
        /// <returns>string that contains serialized object instance data</returns>
        string Serialize<T>(T objectToSerialize);

        /// <summary>
        /// Deserializes any object from (serialized xml, json etc.) string.
        /// </summary>
        /// <typeparam name="T">a type of the object to deserialize</typeparam>
        /// <param name="serializedString">a serialized (xml, json etc.) string
        /// that contains the data of the deserializable object</param>
        /// <returns>an object of type T</returns>
        T Deserialize<T>(string serializedString);

    }
}
