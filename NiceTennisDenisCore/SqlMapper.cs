using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using NiceTennisDenisCore.Models;

namespace NiceTennisDenisCore
{
    /// <summary>
    /// Sql mapper.
    /// </summary>
    internal static class SqlMapper
    {
        private static bool _modelAtpIsLoaded = false;
        private static bool _modelWtaIsLoaded = false;
        private static readonly List<Tuple<uint, bool>> _matchesAtpByYearLoaded = new List<Tuple<uint, bool>>();
        private static readonly List<Tuple<uint, bool>> _matchesWtaByYearLoaded = new List<Tuple<uint, bool>>();
        private static readonly Dictionary<KeyValuePair<uint, DateTime>, IEnumerable<RankingPivot>> _rankingAtpCache =
            new Dictionary<KeyValuePair<uint, DateTime>, IEnumerable<RankingPivot>>();
        private static readonly Dictionary<KeyValuePair<uint, DateTime>, IEnumerable<RankingPivot>> _rankingWtaCache =
            new Dictionary<KeyValuePair<uint, DateTime>, IEnumerable<RankingPivot>>();

        /// <summary>
        /// Loads the full model, except <see cref="MatchPivot"/>.
        /// </summary>
        /// <remarks>Does nothing if the model is already loaded.</remarks>
        internal static void LoadModel()
        {
            if (!ModelIsLoaded())
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

                if (GlobalAppConfig.IsWtaContext)
                {
                    _modelWtaIsLoaded = true;
                }
                else
                {
                    _modelAtpIsLoaded = true;
                }
            }
        }

        private static bool ModelIsLoaded()
        {
            return (GlobalAppConfig.IsWtaContext && _modelWtaIsLoaded) || (!GlobalAppConfig.IsWtaContext && _modelAtpIsLoaded);
        }

        /// <summary>
        /// Loads every matches of a specified year.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <param name="finalOnly">Optionnal; final only y/n.</param>
        /// <remarks>
        /// Does nothing if the model isn't loaded.
        /// NEVER loads twice the same match.
        /// </remarks>
        internal static void LoadMatches(uint year, bool finalOnly = false)
        {
            var cacheToConsider = GlobalAppConfig.IsWtaContext ? _matchesWtaByYearLoaded : _matchesAtpByYearLoaded;

            if (ModelIsLoaded() && (!cacheToConsider.Any(me => me.Item1 == year && me.Item2 == finalOnly)))
            {
                var queryBuilder = new StringBuilder();
                queryBuilder.AppendLine("SELECT *");
                queryBuilder.AppendLine("FROM match_general AS mg");
                queryBuilder.AppendLine("JOIN match_stat AS mst ON mg.id = mst.match_id");
                queryBuilder.AppendLine("JOIN match_score AS msc ON mg.id = msc.match_id");
                queryBuilder.AppendLine("WHERE edition_id IN (SELECT id FROM edition WHERE year = @year)");
                if (finalOnly)
                {
                    queryBuilder.AppendLine("AND mg.round_id = 1");
                }
                else if (cacheToConsider.Any(me => me.Item1 == year && me.Item2))
                {
                    queryBuilder.AppendLine("AND mg.round_id != 1");
                }

                LoadPivotTypeWithQuery(queryBuilder.ToString(), MatchPivot.Create, new MySqlParameter("@year", MySqlDbType.UInt32)
                {
                    Value = year
                });

                cacheToConsider.Add(new Tuple<uint, bool>(year, finalOnly));
            }
        }

        /// <summary>
        /// Loads the ranking at a specified date.
        /// </summary>
        /// <param name="versionId"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="date">Ranking date. If not a monday, takes the previous monday.</param>
        /// <param name="top">maximal number of results returned.</param>
        /// <returns>Ranking at date, sorted by ranking position. <c>Null</c></returns>
        internal static IReadOnlyCollection<RankingPivot> LoadRankingAtDate(uint versionId, DateTime date, uint top)
        {
            date = date.DayOfWeek == DayOfWeek.Monday ? date :
                (date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-6) : date.AddDays(-((int)date.DayOfWeek - 1)));

            var key = new KeyValuePair<uint, DateTime>(versionId, date);

            if ((GlobalAppConfig.IsWtaContext && !_rankingWtaCache.ContainsKey(key)) || (!GlobalAppConfig.IsWtaContext && !_rankingAtpCache.ContainsKey(key)))
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

                if (GlobalAppConfig.IsWtaContext)
                {
                    _rankingWtaCache.Add(key, rankings);
                }
                else
                {
                    _rankingAtpCache.Add(key, rankings);
                }
            }

            return (GlobalAppConfig.IsWtaContext ? _rankingWtaCache[key] : _rankingAtpCache[key]).ToList();
        }

        private static void LoadConfiguration()
        {
            using (var sqlConnection = new MySqlConnection(GlobalAppConfig.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = "SELECT * FROM configuration WHERE id = @id";
                    sqlCommand.Parameters.Add(new MySqlParameter("@id", MySqlDbType.UInt32)
                    {
                        Value = GlobalAppConfig.GetInt32(AppKey.ConfigurationId)
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

        private static IEnumerable<T> LoadPivotType<T>(string table, Func<MySqlDataReader, object[], T> action)
        {
            return LoadPivotTypeWithQuery($"select * from {table}", action);
        }

        private static IEnumerable<T> LoadPivotTypeWithQuery<T>(string query, Func<MySqlDataReader, object[], T> action, params MySqlParameter[] parameters)
        {
            List<T> listofT = new List<T>();

            using (var sqlConnection = new MySqlConnection(GlobalAppConfig.GetConnectionString()))
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
                            listofT.Add(action.Invoke(sqlReader, new object[] { GlobalAppConfig.GetProfilePictureBaseDirectory() }));
                        }
                    }
                }
            }

            return listofT;
        }
    }
}
