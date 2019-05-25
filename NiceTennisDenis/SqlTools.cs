using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using NiceTennisDenis.Properties;

namespace NiceTennisDenis
{
    /// <summary>
    /// SQL tools.
    /// </summary>
    public static class SqlTools
    {
        private static string _connectionString = null;

        /// <summary>
        /// Connection string (from the configuration).
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    _connectionString = string.Format(Settings.Default.sqlConnStringPattern,
                        Settings.Default.sqlServer,
                        Settings.Default.sqlDatabase,
                        Settings.Default.sqlUser,
                        Settings.Default.sqlPassword);
                }

                return _connectionString;
            }
        }

        /// <summary>
        /// Creates an insert SQL statement with parameters, from a list of columns.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="columnHeaders">List of columns.</param>
        /// <returns>SQL insert statement.</returns>
        /// <remarks>Parameters have the same name as the column, with an "@" suffix.</remarks>
        public static string GetSqlInsertStatement(string table, IEnumerable<string> columnHeaders)
        {
            return GetSqlInsertOrReplaceStatement(table, columnHeaders, false);
        }

        /// <summary>
        /// Creates an replace SQL statement with parameters, from a list of columns.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="columnHeaders">List of columns.</param>
        /// <returns>SQL replace statement.</returns>
        /// <remarks>Parameters have the same name as the column, with an "@" suffix.</remarks>
        public static string GetSqlReplaceStatement(string table, IEnumerable<string> columnHeaders)
        {
            return GetSqlInsertOrReplaceStatement(table, columnHeaders, true);
        }

        private static string GetSqlInsertOrReplaceStatement(string table, IEnumerable<string> columnHeaders, bool replace)
        {
            string statementType = replace ? "REPLACE" : "INSERT";
            return $"{statementType} INTO {table} ({string.Join(", ", columnHeaders)}) " +
                $"VALUES ({string.Join(", ", columnHeaders.Select(hc => string.Concat("@", hc)))})";
        }

        /// <summary>
        /// Gets a <see cref="uint"/> value or <see cref="DBNull.Value"/> from the specified <paramref name="column"/> of a <see cref="MySqlDataReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="column">Column name.</param>
        /// <returns>The value.</returns>
        public static object ToUint(this MySqlDataReader reader, string column)
        {
            return ToUint(reader, column, DBNull.Value);
        }

        /// <summary>
        /// Gets a <see cref="uint"/> value or <paramref name="defaultValue"/> from the specified <paramref name="column"/> of a <see cref="MySqlDataReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="MySqlDataReader"/></param>
        /// <param name="column">Column name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>The value.</returns>
        public static object ToUint(this MySqlDataReader reader, string column, object defaultValue)
        {
            var rawValue = reader[column];
            return rawValue == null || rawValue == DBNull.Value || !uint.TryParse(rawValue.ToString(), out uint realValue) ?
                defaultValue : realValue;
        }
    }
}
