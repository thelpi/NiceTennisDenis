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

        /// <summary>
        /// Gets a <see cref="WinnerAndSurface"/> from a year and a slot.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="slotId">Slot identifier.</param>
        /// <returns><see cref="WinnerAndSurface"/> or <c>Null</c>.</returns>
        public static WinnerAndSurface? GetWinnerAndSurface(uint year, uint slotId)
        {
            WinnerAndSurface? result = null;

            using (var sqlConnection = new MySqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    var sql = new System.Text.StringBuilder();
                    sql.AppendLine("SELECT p.first_name, p.last_name, e.surface_id, e.indoor");
                    sql.AppendLine("FROM player AS p");
                    sql.AppendLine("JOIN match_general AS m ON p.id = m.winner_id");
                    sql.AppendLine("JOIN edition AS e ON m.edition_id = e.id");
                    sql.AppendLine("WHERE m.round_id = @round AND e.year = @year AND e.slot_id = @slot");

                    sqlCommand.CommandText = sql.ToString();
                    sqlCommand.Parameters.Add("@round", MySqlDbType.UInt32);
                    sqlCommand.Parameters.Add("@year", MySqlDbType.UInt32);
                    sqlCommand.Parameters.Add("@slot", MySqlDbType.UInt32);
                    sqlCommand.Parameters["@round"].Value = 1; // TODO : constant
                    sqlCommand.Parameters["@year"].Value = year;
                    sqlCommand.Parameters["@slot"].Value = slotId;

                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        if (sqlReader.Read())
                        {
                            result = new WinnerAndSurface(
                                sqlReader.GetString("first_name"),
                                sqlReader.GetString("last_name"),
                                (uint?)sqlReader.ToUint("surface_id", null),
                                sqlReader.GetByte("indoor") > 0);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets every slots inside a given period range.
        /// </summary>
        /// <param name="minYear">The year to begin.</param>
        /// <param name="maxYear">The year to end.</param>
        /// <returns>List of <see cref="Slot"/>.</returns>
        public static List<Slot> GetSlots(uint? minYear, uint? maxYear)
        {
            List<Slot> slots = new List<Slot>();

            using (var sqlConnection = new MySqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    var sql = new System.Text.StringBuilder();
                    sql.AppendLine("SELECT s.id, s.name, l.name AS level_name, s.display_order, (");
                    sql.AppendLine("    SELECT MAX(e.year) FROM edition AS e WHERE e.slot_id = s.id");
                    sql.AppendLine(") AS date_end, (");
                    sql.AppendLine("    SELECT MIN(e.year) FROM edition AS e WHERE e.slot_id = s.id");
                    sql.AppendLine(") AS date_begin");
                    sql.AppendLine("FROM slot AS s");
                    sql.AppendLine("JOIN level AS l ON s.level_id = l.id");
                    sql.AppendLine("ORDER BY l.importance ASC, s.display_order ASC");

                    sqlCommand.CommandText = sql.ToString();

                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            if ((!minYear.HasValue || sqlReader.GetUInt32("date_end") >= minYear.Value)
                                && (!maxYear.HasValue || sqlReader.GetUInt32("date_begin") <= maxYear.Value))
                            {
                                slots.Add(new Slot(
                                    sqlReader.GetUInt32("id"),
                                    sqlReader.GetString("name"),
                                    sqlReader.GetString("level_name"),
                                    sqlReader.GetUInt32("display_order")));
                            }
                        }
                    }
                }
            }

            return slots;
        }
    }
}
