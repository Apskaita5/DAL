using System;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// a subtype of <see cref="SqlException"/> to indicate authentication issue,
    /// i.e. when a user is not authorized to connect to the database
    /// </summary>
    public class SqlAuthenticationException : SqlException
    {
        /// <inheritdoc />
        public SqlAuthenticationException(string message, int code, string statement, Exception innerException) 
            : base(message, code, statement, innerException) { }

        /// <inheritdoc />
        public SqlAuthenticationException(string message, int code, string statement, Exception innerException, 
            Exception rollbackException) : base(message, code, statement, innerException, rollbackException) { }
    }
}
