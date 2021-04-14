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
        Update = 8,

        /// <summary>
        /// initializable, readable, insertable and updateable
        /// </summary>
        Full = 15,

        /// <summary>
        /// initializable and readable
        /// </summary>
        InitAndRead = 3,

        /// <summary>
        /// readable, insertable and updateable
        /// </summary>
        CRUD = 14

    }
}
