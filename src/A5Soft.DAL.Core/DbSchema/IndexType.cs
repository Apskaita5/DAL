namespace A5Soft.DAL.Core.DbSchema
{
    /// <summary>
    /// Represents a canonical database index type.
    /// </summary>
    public enum IndexType
    {
        None = 0,
        Primary = 1,
        Unique = 2,
        Simple = 3,
        ForeignKey = 4,
        ForeignPrimary = 5
    }
}
