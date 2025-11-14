using A5Soft.DAL.Core.MicroOrm.Core;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace A5Soft.DAL.Core.MicroOrm
{
    /// <summary>
    /// A basic class for description of how a business object property (field) is persisted in a database
    /// using auto converters for primitive types and provided property value converter.
    /// </summary>
    /// <typeparam name="TClass">a type of the business object that the field belongs to</typeparam>
    /// <typeparam name="TProperty">a type of the underlying field value</typeparam>
    public class FieldMapWithConverter<TClass, TProperty, TDatabase> : OrmFieldMapBase<TClass> where TClass : class
    {
        private IDbValueConverter<TProperty, TDatabase> _converter;


        /// <summary>
        /// Creates a new DB Map for a property.
        /// </summary>
        /// <param name="propertyExpression">property getter expression</param>
        /// <param name="converter">a converter to use for the property value <-> database value conversions</param>
        /// <param name="dbFieldName">a name of the database field (if the value is not an aggregate query result)</param>
        /// <param name="persistenceType">type of the field persistence</param>
        /// <param name="updateScope">an update scope that updates the property value in database.
        /// Update scopes are application defined enums that convert nicely to int, e.g. Financial, Depreciation etc.
        /// If no scope is assigned the field value is updated for every scope.</param>
        public FieldMapWithConverter(Expression<Func<TClass, TProperty>> propertyExpression,
            IDbValueConverter<TProperty, TDatabase> converter, string dbFieldName = "",
            FieldPersistenceType persistenceType = FieldPersistenceType.Read, int? updateScope = null)
            : base(dbFieldName, propertyExpression.GetPropInfo().Name, persistenceType, updateScope)
        {
            if (null == propertyExpression) throw new ArgumentNullException(nameof(propertyExpression));
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));

            var propInfo = propertyExpression.GetPropInfo();

            // Create getter expression
            Getter = propertyExpression.Compile();

            // Create setter expressions
            var setMethod = propInfo.GetSetMethod(nonPublic: true);
            if (null == setMethod) throw new ArgumentException(
                $"Property {propInfo.Name} does not have a setter.",
                nameof(propertyExpression));

            // Get the FromDatabase method from converter
            var converterType = typeof(IDbValueConverter<TProperty, TDatabase>);
            var convertMethods = converterType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "Convert" && m.ReturnType == typeof(TProperty));
            var fromDatabaseMethod = convertMethods.FirstOrDefault(m => m.ReturnType == typeof(TProperty));
            if (null == fromDatabaseMethod)
            {
                throw new InvalidOperationException(
                    $"Converter does not have a Convert method that takes database type and returns property type.");
            }

            DataReaderSetter = CreateDataReaderSetter(propInfo, setMethod, fromDatabaseMethod, converter);
            DataRowSetter = CreateDataRowSetter(propInfo, setMethod, fromDatabaseMethod, converter);
        }


        private Func<TClass, TProperty> Getter { get; }

        private Action<TClass, ILightDataReader> DataReaderSetter { get; }

        private Action<TClass, LightDataRow> DataRowSetter { get; }


        internal override SqlParam GetParam(TClass instance)
        {
            return SqlParam.Create(DbFieldName, _converter.Convert(Getter(instance)));
        }

        internal override void SetValue(TClass instance, LightDataRow row)
        {
            DataRowSetter(instance, row);
        }

        internal override void SetValue(TClass instance, ILightDataReader reader)
        {
            DataReaderSetter(instance, reader);
        }


        private static Action<TClass, ILightDataReader> CreateDataReaderSetter(PropertyInfo propInfo,
            MethodInfo setMethod, MethodInfo fromDatabaseMethod, IDbValueConverter<TProperty, TDatabase> converter)
        {
            var converterMethod = FindBaseConverterMethod(typeof(ILightDataReader));

            var instanceParam = Expression.Parameter(typeof(TClass), "instance");
            var readerParam = Expression.Parameter(typeof(ILightDataReader), "reader");
            var propertyNameConstant = Expression.Constant(propInfo.Name);

            // Read from database: reader.ConvertXXX(propertyName)
            var readerCall = Expression.Call(
                readerParam,
                converterMethod,
                propertyNameConstant
            );

            // Convert: converter.FromDatabase(databaseValue)
            var converterConstant = Expression.Constant(converter);
            var convertCall = Expression.Call(
                converterConstant,
                fromDatabaseMethod,
                readerCall
            );

            // Set property: instance.Property = convertedValue
            var setterCall = Expression.Call(
                instanceParam,
                setMethod,
                convertCall
            );

            var lambda = Expression.Lambda<Action<TClass, ILightDataReader>>(
                setterCall,
                instanceParam,
                readerParam
            );

            return lambda.Compile();
        }

        private static Action<TClass, LightDataRow> CreateDataRowSetter(PropertyInfo propInfo,
            MethodInfo setMethod, MethodInfo fromDatabaseMethod, IDbValueConverter<TProperty, TDatabase> converter)
        {
            var converterMethod = FindBaseConverterMethod(typeof(LightDataRow));

            var instanceParam = Expression.Parameter(typeof(TClass), "instance");
            var readerParam = Expression.Parameter(typeof(LightDataRow), "row");
            var propertyNameConstant = Expression.Constant(propInfo.Name);

            // Read from database: reader.ConvertXXX(propertyName)
            var readerCall = Expression.Call(
                readerParam,
                converterMethod,
                propertyNameConstant
            );

            // Convert: converter.FromDatabase(databaseValue)
            var converterConstant = Expression.Constant(converter);
            var convertCall = Expression.Call(
                converterConstant,
                fromDatabaseMethod,
                readerCall
            );

            // Set property: instance.Property = convertedValue
            var setterCall = Expression.Call(
                instanceParam,
                setMethod,
                convertCall
            );

            var lambda = Expression.Lambda<Action<TClass, LightDataRow>>(
                setterCall,
                instanceParam,
                readerParam
            );

            return lambda.Compile();
        }

        private static MethodInfo FindBaseConverterMethod(Type readerType)
        {
            var targetType = typeof(TDatabase);

            // Check if it's an enum or nullable enum
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            var isNullableEnum = underlyingType != null && underlyingType.IsEnum;
            var isEnum = targetType.IsEnum;

            if (isNullableEnum)
            {
                // Use GetEnumNullable<TEnum>(propertyName) where TEnum is the underlying enum type
                var getEnumNullableMethod = readerType.GetMethod("GetEnumNullable");
                if (null == getEnumNullableMethod || !getEnumNullableMethod.IsGenericMethod)
                {
                    throw new InvalidOperationException(
                        $"{readerType.Name} does not have a GetEnumNullable<TEnum> method");
                }
                return getEnumNullableMethod.MakeGenericMethod(underlyingType);
            }
            else if (isEnum)
            {
                // Use GetEnum<TEnum>(propertyName)
                var getEnumMethod = readerType.GetMethod("GetEnum");
                if (null == getEnumMethod || !getEnumMethod.IsGenericMethod)
                {
                    throw new InvalidOperationException(
                        $"{readerType.Name} does not have a GetEnum<TEnum> method");
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

                throw new InvalidOperationException(
                        $"{readerType.Name} does not have a converter method for type {targetType.FullName}");
            }
        }
    }
}
