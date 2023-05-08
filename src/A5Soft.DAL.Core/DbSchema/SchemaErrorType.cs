namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Represents a type of a DbError, i.e. schema inconsistence.
    /// </summary>
    public enum SchemaErrorType
    {
        FieldMissing = 1,
        FieldDefinitionObsolete = 2,
        FieldObsolete = 5,
        TableMissing = 0,
        TableObsolete = 6,
        IndexMissing = 3,
        IndexObsolete = 4,
    }
}
