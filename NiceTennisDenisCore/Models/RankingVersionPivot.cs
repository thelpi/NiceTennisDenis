using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents a specific version of ranking.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class RankingVersionPivot : BasePivot
    {
        /// <summary>
        /// Begin date of Open era (first monday).
        /// </summary>
        internal static readonly DateTime OPEN_ERA_BEGIN = new DateTime(1968, 1, 1);

        #region Public properties

        /// <summary>
        /// Creation date.
        /// </summary>
        public DateTime CreationDate { get; private set; }
        /// <summary>
        /// Collection of <see cref="RankingRulePivot"/>.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public List<RankingRulePivot> Rules { get; private set; }

        #endregion

        private RankingVersionPivot(uint id, DateTime creationDate, IEnumerable<uint> ruleIdList) : base(id, null, null)
        {
            CreationDate = creationDate;
            Rules = ruleIdList.Select(me => (RankingRulePivot)me).ToList();
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Debugs ranking calculation for a specified player at specified date.
        /// </summary>
        /// <param name="player"><see cref="PlayerPivot"/></param>
        /// <param name="dateEnd">Ranking date to debug.</param>
        /// <returns>Points count and editions played count.</returns>
        internal Tuple<uint, uint> DebugRankingForPlayer(PlayerPivot player, DateTime dateEnd)
        {
            return ComputePointsAndCountForPlayer(player,
                EditionPivot.EditionsForRankingAtDate(this, dateEnd, out List<PlayerPivot> playersInvolved).ToList(),
                new Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint>());
        }

        /// <summary>
        /// Computes ranking points (and editions played count) for every players at a specified date.
        /// </summary>
        /// <param name="startDate">Ranking date.</param>
        /// <param name="cachePlayerEditionPoints">Cache of player / edition / point.</param>
        /// <returns>Points and editions played count, by player.</returns>
        internal Dictionary<PlayerPivot, Tuple<uint, uint>> ComputePointsForPlayersInvolvedAtDate(DateTime startDate,
            Dictionary<KeyValuePair<PlayerPivot, EditionPivot>, uint> cachePlayerEditionPoints)
        {
            // Collection of players to insert for the current week.
            // Key is the player, Value is number of points and editions played count.
            var playersRankedThisWeek = new Dictionary<PlayerPivot, Tuple<uint, uint>>();

            // Editions in one year rolling to the current date.
            var editionsRollingYear = EditionPivot.EditionsForRankingAtDate(this, startDate,
                out List<PlayerPivot> playersInvolved).ToList();

            // Computes infos for each player involved at the current date.
            foreach (var player in playersInvolved)
            {
                var pointsAndCount = ComputePointsAndCountForPlayer(player, editionsRollingYear, cachePlayerEditionPoints);

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

        private Tuple<uint, uint> ComputePointsAndCountForPlayer(
            PlayerPivot player,
            List<EditionPivot> editionsRollingYear,
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
                    cachePlayerEditionPoints.Add(cacheKey, involvedEdition.GetPlayerPoints(player, this));
                }

                pointsByEdition.Add(involvedEdition, cachePlayerEditionPoints[cacheKey]);
            }

            // Takes mandatories editions
            uint points = (uint)pointsByEdition
                                .Where(me => me.Key.Mandatory)
                                .Sum(me => me.Value);

            // Then best performances (or everything is the rule doesn't apply).
            points += (uint)pointsByEdition
                            .Where(me => !me.Key.Mandatory)
                            .OrderByDescending(me => me.Value)
                            .Take(ContainsRule(RankingRulePivot.BestPerformancesOnly) ?
                                (int)ConfigurationPivot.Default.BestPerformancesCountForRanking : pointsByEdition.Count)
                            .Sum(me => me.Value);

            return new Tuple<uint, uint>(points, (uint)involvedEditions.Count);
        }

        /// <summary>
        /// Checks if a specified ranking rule is applied to this ranking version.
        /// </summary>
        /// <param name="rule">The <see cref="RankingRulePivot"/> to check.</param>
        /// <returns><c>True</c> if contained; <c>False</c> otherwise.</returns>
        internal bool ContainsRule(RankingRulePivot rule)
        {
            return Rules.Contains(rule);
        }

        /// <summary>
        /// Creates a <see cref="RankingVersionPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="RankingVersionPivot"/>.</returns>
        internal static RankingVersionPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            return new RankingVersionPivot(reader.Get<uint>("id"),
                reader.Get<DateTime>("creation_date"),
                reader.IsDBNull("rules_concat") ?
                    new List<uint>() :
                    reader.GetString("rules_concat").Split(',').Select(me => Convert.ToUInt32(me)));
        }

        /// <summary>
        /// Gets an <see cref="RankingVersionPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="RankingVersionPivot"/>. <c>Null</c> if not found.</returns>
        internal static RankingVersionPivot Get(uint id)
        {
            return Get<RankingVersionPivot>(id);
        }

        /// <summary>
        /// Gets every instance of <see cref="RankingVersionPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="RankingVersionPivot"/>.</returns>
        internal static List<RankingVersionPivot> GetList()
        {
            return GetList<RankingVersionPivot>();
        }
    }
}
