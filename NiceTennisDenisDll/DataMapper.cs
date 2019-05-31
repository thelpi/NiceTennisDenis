using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll
{
    /// <summary>
    /// Object-relational mapping class.
    /// </summary>
    public class DataMapper
    {
        private readonly string _connectionString = null;
        private bool _modelIsLoaded = false;
        private List<uint> _matchesByYearLoaded = new List<uint>();

        private static DataMapper _default = null;

        /// <summary>
        /// Initialize singleton instance.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <returns>Initialized instance.</returns>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidConnectionStringException"/></exception>
        public static DataMapper InitializeDefault(string connectionString)
        {
            if (_default != null)
            {
                return _default;
            }

            try
            {
                using (var sqlConnection = new MySqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = "SELECT id FROM player WHERE 1 LIMIT 0, 1";
                        sqlCommand.ExecuteScalar();
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new ArgumentException(string.Concat(Messages.InvalidConnectionStringException, "\r\n", ex.Message), nameof(connectionString));
            }

            _default = new DataMapper(connectionString);
            return _default;
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Messages.NotInitializedInstanceException"/></exception>
        public static DataMapper Default
        {
            get
            {
                if (_default == null)
                {
                    throw new InvalidOperationException(Messages.NotInitializedInstanceException);
                }

                return _default;
            }
        }

        private DataMapper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Loads the full model, except <see cref="Models.MatchPivot"/>.
        /// </summary>
        /// <remarks>Does nothing if the model is already loaded.</remarks>
        public void LoadModel()
        {
            if (!_modelIsLoaded)
            {
                LoadPivotType("tournament", Models.TournamentPivot.Create);
                LoadPivotType("level", Models.LevelPivot.Create);
                LoadPivotType("round", Models.RoundPivot.Create);
                LoadPivotType("entry", Models.EntryPivot.Create);
                LoadPivotType("slot", Models.SlotPivot.Create);
                LoadPivotType("edition", Models.EditionPivot.Create);
                LoadPivotType("player", Models.PlayerPivot.Create);
                _modelIsLoaded = true;
            }
        }

        /// <summary>
        /// Loads every matches of a specified year.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <remarks>Does nothing if the model isn't loaded.</remarks>
        public void LoadMatches(uint year)
        {
            if (_modelIsLoaded && !_matchesByYearLoaded.Contains(year))
            {
                var queryBuilder = new System.Text.StringBuilder();
                queryBuilder.AppendLine("SELECT *");
                queryBuilder.AppendLine("FROM match_general AS mg");
                queryBuilder.AppendLine("JOIN match_stat AS mst ON mg.id = mst.match_id");
                queryBuilder.AppendLine("JOIN match_score AS msc ON mg.id = msc.match_id");
                queryBuilder.AppendLine("WHERE edition_id IN (SELECT id FROM edition WHERE year = @year)");

                LoadPivotTypeWithQuery(queryBuilder.ToString(), Models.MatchPivot.Create, new MySqlParameter("@year", MySqlDbType.UInt32)
                {
                    Value = year
                });
                _matchesByYearLoaded.Add(year);
            }
        }

        private void LoadPivotType<T>(string table, Func<MySqlDataReader, T> action)
            where T : Models.BasePivot
        {
            LoadPivotTypeWithQuery($"select * from {table}", action);
        }

        private void LoadPivotTypeWithQuery<T>(string query, Func<MySqlDataReader, T> action, params MySqlParameter[] parameters)
            where T : Models.BasePivot
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = query;
                    foreach (var parameter in parameters)
                    {
                        sqlCommand.Parameters.Add(parameter);
                    }
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlReader.Read())
                        {
                            action.Invoke(sqlReader);
                        }
                    }
                }
            }
        }
    }
}
