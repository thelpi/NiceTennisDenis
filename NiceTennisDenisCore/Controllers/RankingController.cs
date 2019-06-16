using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using NiceTennisDenisCore.Models;

namespace NiceTennisDenisCore.Controllers
{
    /// <summary>
    /// <see cref="RankingVersionPivot"/> controller. Allows to get and generate rankings.
    /// </summary>
    /// <seealso cref="Controller"/>
    [Produces("application/json")]
    [Route("api/Ranking")]
    public class RankingController : Controller
    {
        /// <summary>
        /// Gets every Wta ranking version.
        /// </summary>
        /// <returns>Collection of <see cref="RankingVersionPivot"/>.</returns>
        [HttpGet("wta")]
        public IReadOnlyCollection<RankingVersionPivot> GetWtaRankingVersion()
        {
            GlobalAppConfig.IsWtaContext = true;
            return RankingVersionPivot.GetList();
        }

        /// <summary>
        /// Gets every Atp ranking version.
        /// </summary>
        /// <returns>Collection of <see cref="RankingVersionPivot"/>.</returns>
        [HttpGet("atp")]
        public IReadOnlyCollection<RankingVersionPivot> GetAtpRankingVersion()
        {
            GlobalAppConfig.IsWtaContext = false;
            return RankingVersionPivot.GetList();
        }

        /// <summary>
        /// Generates a Wta ranking for the specified ruleset.
        /// </summary>
        /// <param name="id"><see cref="RankingVersionPivot"/> identifier.</param>
        [HttpPost("wta/{id}")]
        public void GenerateWtaRanking(uint id)
        {
            GlobalAppConfig.IsWtaContext = true;
            GenerateRanking(id);
        }

        /// <summary>
        /// Generates an Atp ranking for the specified ruleset.
        /// </summary>
        /// <param name="id"><see cref="RankingVersionPivot"/> identifier.</param>
        [HttpPost("atp/{id}")]
        public void GenerateAtpRanking(uint id)
        {
            GlobalAppConfig.IsWtaContext = false;
            GenerateRanking(id);
        }

        private void GenerateRanking(uint id)
        {
            var rankingVersion = RankingVersionPivot.Get(id);
            if (rankingVersion == null)
            {
                throw new ArgumentException(Messages.RankingRulesetNotFoundException, nameof(id));
            }

            // Gets the latest monday with a computed ranking.
            var startDate = MySqlTools.ExecuteScalar(GlobalAppConfig.GetConnectionString(),
                "SELECT MAX(date) FROM ranking WHERE version_id = @version",
                RankingVersionPivot.OPEN_ERA_BEGIN,
                new MySqlParameter("@version", MySqlDbType.UInt32)
                {
                    Value = id
                });
            // Monday one day after the latest tournament played (always a sunday).
            var dateStop = (EditionPivot.GetLatestsEditionDateEnding() ?? startDate).AddDays(1);

            // Loads matches from the previous year.
            SqlMapper.LoadMatches((uint)startDate.Year - 1);

            using (var sqlConnection = new MySqlConnection(GlobalAppConfig.GetConnectionString()))
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
                    sqlCommand.Parameters["@version_id"].Value = id;

                    // Puts in cache the triplet player/edition/points, no need to recompute each week.
                    var cachePlayerEditionPoints = new Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint>();

                    // For each week until latest date.
                    startDate = startDate.AddDays(7);
                    while (startDate <= dateStop)
                    {
                        // Loads matches from the current year (do nothing if already done).
                        SqlMapper.LoadMatches((uint)startDate.Year);

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
        /// Debugs Wta ranking calculation.
        /// </summary>
        /// <param name="id"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="playerId"><see cref="PlayerPivot"/> identifier.</param>
        /// <param name="date">Ranking date to debug.</param>
        /// <returns>Points count and editions played count.</returns>
        [HttpGet("wta/debug/{id}/{date}/{playerId}")]
        public Tuple<uint, uint> DebugWtaRankingForPlayer(uint id, string date, uint playerId)
        {
            GlobalAppConfig.IsWtaContext = true;
            return DebugRankingForPlayer(id, playerId, date);
        }

        /// <summary>
        /// Debugs Atp ranking calculation.
        /// </summary>
        /// <param name="id"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="playerId"><see cref="PlayerPivot"/> identifier.</param>
        /// <param name="date">Ranking date to debug.</param>
        /// <returns>Points count and editions played count.</returns>
        [HttpGet("atp/debug/{id}/{date}/{playerId}")]
        public Tuple<uint, uint> DebugAtpRankingForPlayer(uint id, string date, uint playerId)
        {
            GlobalAppConfig.IsWtaContext = false;
            return DebugRankingForPlayer(id, playerId, date);
        }

        private Tuple<uint, uint> DebugRankingForPlayer(uint id, uint playerId, string date)
        {
            if (!DateTime.TryParse(date, out DateTime realDateEnd))
            {
                throw new ArgumentException(Messages.InvalidInputDateException, nameof(id));
            }

            var rankingVersion = RankingVersionPivot.Get(id);
            if (rankingVersion == null)
            {
                throw new ArgumentException(Messages.RankingRulesetNotFoundException, nameof(id));
            }

            var player = PlayerPivot.Get(playerId);
            if (player == null)
            {
                return null;
            }

            // Ensures monday.
            while (realDateEnd.DayOfWeek != DayOfWeek.Monday)
            {
                realDateEnd = realDateEnd.AddDays(1);
            }

            SqlMapper.LoadMatches((uint)(realDateEnd.Year - 1));
            SqlMapper.LoadMatches((uint)realDateEnd.Year);

            return rankingVersion.DebugRankingForPlayer(player, realDateEnd);
        }

        /// <summary>
        /// Gets an Atp ranking at the specified date.
        /// </summary>
        /// <param name="id"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="date">Ranking date.</param>
        /// <param name="top">Maximal results count.</param>
        /// <returns>Sorted list of players ranked.</returns>
        [HttpGet("atp/{id}/{date}/{top}")]
        public IReadOnlyCollection<RankingPivot> GetAtpRankingAtDate(uint id, DateTime date, uint top)
        {
            GlobalAppConfig.IsWtaContext = false;
            return SqlMapper.LoadRankingAtDate(id, date, top);
        }

        /// <summary>
        /// Gets a Wta ranking at the specified date.
        /// </summary>
        /// <param name="id"><see cref="RankingVersionPivot"/> identifier.</param>
        /// <param name="date">Ranking date.</param>
        /// <param name="top">Maximal results count.</param>
        /// <returns>Sorted list of players ranked.</returns>
        [HttpGet("wta/{id}/{date}/{top}")]
        public IReadOnlyCollection<RankingPivot> GetWtaRankingAtDate(uint id, DateTime date, uint top)
        {
            GlobalAppConfig.IsWtaContext = true;
            return SqlMapper.LoadRankingAtDate(id, date, top);
        }
    }
}