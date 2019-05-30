using System;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisLib
{
    /// <summary>
    /// Tools and extensions for <see cref="MySql"/>.
    /// </summary>
    internal static class MySqlTools
    {
        /// <summary>
        /// Gets if a specified column of a <see cref="MySqlDataReader"/> is <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns><c>True</c> if <see cref="DBNull.Value"/>; <c>False</c> otherwise.</returns>
        public static bool IsDBNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName));
        }

        /// <summary>
        /// Gets the value of a <see cref="MySqlDataReader"/> specified column; its type can be nullable.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>The value.</returns>
        public static T? GetNull<T>(this MySqlDataReader reader, string columnName) where T : struct
        {
            return reader.IsDBNull(columnName) ? (T?)null : (T)Convert.ChangeType(reader[columnName], typeof(T));
        }

        /// <summary>
        /// Gets the value of a <see cref="MySqlDataReader"/> specified column; its type can't be nullable.
        /// </summary>
        /// <typeparam name="T">Data type.</typeparam>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="columnName">Column name.</param>
        /// <returns>The value.</returns>
        public static T Get<T>(this MySqlDataReader reader, string columnName) where T : struct
        {
            return reader.IsDBNull(columnName) ? default(T) : (T)Convert.ChangeType(reader[columnName], typeof(T));
        }
    }
}
