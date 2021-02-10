using System;

namespace A5Soft.DAL.Core.TypeConverters
{
    /// <summary>
    /// Internal exception that externally "looks like" a FormatException
    /// used to simplify throwing by multiple methods.
    /// </summary>
    internal class ConversionException : FormatException
    {
        public ConversionException(object actualValue, Type expectedType)
            : base(string.Format(Properties.Resources.TypeConverters_InvalidFormatException,
                actualValue?.GetType().FullName ?? "[null]", expectedType?.FullName ?? "[null]",
                actualValue?.ToString() ?? "[null]"))
        { }

        public ConversionException(object actualValue, Type expectedType, int index)
            : base(string.Format(Properties.Resources.TypeConverters_InvalidFormatExceptionWithIndex,
                actualValue?.GetType().FullName ?? "[null]", expectedType?.FullName ?? "[null]",
                actualValue?.ToString() ?? "[null]", index))
        { }

        public ConversionException(object actualValue, Type expectedType, string fieldName)
            : base(string.Format(Properties.Resources.TypeConverters_InvalidFormatExceptionWithColumnName,
                actualValue?.GetType().FullName ?? "[null]", expectedType?.FullName ?? "[null]",
                actualValue?.ToString() ?? "[null]", fieldName))
        { }

    }
}
