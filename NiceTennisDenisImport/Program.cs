using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisImport
{
    static class Program
    {
        const string _datasDirectory = @"D:\Ma programmation\csharp\Projects\NiceTennisDenis\datas";
        const string _sourceFileFolderName = "tennis_atp-master"; // "tennis_wta-master"
        const string _matchesFileNamePattern = "atp_matches_{0}.csv"; // wta_matches_{0}.csv
        const string _connectionString = "Server=localhost;Database=nice_tennis_denis;Uid=root;Pwd=;"; // first_for_mugu
        const string _playersFileName = "atp_players.csv"; // "wta_players.csv"
        const int DEFAULT_STRING_COL_SIZE = 255;
        const string COLUMN_SEPARATOR = ",";
        const string COUNTRY_UNKNOWN = "UNK";

        static void Main(string[] args)
        {
            // 00-      check list of players file header
            // 00- ImportNewPlayers()
            // 01- ImportSingleMatchesFileInDatabase([year]);
            // 02-      Checklist (players section)
            // 03- CreatePendingPlayersFromSource()
            // 04- UpdatePlayersHeightFromMatchesSource()
            // 05-      Checklist (editions section)
            // 06- CreatePendingTournamentEditionsFromSource();
            // 07-      Checklist (matches)
            // 08- CreatePendingMatchesFromSource();
            // 09-      Creates or updates slot and tournament for each edition (if tournament exists, add the new code in known_codes)
            // 10-      "Next Gen Finals" => level 10
        }

        #region Main methods

        static void ImportNewPlayers()
        {
            string fullFileName = Path.Combine(_datasDirectory, _sourceFileFolderName, _playersFileName);
            if (!File.Exists(fullFileName))
            {
                throw new Exception("Players datas file not found.");
            }

            List<string> playersUnableToRemove = ComputeUnremovablePlayersList();

            ExtractPlayersColumnsHeadersAndValues(fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent);

            int idIndexOf = headerColumns.IndexOf("player_id");

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = GetSqlReplaceStatement("source_players", headerColumns);
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

        static void ImportSingleMatchesFileInDatabase(int year)
        {
            string fileName = string.Format(_matchesFileNamePattern, year);

            string fullFileName = Path.Combine(_datasDirectory, _sourceFileFolderName, fileName);
            if (!File.Exists(fullFileName))
            {
                throw new ArgumentException("No file found for the specified year in the data directory configured.", nameof(year));
            }

            ExtractMatchesColumnsHeadersAndValues(fileName, fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent);

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = GetSqlReplaceStatement("source_matches", headerColumns);
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

        static void CreatePendingPlayersFromSource()
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
                    sqlCommandUpd.CommandText = GetSqlInsertStatement("player", columnsHeaders);
                    sqlCommandUpd.Parameters.Add("@id", MySqlDbType.UInt32);
                    sqlCommandUpd.Parameters.Add("@first_name", MySqlDbType.String, 255);
                    sqlCommandUpd.Parameters.Add("@last_name", MySqlDbType.String, 255);
                    sqlCommandUpd.Parameters.Add("@hand", MySqlDbType.String, 1);
                    sqlCommandUpd.Parameters.Add("@birth_date", MySqlDbType.DateTime);
                    sqlCommandUpd.Parameters.Add("@country", MySqlDbType.String, 3);
                    sqlCommandUpd.Prepare();

                    var sqlQuery = new StringBuilder();
                    sqlQuery.AppendLine("select CONVERT(player_id, UNSIGNED) AS id, TRIM(name_first) AS first_name,");
                    sqlQuery.AppendLine("TRIM(name_list) AS last_name, IF(hand = 'U', NULL, hand) AS hand,");
                    sqlQuery.AppendLine("CONVERT(birthdate, DATETIME) AS birth_date,");
                    sqlQuery.AppendLine("IF(country='', '" + COUNTRY_UNKNOWN + "', country) AS country");
                    sqlQuery.AppendLine("FROM source_players");
                    sqlQuery.AppendLine("WHERE date_processed IS NULL");

                    sqlCommandRead.CommandText = sqlQuery.ToString();
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

        static void UpdatePlayersHeightFromMatchesSource()
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

        static void CreatePendingTournamentEditionsFromSource()
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
                    sqlCommandUpd.CommandText = GetSqlInsertStatement("edition", columnsHeaders);
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
                    getEditionsSql.AppendLine(" IFNULL((");
                    getEditionsSql.AppendLine("     SELECT indoor FROM edition");
                    getEditionsSql.AppendLine("     WHERE (year + 1) = SUBSTR(tourney_id, 1, 4) AND code = SUBSTR(tourney_id, 6, 255)");
                    getEditionsSql.AppendLine("     LIMIT 0, 1");
                    getEditionsSql.AppendLine(" ), 0) as indoor,");
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

        static void CreatePendingMatchesFromSource()
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
                    sqlCommandCreateGeneral.CommandText = GetSqlInsertStatement("match_general", genHeadersColumnsUint.Concat(genHeadersColumnsBool));
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
                    sqlCommandCreateScore.CommandText = GetSqlInsertStatement("match_score", scoreHeadersColumnsUint.Concat(scoreHeadersColumnsOther));
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
                    sqlCommandCreateStat.CommandText = GetSqlInsertStatement("match_stat", statHeadersColumns);
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

        #endregion

        static void ExtractMatchesColumnsHeadersAndValues(string fileName, string fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent)
        {
            // For WTA, lot of statistic informations are missing, so blank informations must be filled manually for those columns.
            // The columns count must be 49 overall.
            const int countColumnsTheoretical = 49;
            var columnsForced = new List<string>
                {
                    "minutes", "w_ace", "w_df", "w_svpt", "w_1stIn", "w_1stWon", "w_2ndWon", "w_SvGms", "w_bpSaved", "w_bpFaced",
                    "l_ace", "l_df", "l_svpt", "l_1stIn", "l_1stWon", "l_2ndWon", "l_SvGms", "l_bpSaved", "l_bpFaced"
                };

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
                        if (headerColumns.Count == countColumnsTheoretical - columnsForced.Count)
                        {
                            headerColumns.AddRange(columnsForced);
                        }
                    }
                    else
                    {
                        if (columnsList.Count == countColumnsTheoretical - columnsForced.Count)
                        {
                            columnsList.AddRange(columnsForced.Select(me => string.Empty));
                        }
                        if (columnsList.Count != headerColumns.Count)
                        {
                            throw new Exception("The columns count doesn't match the headers count.");
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
                throw new Exception("Invalid content for the specified matches file.");
            }
            headerColumns.Add("file_name");
            linesOfContent.ForEach(line => line.Add(fileName));
        }

        static void ExtractPlayersColumnsHeadersAndValues(string fullFileName, out List<string> headerColumns, out List<List<string>> linesOfContent)
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
                            throw new Exception("The columns count doesn't match the headers count.");
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
                throw new Exception("Invalid content for the players file.");
            }
        }

        static List<string> ComputeUnremovablePlayersList()
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

        static DateTime ComputeEditionDateEnd(string dateBeginString, int matchCountReal)
        {
            DateTime dateBegin;
            if (dateBeginString.All(ch => "0123456789".Contains(ch)))
            {
                dateBegin = DateTime.ParseExact(dateBeginString, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
            }
            else
            {
                dateBegin = DateTime.Parse(dateBeginString);
            }

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

        static void ParseScore(string score, out List<List<uint?>> result, out string superTb)
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

        #region SQL tools

        static string GetSqlReplaceStatement(string table, IEnumerable<string> headers)
        {
            return GetSqlInsertOrReplaceStatement(table, headers, true);
        }

        static string GetSqlInsertOrReplaceStatement(string table, IEnumerable<string> headers, bool replace)
        {
            string statementType = replace ? "REPLACE" : "INSERT";
            return $"{statementType} INTO {table} ({string.Join(", ", headers)}) " +
                $"VALUES ({string.Join(", ", headers.Select(hc => string.Concat("@", hc)))})";
        }

        static string GetSqlInsertStatement(string table, IEnumerable<string> headers)
        {
            return GetSqlInsertOrReplaceStatement(table, headers, false);
        }

        static bool IsDBNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName));
        }

        static T Get<T>(this MySqlDataReader reader, string columnName) where T : struct
        {
            return reader.IsDBNull(columnName) ? default(T) : (T)Convert.ChangeType(reader[columnName], typeof(T));
        }

        static object Parse<T>(this MySqlDataReader reader, string column)
        {
            return reader.Parse<T>(column, DBNull.Value);
        }

        static object Parse<T>(this MySqlDataReader reader, string column, object defaultValue)
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

        #endregion
    }
}
