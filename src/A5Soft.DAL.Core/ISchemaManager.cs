using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using A5Soft.DAL.Core.DbSchema;

namespace A5Soft.DAL.Core
{
    /// <summary>
    /// an abstraction for database schema manager for DI
    /// </summary>
    public interface ISchemaManager
    {

        /// <summary>
        /// Gets a <see cref="Schema">Schema</see> instance (a canonical database description) 
        /// for the current database.
        /// </summary>
        /// <param name="cancellationToken">a cancellation token (if any)</param>
        Task<Schema> GetDbSchemaAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Compares the current database definition to the gauge definition
        /// and returns a list of DbSchema errors, i.e. inconsistencies found 
        /// and SQL statements to repair them.
        /// </summary>
        /// <param name="gaugeSchema">the gauge database schema to test for the errors against</param>
        /// <param name="cancellationToken">a cancellation token (if any)</param>
        /// <exception cref="ArgumentNullException">gauge database schema is not specified</exception>
        Task<List<SchemaError>> GetDbSchemaErrorsAsync(Schema gaugeSchema,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Compares the actualSchema definition to the gaugeSchema definition and returns
        /// a list of DbSchema errors, i.e. inconsistencies found and SQL statements to repair them.
        /// </summary>
        /// <param name="gaugeSchema">the gauge schema definition to compare the actualSchema against</param>
        /// <param name="actualSchema">the schema to check for inconsistencies (and repair)</param>
        List<SchemaError> GetDbSchemaErrors(Schema gaugeSchema, Schema actualSchema);

        /// <summary>
        /// Gets an SQL script to create a database for the dbSchema specified.
        /// </summary>
        /// <param name="dbSchema">the database schema to get the create database script for</param>
        string GetCreateDatabaseSql(Schema dbSchema);
          
        /// <summary>
        /// Creates a new database using DbSchema.
        /// </summary>
        /// <param name="dbSchema">a DbSchema to use for the new database</param>
        /// <remarks>After creating a new database the <see cref="ISqlAgent.CurrentDatabase">CurrentDatabase</see>
        /// property should be set to the new database name.</remarks>
        Task CreateDatabaseAsync(Schema dbSchema);

        /// <summary>
        /// Initializes a database using DbSchema, i.e.:
        /// - creates a database if it does not exist;
        /// - creates tables if it is empty;
        /// - checks and fixed schema errors otherwise.
        /// </summary>
        /// <param name="dbSchema">a DbSchema to use for the database initialization</param>
        /// <remarks>After creating a new database the <see cref="ISqlAgent.CurrentDatabase">CurrentDatabase</see>
        /// property should be set to the new database name.</remarks>
        Task InitDatabaseAsync(Schema dbSchema);

        /// <summary>
        /// Drops (deletes) the current database.
        /// </summary>
        Task DropDatabaseAsync();

        /// <summary>
        /// Creates a clone of the database using another SqlAgent.
        /// </summary>
        /// <param name="cloneManager">an SQL schema manager to use when creating clone database</param>
        /// <param name="schemaToUse">a database schema to use (enforce), 
        /// if null the schema ir read from the database cloned</param>
        /// <param name="ct">a cancellation token (if any)</param>
        /// <param name="progress">a progress callback (if any)</param>
        Task CloneDatabaseAsync(SchemaManagerBase cloneManager, Schema schemaToUse,
            IProgress<CloneProgressArgs> progress = null, CancellationToken ct = default);

    }
}
