using System;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// Describes fow a business object field is persisted in the database
    /// </summary>
    [Flags]
    public enum FieldPersistenceType
    {
        /// <summary>
        /// initializable field
        /// </summary>
        Init = 1,

        /// <summary>
        /// readable field
        /// </summary>
        Read = 2,

        /// <summary>
        /// insertable field
        /// </summary>
        Insert = 4,

        /// <summary>
        /// updateable field
        /// </summary>
        Update = 8
    }
}
