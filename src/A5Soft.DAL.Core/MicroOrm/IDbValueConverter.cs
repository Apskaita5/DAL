namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// Implement to provide conversions to/from database value.
    /// </summary>
    /// <typeparam name="TProperty">type of the property value</typeparam>
    /// <typeparam name="TDatabase">type of the property in the database</typeparam>
    public interface IDbValueConverter<TProperty, TDatabase>
    {
        /// <summary>
        /// Converts database value (type) to the property value (type).
        /// </summary>
        /// <param name="value">value in the database</param>
        /// <returns>property value for the database value</returns>
        TProperty Convert(TDatabase value);

        /// <summary>
        /// Converts property value (type) to the database value (type).
        /// </summary>
        /// <param name="value">value of the property</param>
        /// <returns>database value for the property value</returns>
        TDatabase Convert(TProperty value);
    }
}
