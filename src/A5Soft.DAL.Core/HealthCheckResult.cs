using System;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// A description of a health check result for the sql server.
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Creates a description of a health check result for <see cref="HealthCheckState.Ok"/>
        /// or <see cref="HealthCheckState.PoorPerformance"/>.
        /// </summary>
        /// <param name="isPoorPerformance">whether the sql server is performing poorly, i.e.
        /// <see cref="HealthStatus"/> is <see cref="HealthCheckState.PoorPerformance"/>.</param>
        /// <param name="details">Details of the health check (if any)</param>
        public HealthCheckResult(bool isPoorPerformance = false, string details = null)
        {
            HealthStatus = isPoorPerformance ? HealthCheckState.PoorPerformance : HealthCheckState.Ok;
            Details = details;
            Exception = null;
        }

        /// <summary>
        /// Creates a description of a health check result for <see cref="HealthCheckState.Failed"/>.
        /// </summary>
        /// <param name="ex">an exception thrown while connecting to the server</param>
        public HealthCheckResult(Exception ex)
        {
            if (null == ex) throw new ArgumentNullException(nameof(ex));

            HealthStatus = HealthCheckState.Failed;
            Exception = ex;
            Details = null;
        }


        /// <summary>
        /// Whether the sql server is healthy (functioning nominally)
        /// </summary>
        public HealthCheckState HealthStatus { get; }

        /// <summary>
        /// The exception thrown while connecting to the server if it fails.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Details for the health check results.
        /// </summary>
        public string Details { get; }
    }
}
