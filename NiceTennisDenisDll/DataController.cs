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
    public class DataController
    {
        private readonly string _datasDirectory;
        private readonly uint _configurationId;
        private readonly string _connectionString;
        private readonly string _pathToProfilePictureBase;
        private readonly bool _isWta;
        private readonly List<uint> _matchesByYearLoaded = new List<uint>();
        private readonly Dictionary<KeyValuePair<uint, DateTime>, IEnumerable<RankingPivot>> _rankingCache =
            new Dictionary<KeyValuePair<uint, DateTime>, IEnumerable<RankingPivot>>();

        private bool _modelIsLoaded = false;

        private static DataController _default = null;

        /// <summary>
        /// Initialize singleton instance.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="datasDirectory">Datas directory.</param>
        /// <param name="isWta"><c>True</c> for WTA management; <c>False</c> for ATP management.</param>
        /// <param name="configurationId">Configuration identifier.</param>
        /// <returns>Initialized instance.</returns>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidConnectionStringException"/></exception>
        public static DataController InitializeDefault(string connectionString, string datasDirectory, bool isWta, uint configurationId)
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

            _default = new DataController(connectionString, datasDirectory, isWta, configurationId);
            return _default;
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Messages.NotInitializedInstanceException"/></exception>
        public static DataController Default
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

        private DataController(string connectionString, string datasDirectory, bool isWta, uint configurationId)
        {
            _connectionString = connectionString;
            _datasDirectory = datasDirectory;
            _isWta = isWta;
            _configurationId = configurationId;
            _pathToProfilePictureBase = Path.Combine(_datasDirectory, "profiles", _isWta ? "wta" : "atp");
        }

        /// <summary>
        /// Loads the full model, except <see cref="MatchPivot"/>.
        /// </summary>
        /// <remarks>Does nothing if the model is already loaded.</remarks>
        public void LoadModel()
        {
            if (!_modelIsLoaded)
            {
                LoadConfiguration();

                LoadPivotType("tournament", TournamentPivot.Create);
                LoadPivotType("level", LevelPivot.Create);
                LoadPivotType("round", RoundPivot.Create);
                LoadPivotType("entry", EntryPivot.Create);
                LoadPivotType("slot", SlotPivot.Create);
                LoadPivotType("edition", EditionPivot.Create);
                LoadPivotType("player", PlayerPivot.Create);
                LoadPivotType("grid_point", GridPointPivot.Create);
                LoadPivotType("qualification_point", QualificationPointPivot.Create);

                var sqlQuery = new StringBuilder();
                sqlQuery.AppendLine("select id, creation_date, group_concat(rule_id) as rules_concat");
                sqlQuery.AppendLine("from ranking_version");
                sqlQuery.AppendLine("left join ranking_version_rule on id = version_id");
                sqlQuery.AppendLine("group by id, creation_date");
                LoadPivotTypeWithQuery(sqlQuery.ToString(), RankingVersionPivot.Create);

                _modelIsLoaded = true;
            }
        }

        private void LoadConfiguration()
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = "SELECT * FROM configuration WHERE id = @id";
                    sqlCommand.Parameters.Add(new MySqlParameter("@id", MySqlDbType.UInt32)
                    {
                        Value = _configurationId
                    });
                    using (var sqlReader = sqlCommand.ExecuteReader())
                    {
                        if (sqlReader.Read())
                        {
                            ConfigurationPivot.Initialize(sqlReader);
                        }
                    }
                }
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

        private IEnumerable<T> LoadPivotType<T>(string table, Func<MySqlDataReader, object[], T> action)
        {
            return LoadPivotTypeWithQuery($"select * from {table}", action);
        }

        private IEnumerable<T> LoadPivotTypeWithQuery<T>(string query, Func<MySqlDataReader, object[], T> action, params MySqlParameter[] parameters)
        {
            List<T> listofT = new List<T>();

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
                            listofT.Add(action.Invoke(sqlReader, new object[] { _pathToProfilePictureBase }));
                        }
                    }
                }
            }

            return listofT;
        }

        /// <summary>
        /// Generates a ranking for the specified ruleset.
        /// </summary>
        /// <param name="versionId"><see cref="RankingVersionPivot"/> (ruleset) identifier.</param>
        public void GenerateRanking(uint versionId)
        {
            LoadModel();

            var rankingVersion = RankingVersionPivot.Get(versionId);
            if (rankingVersion == null)
            {
                throw new ArgumentException(Messages.RankingRulesetNotFoundException, nameof(versionId));
            }

            // Gets the latest monday with a computed ranking.
            var startDate = MySqlTools.ExecuteScalar(_connectionString,
                "SELECT MAX(date) FROM ranking WHERE version_id = @version",
                RankingVersionPivot.OPEN_ERA_BEGIN,
                new MySqlParameter("@version", MySqlDbType.UInt32)
                {
                    Value = versionId
                });
            // Monday one day after the latest tournament played (always a sunday).
            var dateStop = (EditionPivot.GetLatestsEditionDateEnding() ?? startDate).AddDays(1);

            // Loads matches from the previous year.
            LoadMatches((uint)startDate.Year - 1);

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = MySqlTools.GetSqlInsertStatement("ranking", new List<string>
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

                    // For each week until latest date.
                    startDate = startDate.AddDays(7);
                    while (startDate <= dateStop)
                    {
                        // Loads matches from the current year (do nothing if already done).
                        LoadMatches((uint)startDate.Year);

                        var playersRankedThisWeek = rankingVersion.ComputePointsForPlayersInvolvedAtDate(startDate, cachePlayerEditionPoints);

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
                    }
                }
            }
        }

        /// <summary>
        /// Debugs ranking calculation.
        /// </summary>
        /// <param name="playerId"><see cref="PlayerPivot"/> identifier.</param>
        /// <param name="versionId"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="dateEnd">Ranking date to debug.</param>
        /// <returns>Points count and editions played count.</returns>
        public Tuple<uint, uint> DebugRankingForPlayer(uint playerId, uint versionId, DateTime dateEnd)
        {
            LoadModel();

            var rankingVersion = RankingVersionPivot.Get(versionId);
            if (rankingVersion == null)
            {
                throw new ArgumentException(Messages.RankingRulesetNotFoundException, nameof(versionId));
            }

            var player = PlayerPivot.Get(playerId);
            if (player == null)
            {
                return null;
            }

            // Ensures monday.
            while (dateEnd.DayOfWeek != DayOfWeek.Monday)
            {
                dateEnd = dateEnd.AddDays(1);
            }

            LoadMatches((uint)(dateEnd.Year - 1));
            LoadMatches((uint)dateEnd.Year);

            return rankingVersion.DebugRankingForPlayer(player, dateEnd);
        }

        /// <summary>
        /// Gets the ranking at a specified date.
        /// </summary>
        /// <param name="versionId"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="date">Ranking date. If not a monday, takes the previous monday.</param>
        /// <param name="top">maximal number of results returned.</param>
        /// <returns>Ranking at date, sorted by ranking position. <c>Null</c></returns>
        public IReadOnlyCollection<RankingPivot> GetRankingAtDate(uint versionId, DateTime date, uint top)
        {
            LoadModel();

            date = date.DayOfWeek == DayOfWeek.Monday ? date :
                (date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-6) : date.AddDays(-((int)date.DayOfWeek - 1)));

            var key = new KeyValuePair<uint, DateTime>(versionId, date);

            if (!_rankingCache.ContainsKey(key))
            {
                var sqlQuery = new StringBuilder();
                sqlQuery.AppendLine("SELECT * FROM ranking");
                sqlQuery.AppendLine("WHERE version_id = @version AND date = @date");
                sqlQuery.AppendLine("ORDER BY ranking ASC");
                sqlQuery.AppendLine($"LIMIT 0, {top}");

                var rankings = LoadPivotTypeWithQuery(sqlQuery.ToString(),
                    RankingPivot.Create,
                    new MySqlParameter("@version", MySqlDbType.UInt32) { Value = versionId },
                    new MySqlParameter("@date", MySqlDbType.DateTime) { Value = date });

                _rankingCache.Add(key, rankings);
            }

            return _rankingCache[key].ToList();
        }
    }
}
