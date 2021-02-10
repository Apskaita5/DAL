namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Represents a type of a DbError, i.e. schema inconsistence.
    /// </summary>
    public enum SchemaErrorType
    {
        FieldMissing = 0,
        FieldDefinitionObsolete = 1,
        FieldObsolete = 2,
        TableMissing = 3,
        TableObsolete = 4,
        IndexMissing = 5,
        IndexObsolete = 6,
    }
}
