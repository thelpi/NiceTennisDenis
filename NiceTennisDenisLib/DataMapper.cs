using System;
using System.IO;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisLib
{
    /// <summary>
    /// Object-relational mapping class.
    /// </summary>
    public class DataMapper
    {
        private readonly string _connectionString = null;

        private static DataMapper _default = null;

        /// <summary>
        /// Initialize singleton instance.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <returns>Initialized instance.</returns>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidConnectionStringException"/></exception>
        public static DataMapper InitializeDefault(string connectionString)
        {
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
        public void LoadModel()
        {
            LoadPivotType("tournament", Models.TournamentPivot.Create);
            LoadPivotType("level", Models.LevelPivot.Create);
            LoadPivotType("round", Models.RoundPivot.Create);
            LoadPivotType("entry", Models.EntryPivot.Create);
            LoadPivotType("slot", Models.SlotPivot.Create);
            LoadPivotType("edition", Models.EditionPivot.Create);
            LoadPivotType("player", Models.PlayerPivot.Create);
        }

        private void LoadPivotType<T>(string table, Func<MySqlDataReader, T> action) where T : Models.BasePivot
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"select * from {table}";
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
