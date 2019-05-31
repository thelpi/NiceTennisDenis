using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll
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
        internal static bool IsDBNull(this MySqlDataReader reader, string columnName)
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
        internal static T? GetNull<T>(this MySqlDataReader reader, string columnName) where T : struct
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
        internal static T Get<T>(this MySqlDataReader reader, string columnName) where T : struct
        {
            return reader.IsDBNull(columnName) ? default(T) : (T)Convert.ChangeType(reader[columnName], typeof(T));
        }

        /// <summary>
        /// Creates an insert SQL statement with parameters, from a list of columns.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="headers">List of columns names.</param>
        /// <returns>SQL insert statement.</returns>
        /// <remarks>Parameters have the same name as the column, with an "@" suffix.</remarks>
        internal static string GetSqlInsertStatement(string table, IEnumerable<string> headers)
        {
            return GetSqlInsertOrReplaceStatement(table, headers, false);
        }

        /// <summary>
        /// Creates an replace SQL statement with parameters, from a list of columns.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="headers">List of columns names.</param>
        /// <returns>SQL replace statement.</returns>
        /// <remarks>Parameters have the same name as the column, with an "@" suffix.</remarks>
        internal static string GetSqlReplaceStatement(string table, IEnumerable<string> headers)
        {
            return GetSqlInsertOrReplaceStatement(table, headers, true);
        }

        private static string GetSqlInsertOrReplaceStatement(string table, IEnumerable<string> headers, bool replace)
        {
            string statementType = replace ? "REPLACE" : "INSERT";
            return $"{statementType} INTO {table} ({string.Join(", ", headers)}) " +
                $"VALUES ({string.Join(", ", headers.Select(hc => string.Concat("@", hc)))})";
        }

        /// <summary>
        /// Tries to parse a column value into a specified type.
        /// </summary>
        /// <typeparam name="T">Expected type after parsing.</typeparam>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="column">Column name.</param>
        /// <returns>Parsed value; <see cref="DBNull.Value"/> if failure.</returns>
        internal static object Parse<T>(this MySqlDataReader reader, string column)
        {
            return reader.Parse<T>(column, DBNull.Value);
        }

        /// <summary>
        /// Tries to parse a column value into a specified type.
        /// </summary>
        /// <typeparam name="T">Expected type after parsing.</typeparam>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="column">Column name.</param>
        /// <param name="defaultValue">Value returned in case of <see cref="DBNull.Value"/> or conversion failure.</param>
        /// <returns>Parsed value.</returns>
        internal static object Parse<T>(this MySqlDataReader reader, string column, object defaultValue)
        {
            var rawValue = reader[column];

            if (rawValue == DBNull.Value)
            {
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(rawValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
