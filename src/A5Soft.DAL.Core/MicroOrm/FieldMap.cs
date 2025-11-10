using A5Soft.DAL.Core.MicroOrm.Core;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// A basic class for description of how a business object property (field) is persisted in a database
    /// using auto converters for primitive types.
    /// </summary>
    /// <typeparam name="TClass">a type of the business object that the field belongs to</typeparam>
    /// <typeparam name="TProperty">a type of the underlying field value</typeparam>
    public class FieldMap<TClass, TProperty> : OrmFieldMapBase<TClass> where TClass : class
    {
        /// <summary>
        /// Creates a new DB Map for a property.
        /// </summary>
        /// <param name="propertyExpression">property getter expression</param>
        /// <param name="dbFieldName">a name of the database field (if the value is not an aggregate query result)</param>
        /// <param name="persistenceType">type of the field persistence</param>
        /// <param name="updateScope">an update scope that updates the property value in database.
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.</param>
        public FieldMap(Expression<Func<TClass, TProperty>> propertyExpression, string dbFieldName = "",
            FieldPersistenceType persistenceType = FieldPersistenceType.Read, int? updateScope = null)
            : base(dbFieldName, GetPropInfo(propertyExpression).Name, persistenceType, updateScope)
        {
            if (null == propertyExpression) throw new ArgumentNullException(nameof(propertyExpression));

            var propInfo = GetPropInfo(propertyExpression);

            // Create getter expression
            Getter = propertyExpression.Compile();

            // Create setter expressions
            var setMethod = propInfo.GetSetMethod(nonPublic: true);
            if (null == setMethod) throw new ArgumentException(
                $"Property {propInfo.Name} does not have a setter.",
                nameof(propertyExpression));

            DataReaderSetter = CreateDataReaderSetter(propInfo, setMethod);
            DataRowSetter = CreateDataRowSetter(propInfo, setMethod);
        }


        internal override SqlParam GetParam(TClass instance)
        {
            return SqlParam.Create(DbFieldName, Getter(instance));
        }

        internal override void SetValue(TClass instance, LightDataRow row)
        {
            DataRowSetter(instance, row);
        }

        internal override void SetValue(TClass instance, ILightDataReader reader)
        {
            DataReaderSetter(instance, reader);
        }


        private Func<TClass, TProperty> Getter { get; }

        private Action<TClass, ILightDataReader> DataReaderSetter { get; }

        private Action<TClass, LightDataRow> DataRowSetter { get; }


        private static PropertyInfo GetPropInfo(Expression<Func<TClass, TProperty>> propertyExpression)
        {
            if (null == propertyExpression) throw new ArgumentNullException(nameof(propertyExpression));

            if (propertyExpression.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propInfo) return propInfo;

            throw new ArgumentException("Expression must be a property access expression",
                nameof(propertyExpression));
        }

        private static Action<TClass, ILightDataReader> CreateDataReaderSetter(
            PropertyInfo propInfo, MethodInfo setMethod)
        {
            var converterMethod = FindReaderConverterMethod(typeof(TProperty));

            if (converterMethod == null)
            {
                throw new InvalidOperationException(
                    $"No DataReader converter method found for type {typeof(TProperty).Name}");
            }

            var instanceParam = Expression.Parameter(typeof(TClass), "instance");
            var readerParam = Expression.Parameter(typeof(ILightDataReader), "reader");
            var propertyNameConstant = Expression.Constant(propInfo.Name);

            // Call instance method: reader.ConvertXXX(propertyName)
            var converterCall = Expression.Call(
                readerParam,
                converterMethod,
                propertyNameConstant
            );

            var setterCall = Expression.Call(
                instanceParam,
                setMethod,
                converterCall
            );

            var lambda = Expression.Lambda<Action<TClass, ILightDataReader>>(
                setterCall,
                instanceParam,
                readerParam
            );

            return lambda.Compile();
        }

        private static Action<TClass, LightDataRow> CreateDataRowSetter(
            PropertyInfo propInfo, MethodInfo setMethod)
        {
            var converterMethod = FindDataRowConverterMethod(typeof(TProperty));

            if (converterMethod == null)
            {
                throw new InvalidOperationException(
                    $"No DataRow converter method found for type {typeof(TProperty).Name}");
            }

            var instanceParam = Expression.Parameter(typeof(TClass), "instance");
            var rowParam = Expression.Parameter(typeof(LightDataRow), "row");
            var propertyNameConstant = Expression.Constant(propInfo.Name);

            // Call instance method: reader.ConvertXXX(propertyName)
            var converterCall = Expression.Call(
                rowParam,
                converterMethod,
                propertyNameConstant
            );

            var setterCall = Expression.Call(
                instanceParam,
                setMethod,
                converterCall
            );

            var lambda = Expression.Lambda<Action<TClass, LightDataRow>>(
                setterCall,
                instanceParam,
                rowParam
            );

            return lambda.Compile();
        }

        private static MethodInfo FindReaderConverterMethod(Type targetType)
        {
            var readerType = typeof(ILightDataReader);

            // Check if it's an enum or nullable enum
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var isNullableEnum = underlyingType != null && underlyingType.IsEnum;
            var isEnum = targetType.IsEnum;

            if (isNullableEnum)
            {
                // Use GetEnumNullable<TEnum>(propertyName) where TEnum is the underlying enum type
                var getEnumNullableMethod = readerType.GetMethod("GetEnumNullable");
                if (getEnumNullableMethod == null)
                {
                    throw new InvalidOperationException(
                        "ILightDataReader does not have a GetEnumNullable<TEnum> method");
                }
                return getEnumNullableMethod.MakeGenericMethod(underlyingType);
            }
            else if (isEnum)
            {
                // Use GetEnum<TEnum>(propertyName)
                var getEnumMethod = readerType.GetMethod("GetEnum");
                if (getEnumMethod == null)
                {
                    throw new InvalidOperationException(
                        "ILightDataReader does not have a GetEnum<TEnum> method");
                }
                return getEnumMethod.MakeGenericMethod(targetType);
            }
            else
            {
                // Find regular converter method for non-enum types
                var methods = readerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();

                    // Find instance method with signature: TProperty Method(string columnName)
                    if (parameters.Length == 1 &&
                        parameters[0].ParameterType == typeof(string) &&
                        method.ReturnType == targetType &&
                        !method.IsGenericMethod)  // Exclude generic methods like GetEnum<T>
                    {
                        return method;
                    }
                }

                return null;
            }
        }

        private static MethodInfo FindDataRowConverterMethod(Type targetType)
        {
            var readerType = typeof(LightDataRow);

            // Check if it's an enum or nullable enum
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var isNullableEnum = underlyingType != null && underlyingType.IsEnum;
            var isEnum = targetType.IsEnum;

            if (isNullableEnum)
            {
                // Use GetEnumNullable<TEnum>(propertyName) where TEnum is the underlying enum type
                var getEnumNullableMethod = readerType.GetMethod("GetEnumNullable");
                if (getEnumNullableMethod == null)
                {
                    throw new InvalidOperationException(
                        "LightDataRow does not have a GetEnumNullable<TEnum> method");
                }
                return getEnumNullableMethod.MakeGenericMethod(underlyingType);
            }
            else if (isEnum)
            {
                // Use GetEnum<TEnum>(propertyName)
                var getEnumMethod = readerType.GetMethod("GetEnum");
                if (getEnumMethod == null)
                {
                    throw new InvalidOperationException(
                        "LightDataRow does not have a GetEnum<TEnum> method");
                }
                return getEnumMethod.MakeGenericMethod(targetType);
            }
            else
            {
                // Find regular converter method for non-enum types
                var methods = readerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();

                    // Find instance method with signature: TProperty Method(string columnName)
                    if (parameters.Length == 1 &&
                        parameters[0].ParameterType == typeof(string) &&
                        method.ReturnType == targetType &&
                        !method.IsGenericMethod)  // Exclude generic methods like GetEnum<T>
                    {
                        return method;
                    }
                }

                return null;
            }
        }
    }
}
