using System;
using System.Data;

namespace NiceTennisDenis
{
    /// <summary>
    /// Extension methods for <see cref="IDataReader"/>.
    /// </summary>
    public static class IDataReaderExtensions
    {
        /// <summary>
        /// Extracts a nullable value from a <see cref="IDataReader"/> at a specified column.
        /// </summary>
        /// <typeparam name="T">The nullable output type.</typeparam>
        /// <param name="reader"><see cref="IDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Value.</returns>
        public static T? GetNullValue<T>(this IDataReader reader, string columnName) where T : struct
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName)) ? (T?)null
                : (T)Convert.ChangeType(reader[columnName], typeof(T));
        }

        /// <summary>
        /// Extracts a non-nullable value from a <see cref="IDataReader"/> at a specified column.
        /// </summary>
        /// <typeparam name="T">The output type.</typeparam>
        /// <param name="reader"><see cref="IDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Value.</returns>
        public static T GetValue<T>(this IDataReader reader, string columnName) where T : struct
        {
            object nonTypedValue = reader[columnName];
            if (nonTypedValue == null || nonTypedValue == DBNull.Value)
            {
                return default(T);
            }
            return (T)Convert.ChangeType(nonTypedValue, typeof(T));
        }

        /// <summary>
        /// Gets a value of type <see cref="double"/> from <paramref name="reader"/> at the specified <paramref name="columnName"/>.
        /// </summary>
        /// <param name="reader"><see cref="IDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Value of type <see cref="double"/>.</returns>
        public static double GetDouble(this IDataReader reader, string columnName)
        {
            return reader.GetValue<double>(columnName);
        }

        /// <summary>
        /// Gets a value of type <see cref="int"/> from <paramref name="reader"/> at the specified <paramref name="columnName"/>.
        /// </summary>
        /// <param name="reader"><see cref="IDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Value of type <see cref="int"/>.</returns>
        public static int GetInt32(this IDataReader reader, string columnName)
        {
            return reader.GetValue<int>(columnName);
        }

        /// <summary>
        /// Gets a value of type <see cref="string"/> from <paramref name="reader"/> at the specified <paramref name="columnName"/>.
        /// </summary>
        /// <param name="reader"><see cref="IDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>Value of type <see cref="string"/>.</returns>
        public static string GetString(this IDataReader reader, string columnName)
        {
            return reader.GetString(reader.GetOrdinal(columnName));
        }
    }
}
