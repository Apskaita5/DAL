namespace A5Soft.DAL.Core
{
    /// <summary>
    /// Sql server health state.
    /// </summary>
    public enum HealthCheckState
    {
        /// <summary>
        /// Sql server is functioning nominally.
        /// </summary>
        Ok,

        /// <summary>
        /// Sql server is functioning but the performance is poor.
        /// </summary>
        PoorPerformance,

        /// <summary>
        /// Sql server is not functioning.
        /// </summary>
        Failed
    }
}
