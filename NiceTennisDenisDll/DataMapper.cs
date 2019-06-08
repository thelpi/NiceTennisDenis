using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using NiceTennisDenisDll.Models;

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
        private readonly Import _import = null;

        private static DataMapper _default = null;

        /// <summary>
        /// Initialize singleton instance.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="datasDirectory">Datas directory.</param>
        /// <returns>Initialized instance.</returns>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidConnectionStringException"/></exception>
        public static DataMapper InitializeDefault(string connectionString, string datasDirectory)
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

            _default = new DataMapper(connectionString, datasDirectory);
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

        private DataMapper(string connectionString, string datasDirectory)
        {
            _connectionString = connectionString;
            _import = new Import(connectionString, datasDirectory);
        }

        /// <summary>
        /// Loads the full model, except <see cref="MatchPivot"/>.
        /// </summary>
        /// <remarks>Does nothing if the model is already loaded.</remarks>
        public void LoadModel()
        {
            if (!_modelIsLoaded)
            {
                LoadPivotType("tournament", TournamentPivot.Create);
                LoadPivotType("level", LevelPivot.Create);
                LoadPivotType("round", RoundPivot.Create);
                LoadPivotType("entry", EntryPivot.Create);
                LoadPivotType("slot", SlotPivot.Create);
                LoadPivotType("edition", EditionPivot.Create);
                LoadPivotType("player", PlayerPivot.Create);
                LoadPivotType("atp_grid_point", AtpGridPointPivot.Create);
                LoadPivotType("atp_qualification_point", AtpQualificationPivot.Create);

                var sqlQuery = new StringBuilder();
                sqlQuery.AppendLine("select id, creation_date, group_concat(rule_id) as rules_concat");
                sqlQuery.AppendLine("from atp_ranking_version");
                sqlQuery.AppendLine("left join atp_ranking_version_rule on id = version_id");
                sqlQuery.AppendLine("group by id, creation_date");
                LoadPivotTypeWithQuery(sqlQuery.ToString(), AtpRankingVersionPivot.Create);

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
                var queryBuilder = new StringBuilder();
                queryBuilder.AppendLine("SELECT *");
                queryBuilder.AppendLine("FROM match_general AS mg");
                queryBuilder.AppendLine("JOIN match_stat AS mst ON mg.id = mst.match_id");
                queryBuilder.AppendLine("JOIN match_score AS msc ON mg.id = msc.match_id");
                queryBuilder.AppendLine("WHERE edition_id IN (SELECT id FROM edition WHERE year = @year)");

                LoadPivotTypeWithQuery(queryBuilder.ToString(), MatchPivot.Create, new MySqlParameter("@year", MySqlDbType.UInt32)
                {
                    Value = year
                });
                _matchesByYearLoaded.Add(year);
            }
        }

        private void LoadPivotType<T>(string table, Func<MySqlDataReader, T> action)
            where T : BasePivot
        {
            LoadPivotTypeWithQuery($"select * from {table}", action);
        }

        private void LoadPivotTypeWithQuery<T>(string query, Func<MySqlDataReader, T> action, params MySqlParameter[] parameters)
            where T : BasePivot
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

        /// <summary>
        /// <see cref="Import.GenerateAtpRanking"/>
        /// </summary>
        public void GenerateAtpRanking(uint versionId)
        {
            _import.GenerateAtpRanking(versionId);
        }

        /// <summary>
        /// Debugs ATP ranking calculation.
        /// </summary>
        /// <param name="playerId"><see cref="PlayerPivot"/> identifier.</param>
        /// <param name="versionId"><see cref="AtpRankingVersionPivot"/> identifier.</param>
        /// <param name="dateEnd"></param>
        public void DebugAtpRankingForPlayer(uint playerId, uint versionId, DateTime dateEnd)
        {
            LoadModel();

            // Ensures monday.
            while (dateEnd.DayOfWeek != DayOfWeek.Monday)
            {
                dateEnd = dateEnd.AddDays(1);
            }

            var player = PlayerPivot.Get(playerId);
            var atpRankingVersion = AtpRankingVersionPivot.Get(versionId);
            if (player == null || atpRankingVersion == null)
            {
                return;
            }

            LoadMatches((uint)(dateEnd.Year - 1));
            LoadMatches((uint)dateEnd.Year);

            Import.ComputePointsAndCountForPlayer(atpRankingVersion, player,
                EditionPivot.EditionsForAtpRankingAtDate(atpRankingVersion, dateEnd, out IReadOnlyCollection<PlayerPivot> playersInvolved),
                new Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint>());
        }

        private class Import
        {
            private const string SOURCE_FILE_FOLDER_NAME = "tennis_atp-master";
            private const string MATCHES_FILE_NAME_PATTERN = "atp_matches_{0}.csv";
            private const string PLAYERS_FILE_NAME = "atp_players.csv";
            private const int DEFAULT_STRING_COL_SIZE = 255;
            private const string COLUMN_SEPARATOR = ",";

            private readonly string _connectionString;
            private readonly string _datasDirectory;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="connectionString">Connection string.</param>
            /// <param name="datasDirectory">Datas directory.</param>
            public Import(string connectionString, string datasDirectory)
            {
                _connectionString = connectionString;
                _datasDirectory = datasDirectory;
            }

            /// <summary>
            /// Proceeds to import a single file of matches in the "source_datas" table of the database.
            /// </summary>
            /// <param name="year">The year of matches.</param>
            /// <remarks>Reaplce existing lines.</remarks>
            /// <exception cref="ArgumentException"><see cref="Messages.NoYearDataFileFoundException"/></exception>
            /// <exception cref="ArgumentException"><see cref="Messages.InvalidMatchesFileDatasException"/></exception>
            /// <exception cref="ArgumentException"><see cref="Messages.InvalidDatasToHeadersException"/></exception>
            public void ImportSingleMatchesFileInDatabase(int year)
            {
                string fileName = string.Format(MATCHES_FILE_NAME_PATTERN, year);

                string fullFileName = Path.Combine(_datasDirectory, SOURCE_FILE_FOLDER_NAME, fileName);
                if (!File.Exists(fullFileName))
                {
                    throw new ArgumentException(Messages.NoYearDataFileFoundException, nameof(year));
                }

                ExtractMatchesColumnsHeadersAndValues(fileName, fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent);

                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = MySqlTools.GetSqlReplaceStatement("source_matches", headerColumns);
                        headerColumns.ForEach(me => sqlCommand.Parameters.Add(string.Concat("@", me), MySqlDbType.String, DEFAULT_STRING_COL_SIZE));
                        sqlCommand.Prepare();
                        foreach (var contentLine in linesOfContent)
                        {
                            int i = 0;
                            foreach (var contentColumn in contentLine)
                            {
                                sqlCommand.Parameters[i].Value = contentColumn;
                                i++;
                            }
                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }
            }

            private static void ExtractMatchesColumnsHeadersAndValues(string fileName, string fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent)
            {
                headerColumns = null;
                linesOfContent = new List<List<string>>();
                using (var reader = new StreamReader(fullFileName))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        List<string> columnsList = line.Split(new string[] { COLUMN_SEPARATOR }, StringSplitOptions.None).ToList();
                        if (headerColumns == null)
                        {
                            headerColumns = columnsList;
                        }
                        else if (columnsList.Count != headerColumns.Count)
                        {
                            throw new Exception(Messages.InvalidDatasToHeadersException);
                        }
                        else
                        {
                            linesOfContent.Add(columnsList);
                        }
                    }
                }

                if (headerColumns == null || linesOfContent.Count == 0)
                {
                    throw new Exception(Messages.InvalidMatchesFileDatasException);
                }
                headerColumns.Add("file_name");
                linesOfContent.ForEach(line => line.Add(fileName));
            }

            /// <summary>
            /// Proceeds to import the file of players in the "source_players" table of database.
            /// Imports only players not processed. Existing non-processed players are replaced.
            /// </summary>
            /// <exception cref="Exception"><see cref="Messages.PlayersDatasFileNotFoundException"/></exception>
            /// <exception cref="Exception"><see cref="Messages.InvalidDatasToHeadersException"/></exception>
            /// <exception cref="Exception"><see cref="Messages.InvalidPlayersFileDatasException"/></exception>
            public void ImportNewPlayers()
            {
                string fullFileName = Path.Combine(_datasDirectory, PLAYERS_FILE_NAME);
                if (!File.Exists(fullFileName))
                {
                    throw new Exception(Messages.PlayersDatasFileNotFoundException);
                }

                List<string> playersUnableToRemove = ComputeUnremovablePlayersList();

                ExtractPlayersColumnsHeadersAndValues(fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent);

                int idIndexOf = headerColumns.IndexOf("player_id");

                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = MySqlTools.GetSqlReplaceStatement("source_players", headerColumns);
                        headerColumns.ForEach(hc => sqlCommand.Parameters.Add(string.Concat("@", hc), MySqlDbType.String, 255));
                        sqlCommand.Prepare();
                        foreach (var contentLine in linesOfContent.Where(lc => !playersUnableToRemove.Contains(lc[idIndexOf])))
                        {
                            int i = 0;
                            foreach (var contentColumn in contentLine)
                            {
                                sqlCommand.Parameters[i].Value = contentColumn;
                                i++;
                            }
                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }
            }

            private List<string> ComputeUnremovablePlayersList()
            {
                List<string> players = new List<string>();

                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = "SELECT player_id FROM source_players WHERE date_processed IS NOT NULL";
                        using (var sqlReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                players.Add(sqlReader.GetString("player_id"));
                            }
                        }
                    }
                }

                return players;
            }

            private static void ExtractPlayersColumnsHeadersAndValues(string fullFileName,
                out List<string> headerColumns, out List<List<string>> linesOfContent)
            {
                headerColumns = null;
                linesOfContent = new List<List<string>>();
                using (var reader = new StreamReader(fullFileName))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            List<string> columnsList = line.Split(new string[] { COLUMN_SEPARATOR }, StringSplitOptions.None).ToList();
                            if (headerColumns == null)
                            {
                                headerColumns = columnsList;
                            }
                            else if (columnsList.Count != headerColumns.Count)
                            {
                                throw new Exception(Messages.InvalidDatasToHeadersException);
                            }
                            else
                            {
                                linesOfContent.Add(columnsList);
                            }
                        }
                    }
                }

                if (headerColumns == null || linesOfContent.Count == 0)
                {
                    throw new Exception(Messages.InvalidPlayersFileDatasException);
                }
            }

            /// <summary>
            /// Creates in the table "player" players pending in the "source_players" table.
            /// </summary>
            public void CreatePendingPlayersFromSource()
            {
                using (MySqlConnection sqlConnectionRead = new MySqlConnection(_connectionString),
                    sqlConnectionUpd = new MySqlConnection(_connectionString))
                {
                    sqlConnectionRead.Open();
                    sqlConnectionUpd.Open();
                    using (MySqlCommand sqlCommandRead = sqlConnectionRead.CreateCommand(),
                        sqlCommandUpd = sqlConnectionUpd.CreateCommand())
                    {
                        var columnsHeaders = new List<string>
                        {
                            "id", "first_name", "last_name", "hand", "birth_date", "country"
                        };
                        sqlCommandUpd.CommandText = MySqlTools.GetSqlInsertStatement("player", columnsHeaders);
                        sqlCommandUpd.Parameters.Add("@id", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@first_name", MySqlDbType.String, 255);
                        sqlCommandUpd.Parameters.Add("@last_name", MySqlDbType.String, 255);
                        sqlCommandUpd.Parameters.Add("@hand", MySqlDbType.String, 1);
                        sqlCommandUpd.Parameters.Add("@birth_date", MySqlDbType.DateTime);
                        sqlCommandUpd.Parameters.Add("@country", MySqlDbType.String, 3);
                        sqlCommandUpd.Prepare();

                        sqlCommandRead.CommandText = "select CONVERT(player_id, UNSIGNED) AS id, TRIM(name_first) AS first_name, " +
                            "TRIM(name_list) AS last_name, IF(hand = 'U', NULL, hand) AS hand, CONVERT(birthdate, DATETIME) AS birth_date, country " +
                            "FROM source_players WHERE date_processed IS NULL";
                        using (var sqlReader = sqlCommandRead.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                columnsHeaders.ForEach(ch => sqlCommandUpd.Parameters[string.Concat("@", ch)].Value = sqlReader[ch]);
                                sqlCommandUpd.ExecuteNonQuery();
                            }
                        }

                        // Note : variable's name is misleading at this point.
                        sqlCommandRead.CommandText = "UPDATE source_players SET date_processed = NOW() WHERE date_processed IS NULL";
                        sqlCommandRead.ExecuteNonQuery();
                    }
                }
            }

            /// <summary>
            /// Creates in the table "edition" tournaments editions pending in the "source_matches" table.
            /// </summary>
            public void CreatePendingTournamentEditionsFromSource()
            {
                var surfaces = new Dictionary<string, uint>();
                var levels = new Dictionary<string, uint>();

                using (MySqlConnection sqlConnectionReadSurface = new MySqlConnection(_connectionString),
                     sqlConnectionReaderLevel = new MySqlConnection(_connectionString))
                {
                    sqlConnectionReadSurface.Open();
                    sqlConnectionReaderLevel.Open();
                    using (MySqlCommand sqlCommandReadSurface = sqlConnectionReadSurface.CreateCommand(),
                        sqlCommandReadLevel = sqlConnectionReaderLevel.CreateCommand())
                    {
                        sqlCommandReadSurface.CommandText = "select name, id from surface";
                        sqlCommandReadLevel.CommandText = "select code, id from level";
                        using (var sqlReaderSurface = sqlCommandReadSurface.ExecuteReader())
                        {
                            while (sqlReaderSurface.Read())
                            {
                                surfaces.Add(sqlReaderSurface.GetString("name"), sqlReaderSurface.Get<uint>("id"));
                            }
                        }
                        using (var sqlReaderLevel = sqlCommandReadLevel.ExecuteReader())
                        {
                            while (sqlReaderLevel.Read())
                            {
                                levels.Add(sqlReaderLevel.GetString("code"), sqlReaderLevel.Get<uint>("id"));
                            }
                        }
                    }
                }

                using (MySqlConnection sqlConnectionRead = new MySqlConnection(_connectionString),
                    sqlConnectionUpd = new MySqlConnection(_connectionString))
                {
                    sqlConnectionRead.Open();
                    sqlConnectionUpd.Open();
                    using (MySqlCommand sqlCommandRead = sqlConnectionRead.CreateCommand(),
                        sqlCommandUpd = sqlConnectionUpd.CreateCommand())
                    {
                        var columnsHeaders = new List<string>
                        {
                            "year", "code", "name", "surface_id", "indoor", "draw_size", "level_id",
                            "date_begin", "date_end", "tournament_id", "slot_id"
                        };
                        sqlCommandUpd.CommandText = MySqlTools.GetSqlInsertStatement("edition", columnsHeaders);
                        sqlCommandUpd.Parameters.Add("@year", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@code", MySqlDbType.String, 255);
                        sqlCommandUpd.Parameters.Add("@name", MySqlDbType.String, 255);
                        sqlCommandUpd.Parameters.Add("@surface_id", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@indoor", MySqlDbType.Byte);
                        sqlCommandUpd.Parameters.Add("@draw_size", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@level_id", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@date_begin", MySqlDbType.DateTime);
                        sqlCommandUpd.Parameters.Add("@date_end", MySqlDbType.DateTime);
                        sqlCommandUpd.Parameters.Add("@tournament_id", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@slot_id", MySqlDbType.UInt32);
                        sqlCommandUpd.Prepare();

                        var getEditionsSql = new StringBuilder();
                        getEditionsSql.AppendLine("SELECT");
                        getEditionsSql.AppendLine(" CONVERT(SUBSTR(tourney_id, 1, 4), UNSIGNED) AS year,");
                        getEditionsSql.AppendLine(" SUBSTR(tourney_id, 6, 255) AS code,");
                        getEditionsSql.AppendLine(" MIN(tourney_name) AS name,");
                        getEditionsSql.AppendLine(" MIN(surface) AS surface,");
                        // indoor from previous edition
                        getEditionsSql.AppendLine(" (");
                        getEditionsSql.AppendLine("     SELECT indoor FROM edition");
                        getEditionsSql.AppendLine("     WHERE (year + 1) = SUBSTR(tourney_id, 1, 4) AND code = SUBSTR(tourney_id, 6, 255)");
                        getEditionsSql.AppendLine("     LIMIT 0, 1");
                        getEditionsSql.AppendLine(" ) as indoor,");
                        getEditionsSql.AppendLine(" CONVERT(MIN(draw_size), UNSIGNED) AS draw_size,");
                        getEditionsSql.AppendLine(" MIN(tourney_level) AS tourney_level,");
                        getEditionsSql.AppendLine(" CONVERT(MIN(tourney_date), DATETIME) AS tourney_date,");
                        // slot from previous edition
                        getEditionsSql.AppendLine(" (");
                        getEditionsSql.AppendLine("     SELECT slot_id FROM edition");
                        getEditionsSql.AppendLine("     WHERE (year + 1) = SUBSTR(tourney_id, 1, 4) AND code = SUBSTR(tourney_id, 6, 255)");
                        getEditionsSql.AppendLine("     LIMIT 0, 1");
                        getEditionsSql.AppendLine(" ) as slot_id,");
                        getEditionsSql.AppendLine(" (");
                        getEditionsSql.AppendLine("     SELECT id FROM tournament");
                        getEditionsSql.AppendLine("     WHERE known_codes = SUBSTR(tourney_id, 6, 255)");
                        getEditionsSql.AppendLine("         OR known_codes LIKE CONCAT(SUBSTR(tourney_id, 6, 255), ';%')");
                        getEditionsSql.AppendLine("         OR known_codes LIKE CONCAT('%;', SUBSTR(tourney_id, 6, 255))");
                        getEditionsSql.AppendLine("         OR known_codes LIKE CONCAT('%;', SUBSTR(tourney_id, 6, 255), ';%')");
                        getEditionsSql.AppendLine("     LIMIT 0, 1");
                        getEditionsSql.AppendLine(" ) as tournament_id, (");
                        getEditionsSql.AppendLine("     SELECT COUNT(*) FROM source_matches AS sm2 WHERE sm2.tourney_id = sm.tourney_id");
                        getEditionsSql.AppendLine(" ) as matches_count");
                        getEditionsSql.AppendLine("FROM source_matches AS sm");
                        getEditionsSql.AppendLine("WHERE NOT EXISTS (");
                        getEditionsSql.AppendLine("     SELECT 1 FROM edition");
                        getEditionsSql.AppendLine("     WHERE year = SUBSTR(tourney_id, 1, 4) AND code = SUBSTR(tourney_id, 6, 255)");
                        getEditionsSql.AppendLine(")");
                        getEditionsSql.AppendLine("GROUP BY");
                        getEditionsSql.AppendLine(" CONVERT(SUBSTR(tourney_id, 1, 4), UNSIGNED),");
                        getEditionsSql.AppendLine(" SUBSTR(tourney_id, 6, 255)");
                        getEditionsSql.AppendLine("ORDER BY");
                        getEditionsSql.AppendLine(" CONVERT(SUBSTR(tourney_id, 1, 4), UNSIGNED) ASC,");
                        getEditionsSql.AppendLine(" SUBSTR(tourney_id, 6, 255) ASC");

                        sqlCommandRead.CommandText = getEditionsSql.ToString();
                        using (var sqlReader = sqlCommandRead.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                sqlCommandUpd.Parameters["@year"].Value = sqlReader["year"];
                                sqlCommandUpd.Parameters["@code"].Value = sqlReader["code"];
                                sqlCommandUpd.Parameters["@name"].Value = sqlReader["name"];
                                sqlCommandUpd.Parameters["@surface_id"].Value = surfaces.ContainsKey(sqlReader.GetString("surface")) ?
                                    (object)surfaces[sqlReader.GetString("surface")] : DBNull.Value;
                                sqlCommandUpd.Parameters["@indoor"].Value = sqlReader["indoor"];
                                sqlCommandUpd.Parameters["@draw_size"].Value = sqlReader["draw_size"];
                                sqlCommandUpd.Parameters["@level_id"].Value = levels[sqlReader.GetString("tourney_level")];
                                sqlCommandUpd.Parameters["@date_begin"].Value = sqlReader["tourney_date"];
                                sqlCommandUpd.Parameters["@date_end"].Value =
                                    ComputeEditionDateEnd(sqlReader.GetString("tourney_date"), sqlReader.GetInt32("matches_count"));
                                sqlCommandUpd.Parameters["@tournament_id"].Value = sqlReader["tournament_id"];
                                sqlCommandUpd.Parameters["@slot_id"].Value = sqlReader["slot_id"];
                                sqlCommandUpd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Updates the "height" information on players from the source of matches.
            /// </summary>
            public void UpdatePlayersHeightFromMatchesSource()
            {
                List<uint> playersId = new List<uint>();

                using (MySqlConnection sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (MySqlCommand sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = "SELECT id FROM player WHERE height IS NULL";
                        using (var sqlReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                playersId.Add(sqlReader.Get<uint>("id"));
                            }
                        }
                    }
                }

                using (MySqlConnection sqlConnectionRead = new MySqlConnection(_connectionString),
                    sqlConnectionUpd = new MySqlConnection(_connectionString))
                {
                    sqlConnectionRead.Open();
                    sqlConnectionUpd.Open();
                    using (MySqlCommand sqlCommandRead = sqlConnectionRead.CreateCommand(),
                        sqlCommandUpd = sqlConnectionUpd.CreateCommand())
                    {
                        sqlCommandUpd.CommandText = "UPDATE player SET height = @height WHERE id = @id";
                        sqlCommandUpd.Parameters.Add("@id", MySqlDbType.UInt32);
                        sqlCommandUpd.Parameters.Add("@height", MySqlDbType.UInt32);
                        sqlCommandUpd.Prepare();

                        var getHeightSql = new StringBuilder();
                        getHeightSql.AppendLine("SELECT tmp.pid, tmp.ht");
                        getHeightSql.AppendLine("FROM (");
                        getHeightSql.AppendLine("   SELECT CONVERT(winner_id, UNSIGNED) AS pid, CONVERT(winner_ht, UNSIGNED) AS ht");
                        getHeightSql.AppendLine("   FROM source_matches");
                        getHeightSql.AppendLine($"  WHERE CONVERT(winner_id, UNSIGNED) IN ({string.Join(", ", playersId)})");
                        getHeightSql.AppendLine("   )");
                        getHeightSql.AppendLine("   AND CONVERT(winner_ht, UNSIGNED) IS NOT NULL");
                        getHeightSql.AppendLine("   AND CONVERT(winner_ht, UNSIGNED) > 0");
                        getHeightSql.AppendLine("   UNION ALL");
                        getHeightSql.AppendLine("   SELECT CONVERT(loser_id, UNSIGNED) AS pid, CONVERT(loser_ht, UNSIGNED) AS ht");
                        getHeightSql.AppendLine("   FROM source_matches");
                        getHeightSql.AppendLine($"  WHERE CONVERT(loser_id, UNSIGNED) IN ({string.Join(", ", playersId)})");
                        getHeightSql.AppendLine("   AND CONVERT(loser_ht, UNSIGNED) IS NOT NULL");
                        getHeightSql.AppendLine("   AND CONVERT(loser_ht, UNSIGNED) > 0");
                        getHeightSql.AppendLine(") AS tmp");
                        getHeightSql.AppendLine("GROUP BY tmp.pid, tmp.ht");
                        getHeightSql.AppendLine("ORDER BY tmp.pid, COUNT(*) DESC");

                        sqlCommandRead.CommandText = getHeightSql.ToString();
                        using (var sqlReader = sqlCommandRead.ExecuteReader())
                        {
                            uint? currentPlayerId = null;
                            while (sqlReader.Read())
                            {
                                if (!currentPlayerId.HasValue || currentPlayerId.Value != sqlReader.Get<uint>("pid"))
                                {
                                    currentPlayerId = sqlReader.Get<uint>("pid");
                                    sqlCommandUpd.Parameters["@id"].Value = currentPlayerId;
                                    sqlCommandUpd.Parameters["@height"].Value = sqlReader["ht"];
                                    sqlCommandUpd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Creates pending matches from the table "source_matches".
            /// </summary>
            public void CreatePendingMatchesFromSource()
            {
                var editions = new Dictionary<string, uint>();
                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = "select id, concat(year, '-', code) as full_code from edition";
                        using (var sqlReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                editions.Add(sqlReader.GetString("full_code"), sqlReader.Get<uint>("id"));
                            }
                        }
                    }
                }

                var entries = new Dictionary<string, uint>();
                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = "select code, id from entry";
                        using (var sqlReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                entries.Add(sqlReader.GetString("code"), sqlReader.Get<uint>("id"));
                            }
                        }
                    }
                }

                var rounds = new Dictionary<string, uint>();
                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = "select code, id from round";
                        using (var sqlReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                rounds.Add(sqlReader.GetString("code"), sqlReader.Get<uint>("id"));
                            }
                        }
                    }
                }

                var retirements = new List<string> { "ret", "abd", "abn", "aba" };
                var walkovers = new List<string> { "w/o", "walkover", "wo" };
                var disqualifications = new List<string> { "def", "disq" };
                var unfinished = new List<string> { "unfinished" };

                using (MySqlConnection sqlConnectionGetMatches = new MySqlConnection(_connectionString),
                    sqlConnectionCreateGeneral = new MySqlConnection(_connectionString),
                    sqlConnectionCreateScore = new MySqlConnection(_connectionString),
                    sqlConnectionCreateStat = new MySqlConnection(_connectionString),
                    sqlConnectionUpdateDateprocessed = new MySqlConnection(_connectionString))
                {
                    sqlConnectionGetMatches.Open();
                    sqlConnectionCreateGeneral.Open();
                    sqlConnectionCreateScore.Open();
                    sqlConnectionCreateStat.Open();
                    sqlConnectionUpdateDateprocessed.Open();
                    using (MySqlCommand sqlCommandGetMatches = sqlConnectionGetMatches.CreateCommand(),
                        sqlCommandCreateGeneral = sqlConnectionCreateGeneral.CreateCommand(),
                        sqlCommandCreateScore = sqlConnectionCreateScore.CreateCommand(),
                        sqlCommandCreateStat = sqlConnectionCreateStat.CreateCommand(),
                        sqlCommandUpdateDateProcessed = sqlConnectionUpdateDateprocessed.CreateCommand())
                    {
                        #region Prepares insert match_general

                        var genHeadersColumnsUint = new List<string>
                        {
                            "edition_id", "match_num", "best_of", "round_id", "minutes",
                            "winner_id", "winner_seed", "winner_entry_id", "winner_rank", "winner_rank_points",
                            "loser_id", "loser_seed", "loser_entry_id", "loser_rank", "loser_rank_points"
                        };
                        var genHeadersColumnsBool = new List<string>
                        {
                            "walkover", "retirement", "disqualification", "unfinished"
                        };
                        sqlCommandCreateGeneral.CommandText = MySqlTools.GetSqlInsertStatement("match_general", genHeadersColumnsUint.Concat(genHeadersColumnsBool));
                        foreach (var headerColumn in genHeadersColumnsUint)
                        {
                            sqlCommandCreateGeneral.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.UInt32);
                        }
                        foreach (var headerColumn in genHeadersColumnsBool)
                        {
                            sqlCommandCreateGeneral.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.Byte);
                        }
                        sqlCommandCreateGeneral.Prepare();

                        #endregion

                        #region Prepares insert match_score

                        var scoreHeadersColumnsUint = new List<string>();
                        for (var i = 1; i <= 5; i++)
                        {
                            scoreHeadersColumnsUint.Add($"w_set_{i}");
                            scoreHeadersColumnsUint.Add($"l_set_{i}");
                            scoreHeadersColumnsUint.Add($"tb_set_{i}");
                        }
                        var scoreHeadersColumnsOther = new List<string> { "match_id", "super_tb" };
                        sqlCommandCreateScore.CommandText = MySqlTools.GetSqlInsertStatement("match_score", scoreHeadersColumnsUint.Concat(scoreHeadersColumnsOther));
                        sqlCommandCreateScore.Parameters.Add("@match_id", MySqlDbType.UInt64);
                        sqlCommandCreateScore.Parameters.Add("@super_tb", MySqlDbType.String, 255);
                        foreach (var headerColumn in scoreHeadersColumnsUint)
                        {
                            sqlCommandCreateScore.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.UInt32);
                        }
                        sqlCommandCreateScore.Prepare();

                        #endregion

                        #region Prepares insert match_stat

                        var statHeadersColumns = new List<string>
                        {
                            "match_id", "w_ace", "l_ace", "w_df", "l_df", "w_sv_pt", "l_sv_pt", "w_1st_in", "l_1st_in", "w_1st_won", "l_1st_won",
                            "w_2nd_won", "l_2nd_won", "w_sv_gms", "l_sv_gms", "w_bp_saved", "l_bp_saved", "w_bp_faced", "l_bp_faced"
                        };
                        sqlCommandCreateStat.CommandText = MySqlTools.GetSqlInsertStatement("match_stat", statHeadersColumns);
                        sqlCommandCreateStat.Parameters.Add("@match_id", MySqlDbType.UInt64);
                        // Note : the first column "match_id" is skipped.
                        foreach (var headerColumn in statHeadersColumns.Skip(1))
                        {
                            sqlCommandCreateStat.Parameters.Add(string.Concat("@", headerColumn), MySqlDbType.UInt32);
                        }
                        sqlCommandCreateStat.Prepare();

                        #endregion

                        // Prepares update source_match
                        sqlCommandUpdateDateProcessed.CommandText = "UPDATE source_matches SET date_processed = NOW() WHERE id = @id";
                        sqlCommandUpdateDateProcessed.Parameters.Add("@id", MySqlDbType.UInt64);
                        sqlCommandUpdateDateProcessed.Prepare();

                        // Gets matches to insert
                        sqlCommandGetMatches.CommandText = "SELECT * FROM source_matches WHERE date_processed IS NULL";
                        using (var sqlReader = sqlCommandGetMatches.ExecuteReader())
                        {
                            while (sqlReader.Read())
                            {
                                string rawScore = sqlReader.GetString("score").Trim().ToLowerInvariant();

                                #region inserts general

                                sqlCommandCreateGeneral.Parameters["@edition_id"].Value = editions[sqlReader.GetString("tourney_id")];
                                sqlCommandCreateGeneral.Parameters["@match_num"].Value = Convert.ToUInt32(sqlReader["match_num"]);
                                sqlCommandCreateGeneral.Parameters["@best_of"].Value = Convert.ToUInt32(sqlReader["best_of"]);
                                sqlCommandCreateGeneral.Parameters["@round_id"].Value = rounds[sqlReader.GetString("round")];
                                sqlCommandCreateGeneral.Parameters["@minutes"].Value = sqlReader.Parse<uint>("minutes");
                                sqlCommandCreateGeneral.Parameters["@winner_id"].Value = Convert.ToUInt32(sqlReader["winner_id"]);
                                sqlCommandCreateGeneral.Parameters["@winner_seed"].Value = sqlReader.Parse<uint>("winner_seed");
                                sqlCommandCreateGeneral.Parameters["@winner_entry_id"].Value =
                                    !string.IsNullOrWhiteSpace(sqlReader.GetString("winner_entry")) ?
                                        (object)entries[sqlReader.GetString("winner_entry")] : DBNull.Value;
                                sqlCommandCreateGeneral.Parameters["@winner_rank"].Value = sqlReader.Parse<uint>("winner_rank");
                                sqlCommandCreateGeneral.Parameters["@winner_rank_points"].Value = sqlReader.Parse<uint>("winner_rank_points");
                                sqlCommandCreateGeneral.Parameters["@loser_id"].Value = Convert.ToUInt32(sqlReader["loser_id"]);
                                sqlCommandCreateGeneral.Parameters["@loser_seed"].Value = sqlReader.Parse<uint>("loser_seed");
                                sqlCommandCreateGeneral.Parameters["@loser_entry_id"].Value =
                                    !string.IsNullOrWhiteSpace(sqlReader.GetString("loser_entry")) ?
                                        (object)entries[sqlReader.GetString("loser_entry")] : DBNull.Value;
                                sqlCommandCreateGeneral.Parameters["@loser_rank"].Value = sqlReader.Parse<uint>("loser_rank");
                                sqlCommandCreateGeneral.Parameters["@loser_rank_points"].Value = sqlReader.Parse<uint>("loser_rank_points");
                                sqlCommandCreateGeneral.Parameters["@walkover"].Value =
                                    walkovers.Any(x => rawScore.Contains(x)) ? 1 : 0;
                                sqlCommandCreateGeneral.Parameters["@retirement"].Value =
                                    retirements.Any(x => rawScore.Contains(x)) ? 1 : 0;
                                sqlCommandCreateGeneral.Parameters["@disqualification"].Value =
                                    disqualifications.Any(x => rawScore.Contains(x)) ? 1 : 0;
                                sqlCommandCreateGeneral.Parameters["@unfinished"].Value =
                                    unfinished.Any(x => rawScore.Contains(x)) ? 1 : 0;
                                sqlCommandCreateGeneral.ExecuteNonQuery();

                                #endregion

                                long matchId = sqlCommandCreateGeneral.LastInsertedId;

                                #region Inserts score

                                ParseScore(rawScore, out List<List<uint?>> result, out string superTb);

                                if (result != null)
                                {
                                    sqlCommandCreateScore.Parameters["@match_id"].Value = matchId;
                                    sqlCommandCreateScore.Parameters["@w_set_1"].Value =
                                        result[0][0].HasValue ? (object)result[0][0].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@l_set_1"].Value =
                                        result[0][1].HasValue ? (object)result[0][1].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@tb_set_1"].Value =
                                        result[0][2].HasValue ? (object)result[0][2].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@w_set_2"].Value =
                                        result[1][0].HasValue ? (object)result[1][0].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@l_set_2"].Value =
                                        result[1][1].HasValue ? (object)result[1][1].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@tb_set_2"].Value =
                                        result[1][2].HasValue ? (object)result[1][2].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@w_set_3"].Value =
                                        result[2][0].HasValue ? (object)result[2][0].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@l_set_3"].Value =
                                        result[2][1].HasValue ? (object)result[2][1].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@tb_set_3"].Value =
                                        result[2][2].HasValue ? (object)result[2][2].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@w_set_4"].Value =
                                        result[3][0].HasValue ? (object)result[3][0].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@l_set_4"].Value =
                                        result[3][1].HasValue ? (object)result[3][1].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@tb_set_4"].Value =
                                        result[3][2].HasValue ? (object)result[3][2].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@w_set_5"].Value =
                                        result[4][0].HasValue ? (object)result[4][0].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@l_set_5"].Value =
                                        result[4][1].HasValue ? (object)result[4][1].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@tb_set_5"].Value =
                                        result[4][2].HasValue ? (object)result[4][2].Value : DBNull.Value;
                                    sqlCommandCreateScore.Parameters["@super_tb"].Value =
                                        !string.IsNullOrWhiteSpace(superTb) ? (object)superTb : DBNull.Value;
                                    sqlCommandCreateScore.ExecuteNonQuery();
                                }

                                #endregion

                                #region Inserts match_stat

                                sqlCommandCreateStat.Parameters["@match_id"].Value = matchId;
                                sqlCommandCreateStat.Parameters["@w_ace"].Value = sqlReader.Parse<uint>("w_ace");
                                sqlCommandCreateStat.Parameters["@l_ace"].Value = sqlReader.Parse<uint>("l_ace");
                                sqlCommandCreateStat.Parameters["@w_df"].Value = sqlReader.Parse<uint>("w_df");
                                sqlCommandCreateStat.Parameters["@l_df"].Value = sqlReader.Parse<uint>("l_df");
                                sqlCommandCreateStat.Parameters["@w_sv_pt"].Value = sqlReader.Parse<uint>("w_svPt");
                                sqlCommandCreateStat.Parameters["@l_sv_pt"].Value = sqlReader.Parse<uint>("l_svPt");
                                sqlCommandCreateStat.Parameters["@w_1st_in"].Value = sqlReader.Parse<uint>("w_1stIn");
                                sqlCommandCreateStat.Parameters["@l_1st_in"].Value = sqlReader.Parse<uint>("l_1stIn");
                                sqlCommandCreateStat.Parameters["@w_1st_won"].Value = sqlReader.Parse<uint>("w_1stWon");
                                sqlCommandCreateStat.Parameters["@l_1st_won"].Value = sqlReader.Parse<uint>("l_1stWon");
                                sqlCommandCreateStat.Parameters["@w_2nd_won"].Value = sqlReader.Parse<uint>("w_2ndWon");
                                sqlCommandCreateStat.Parameters["@l_2nd_won"].Value = sqlReader.Parse<uint>("l_2ndWon");
                                sqlCommandCreateStat.Parameters["@w_sv_gms"].Value = sqlReader.Parse<uint>("w_svGms");
                                sqlCommandCreateStat.Parameters["@l_sv_gms"].Value = sqlReader.Parse<uint>("l_svGms");
                                sqlCommandCreateStat.Parameters["@w_bp_saved"].Value = sqlReader.Parse<uint>("w_bpSaved");
                                sqlCommandCreateStat.Parameters["@l_bp_saved"].Value = sqlReader.Parse<uint>("l_bpSaved");
                                sqlCommandCreateStat.Parameters["@w_bp_faced"].Value = sqlReader.Parse<uint>("w_bpFaced");
                                sqlCommandCreateStat.Parameters["@l_bp_faced"].Value = sqlReader.Parse<uint>("l_bpFaced");
                                sqlCommandCreateStat.ExecuteNonQuery();

                                #endregion

                                // Updates source_matches
                                sqlCommandUpdateDateProcessed.Parameters["@id"].Value = sqlReader["id"];
                                sqlCommandUpdateDateProcessed.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            private static void ParseScore(string score, out List<List<uint?>> result, out string superTb)
            {
                result = null;
                superTb = null;

                if (string.IsNullOrWhiteSpace(score))
                {
                    return;
                }

                if (score.EndsWith("]"))
                {
                    score = score.Substring(0, score.Length - 1);
                    var posOfSuperTbBegin = score.LastIndexOf("[");
                    if (posOfSuperTbBegin > -1)
                    {
                        superTb = score.Substring(posOfSuperTbBegin + 1);
                        score = score.Substring(0, posOfSuperTbBegin);
                    }
                }

                int setNo = 1;
                var sets = score.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string set in sets)
                {
                    var elementsOfSet = set.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                    if (elementsOfSet.Length == 2)
                    {
                        uint? wSet = null;
                        uint? lSet = null;
                        uint? tb = null;

                        if (uint.TryParse(elementsOfSet[0], out uint wSetReal))
                        {
                            wSet = wSetReal;
                        }
                        else
                        {
                            // invalid set
                            break;
                        }

                        if (elementsOfSet[1].EndsWith(")"))
                        {
                            elementsOfSet[1] = elementsOfSet[1].Substring(0, elementsOfSet[1].Length - 1);
                            var posOfTbBegin = elementsOfSet[1].IndexOf("(");
                            if (posOfTbBegin > -1)
                            {
                                if (uint.TryParse(elementsOfSet[1].Substring(posOfTbBegin + 1), out uint tbReal))
                                {
                                    tb = tbReal;
                                }
                                else
                                {
                                    // invalid set
                                    break;
                                }
                                elementsOfSet[1] = elementsOfSet[1].Substring(0, posOfTbBegin);
                            }
                            else
                            {
                                // invalid set
                                break;
                            }
                        }

                        if (uint.TryParse(elementsOfSet[1], out uint lSetReal))
                        {
                            lSet = lSetReal;
                        }
                        else
                        {
                            // invalid set
                            break;
                        }

                        if (result == null)
                        {
                            result = new List<List<uint?>>();
                        }
                        result.Add(new List<uint?> { wSet, lSet, tb });

                        setNo++;
                    }
                    else
                    {
                        // invalid set
                        break;
                    }
                }

                if (result != null && result.Count < 5)
                {
                    while (result.Count < 5)
                    {
                        result.Add(new List<uint?> { null, null, null });
                    }
                }
            }

            private DateTime ComputeEditionDateEnd(string dateBeginString, int matchCountReal)
            {
                DateTime dateBegin = DateTime.ParseExact(dateBeginString, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);

                var authorizedCounts = new Dictionary<DayOfWeek, int>
                {
                    { DayOfWeek.Sunday, 1 },
                    { DayOfWeek.Saturday, 3 },
                    { DayOfWeek.Friday, 7 },
                    { DayOfWeek.Thursday, 15 },
                    { DayOfWeek.Wednesday, 31 },
                    { DayOfWeek.Tuesday, 63 },
                    { DayOfWeek.Monday, 127 },
                };
                
                int matchCountTheoretical = matchCountReal > authorizedCounts.Values.Last() ? authorizedCounts.Values.Last() :
                    authorizedCounts.Values.First(me => me >= matchCountReal);
                var dayOfWeekBegin = dateBegin.DayOfWeek;

                int countWeeks = 0;
                switch (dayOfWeekBegin)
                {
                    case DayOfWeek.Sunday:
                        if (matchCountTheoretical > 63)
                        {
                            countWeeks++;
                        }
                        if (matchCountTheoretical > authorizedCounts[dayOfWeekBegin])
                        {
                            countWeeks++;
                        }
                        break;
                    case DayOfWeek.Monday:
                        if (matchCountTheoretical > 63)
                        {
                            countWeeks++;
                        }
                        break;
                    default:
                        if (matchCountTheoretical > authorizedCounts[dayOfWeekBegin])
                        {
                            countWeeks++;
                        }
                        break;
                }

                var dateEndComputed = dateBegin.AddDays(7 * countWeeks);
                while (dateEndComputed.DayOfWeek != DayOfWeek.Sunday)
                {
                    dateEndComputed = dateEndComputed.AddDays(1);
                }

                return dateEndComputed;
            }

            /// <summary>
            /// Generates an ATP ranking with the specified ruleset.
            /// </summary>
            /// <param name="versionId">Version identifier.</param>
            public void GenerateAtpRanking(uint versionId)
            {
                Default.LoadModel();

                var atpRankingVersion = AtpRankingVersionPivot.Get(versionId);
                if (atpRankingVersion == null)
                {
                    throw new ArgumentException(Messages.RankingRulesetNotFoundException, nameof(versionId));
                }

                // Gets the latest monday with a computed ranking.
                var startDate = MySqlTools.ExecuteScalar(_connectionString,
                    "SELECT MAX(date) FROM atp_ranking WHERE version_id = @version",
                    new DateTime(1968, 1, 1),
                    new MySqlParameter("@version", MySqlDbType.UInt32)
                    {
                        Value = versionId
                    });
                // Monday one day after the latest tournament played (always a sunday).
                var dateStop = (EditionPivot.GetLatestsEditionDateEnding() ?? startDate).AddDays(1);

                // Loads matches from the previous year.
                Default.LoadMatches((uint)startDate.Year - 1);

                using (var sqlConnection = new MySqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandText = MySqlTools.GetSqlInsertStatement("atp_ranking", new List<string>
                        {
                            "player_id", "date", "points", "ranking", "version_id", "editions"
                        });
                        sqlCommand.Parameters.Add("@player_id", MySqlDbType.UInt32);
                        sqlCommand.Parameters.Add("@date", MySqlDbType.DateTime);
                        sqlCommand.Parameters.Add("@points", MySqlDbType.UInt32);
                        sqlCommand.Parameters.Add("@ranking", MySqlDbType.UInt32);
                        sqlCommand.Parameters.Add("@version_id", MySqlDbType.UInt32);
                        sqlCommand.Parameters.Add("@editions", MySqlDbType.UInt32);
                        sqlCommand.Prepare();

                        // Static.
                        sqlCommand.Parameters["@version_id"].Value = versionId;

                        // Puts in cache the triplet player/edition/points, no need to recompute each week.
                        var cachePlayerEditionPoints = new Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint>();

                        // Performances watcher.
                        DateTime dateBeginTreatment = DateTime.Now;

                        // For each week until latest date.
                        startDate = startDate.AddDays(7);
                        while (startDate <= dateStop)
                        {
                            // Loads matches from the current year (do nothing if already done).
                            Default.LoadMatches((uint)startDate.Year);

                            var playersRankedThisWeek = ComputePointsForPlayersInvolvedAtDate(atpRankingVersion, startDate, cachePlayerEditionPoints);

                            // Static for each player.
                            sqlCommand.Parameters["@date"].Value = startDate;

                            // Inserts each player.
                            int rank = 1;
                            foreach (var player in playersRankedThisWeek.Keys)
                            {
                                sqlCommand.Parameters["@player_id"].Value = player.Id;
                                sqlCommand.Parameters["@points"].Value = playersRankedThisWeek[player].Item1;
                                sqlCommand.Parameters["@editions"].Value = playersRankedThisWeek[player].Item2;
                                sqlCommand.Parameters["@ranking"].Value = rank;
                                sqlCommand.ExecuteNonQuery();
                                rank++;
                            }

                            startDate = startDate.AddDays(7);

                            // Performances watcher
                            System.Diagnostics.Debug.WriteLine($"Elapsed for one week : {(DateTime.Now - dateBeginTreatment).TotalSeconds}.");
                            dateBeginTreatment = DateTime.Now;
                        }
                    }
                }
            }

            private static Dictionary<PlayerPivot, Tuple<uint, uint>> ComputePointsForPlayersInvolvedAtDate(AtpRankingVersionPivot atpRankingVersion,
                DateTime startDate, Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint> cachePlayerEditionPoints)
            {
                // Collection of players to insert for the current week.
                // Key is the player, Value is number of points and editions played count.
                var playersRankedThisWeek = new Dictionary<PlayerPivot, Tuple<uint, uint>>();

                // Editions in one year rolling to the current date.
                var editionsRollingYear = EditionPivot.EditionsForAtpRankingAtDate(atpRankingVersion, startDate,
                    out IReadOnlyCollection<PlayerPivot> playersInvolved);

                // Computes infos for each player involved at the current date.
                foreach (var player in playersInvolved)
                {
                    var pointsAndCount = ComputePointsAndCountForPlayer(atpRankingVersion, player, editionsRollingYear, cachePlayerEditionPoints);

                    playersRankedThisWeek.Add(player, pointsAndCount);
                }

                // Sorts each player by descending points.
                // Then by editions played count (the fewer the better).
                playersRankedThisWeek =
                    playersRankedThisWeek
                        .OrderByDescending(me => me.Value.Item1)
                        .ThenBy(me => me.Value.Item2)
                        .ToDictionary(me => me.Key, me => me.Value);

                return playersRankedThisWeek;
            }

            /// <summary>
            /// Computes points and editions played count for a specified player.
            /// </summary>
            /// <param name="atpRankingVersion"><see cref="AtpRankingVersionPivot"/></param>
            /// <param name="player"><see cref="PlayerPivot"/></param>
            /// <param name="editionsRollingYear">Collection of <see cref="EditionPivot"/>.</param>
            /// <param name="cachePlayerEditionPoints">Cache of tuple player/edition with points already computed.</param>
            /// <returns>Points gained by the player for specified editions, and number of editions played.</returns>
            internal static Tuple<uint, uint> ComputePointsAndCountForPlayer(
                AtpRankingVersionPivot atpRankingVersion,
                PlayerPivot player,
                IReadOnlyCollection<EditionPivot> editionsRollingYear,
                Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint> cachePlayerEditionPoints)
            {
                // Editions the player has played
                var involvedEditions = editionsRollingYear.Where(me => me.InvolvePlayer(player)).ToList();

                // Computes points by edition involved.
                var pointsByEdition = new Dictionary<EditionPivot, uint>();
                foreach (var involvedEdition in involvedEditions)
                {
                    var cacheKey = new KeyValuePair<PlayerPivot, EditionPivot>(player, involvedEdition);
                    if (!cachePlayerEditionPoints.ContainsKey(cacheKey))
                    {
                        cachePlayerEditionPoints.Add(cacheKey, involvedEdition.GetPlayerPoints(player, atpRankingVersion));
                    }

                    pointsByEdition.Add(involvedEdition, cachePlayerEditionPoints[cacheKey]);
                }

                // Takes mandatories editions
                uint points = (uint)pointsByEdition
                                    .Where(me => me.Key.MandatoryAtp)
                                    .Sum(me => me.Value);

                // Then 6 best performances (or everything is the rule doesn't apply).
                points += (uint)pointsByEdition
                                .Where(me => !me.Key.MandatoryAtp)
                                .OrderByDescending(me => me.Value)
                                .Take(atpRankingVersion.Rules.Contains(AtpRankingRulePivot.SixBestPerformancesOnly) ? 6 : pointsByEdition.Count)
                                .Sum(me => me.Value);

                return new Tuple<uint, uint>(points, (uint)involvedEditions.Count);
            }
        }
    }
}
