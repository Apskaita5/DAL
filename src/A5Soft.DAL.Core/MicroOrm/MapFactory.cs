using A5Soft.DAL.Core.MicroOrm.Core;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// Factory methods for maps.
    /// </summary>
    public static class MapFactory
    {
        /// <summary>
        /// Gets a DB map for a readonly field.
        /// </summary>
        /// <typeparam name="TClass">a type of the business object that the field belongs to</typeparam>
        /// <typeparam name="TProperty">a type of the underlying field value</typeparam>
        /// <param name="propertyExpression">property getter expression</param>
        /// <param name="dbFieldName">a name of the database field (if the value is not an aggregate query result)</param>
        /// <returns>a DB map for a readonly field</returns>
        public static OrmFieldMapBase<TClass> ReadonlyFieldMapFor<TClass, TProperty>(
            Expression<Func<TClass, TProperty>> propertyExpression, string dbFieldName = "")
            where TClass : class
        {
            return new FieldMap<TClass, TProperty>(propertyExpression, dbFieldName);
        }

        /// <summary>
        /// Gets a DB map for a readonly field.
        /// </summary>
        /// <typeparam name="TClass">a type of the business object that the field belongs to</typeparam>
        /// <typeparam name="TProperty">a type of the underlying field value</typeparam>
        /// <param name="propertyExpression">property getter expression</param>
        /// <param name="dbFieldName">a name of the database field</param>
        /// <param name="persistenceType">type of the field persistence</param>
        /// <param name="updateScope">an update scope that updates the property value in database.
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.</param>
        /// <returns>a DB map for a readonly field</returns>
        public static OrmFieldMapBase<TClass> FieldMapFor<TClass, TProperty>(
            Expression<Func<TClass, TProperty>> propertyExpression, string dbFieldName,
            FieldPersistenceType persistenceType, int? updateScope = null)
            where TClass : class
        {
            if (string.IsNullOrWhiteSpace(dbFieldName)) throw new ArgumentNullException(nameof(dbFieldName));

            return new FieldMap<TClass, TProperty>(propertyExpression, dbFieldName, persistenceType, updateScope);
        }

        /// <summary>
        /// Gets a DB map for a readonly field.
        /// </summary>
        /// <typeparam name="TClass">a type of the business object that the field belongs to</typeparam>
        /// <typeparam name="TProperty">a type of the underlying field value</typeparam>
        /// <param name="propertyExpression">property getter expression</param>
        /// <param name="converter">converter for database <-> property value conversions</param>
        /// <param name="dbFieldName">a name of the database field (if the value is not an aggregate query result)</param>
        /// <returns>a DB map for a readonly field</returns>
        public static OrmFieldMapBase<TClass> ReadonlyFieldMapFor<TClass, TProperty, TDatabase>(
            Expression<Func<TClass, TProperty>> propertyExpression,
            IDbValueConverter<TProperty, TDatabase> converter, string dbFieldName = "")
            where TClass : class
        {
            return new FieldMapWithConverter<TClass, TProperty, TDatabase>(propertyExpression,
                converter, dbFieldName);
        }

        /// <summary>
        /// Gets a DB map for a readonly field.
        /// </summary>
        /// <typeparam name="TClass">a type of the business object that the field belongs to</typeparam>
        /// <typeparam name="TProperty">a type of the underlying field value</typeparam>
        /// <param name="propertyExpression">property getter expression</param>
        /// <param name="converter">converter for database <-> property value conversions</param>
        /// <param name="dbFieldName">a name of the database field</param>
        /// <param name="persistenceType">type of the field persistence</param>
        /// <param name="updateScope">an update scope that updates the property value in database.
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.</param>
        /// <returns>a DB map for a readonly field</returns>
        public static OrmFieldMapBase<TClass> FieldMapFor<TClass, TProperty, TDatabase>(
            Expression<Func<TClass, TProperty>> propertyExpression,
            IDbValueConverter<TProperty, TDatabase> converter, string dbFieldName,
            FieldPersistenceType persistenceType, int? updateScope = null)
            where TClass : class
        {
            if (string.IsNullOrWhiteSpace(dbFieldName)) throw new ArgumentNullException(nameof(dbFieldName));

            return new FieldMapWithConverter<TClass, TProperty, TDatabase>(propertyExpression, converter,
                dbFieldName, persistenceType, updateScope);
        }


        internal static PropertyInfo GetPropInfo<TClass, TProperty>(this Expression<Func<TClass, TProperty>> propertyExpression)
        {
            if (null == propertyExpression) throw new ArgumentNullException(nameof(propertyExpression));

            if (propertyExpression.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propInfo) return propInfo;

            throw new ArgumentException("Expression must be a property access expression",
                nameof(propertyExpression));
        }
    }
}
