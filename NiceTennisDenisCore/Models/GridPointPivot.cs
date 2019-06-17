using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents the scale of ranking points.
    /// </summary>
    /// <remarks><see cref="BasePivot.Id"/> should be ignored.</remarks>
    /// <seealso cref="BasePivot"/>
    public sealed class GridPointPivot : BasePivot
    {
        #region Public properties

        /// <summary>
        /// <see cref="LevelPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public LevelPivot Level { get; private set; }
        /// <summary>
        /// <see cref="RoundPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public RoundPivot Round { get; private set; }
        /// <summary>
        /// Points.
        /// </summary>
        public uint Points { get; private set; }
        /// <summary>
        /// Points gained for a lose at the specified round.
        /// </summary>
        public uint ParticipationPoints { get; private set; }

        #endregion

        private GridPointPivot(uint levelId, uint roundId, uint points, uint participationPoints) : base(0, null, null)
        {
            Level = LevelPivot.Get(levelId);
            Round = RoundPivot.Get(roundId);
            Points = points;
            ParticipationPoints = participationPoints;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates a <see cref="GridPointPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="GridPointPivot"/>.</returns>
        internal static GridPointPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            return new GridPointPivot(reader.Get<uint>("level_id"),
                reader.Get<uint>("round_id"),
                reader.Get<uint>("points"),
                reader.Get<uint>("participation_points"));
        }

        /// <summary>
        /// Gets a <see cref="GridPointPivot"/> by its level and round.
        /// </summary>
        /// <param name="levelId"><see cref="LevelPivot"/> identifier.</param>
        /// <param name="roundId"><see cref="RoundPivot"/> identifier.</param>
        /// <returns><see cref="GridPointPivot"/>; <c>Null</c> if not found.</returns>
        internal static GridPointPivot GetByLevelAndRound(uint levelId, uint roundId)
        {
            return GetList<GridPointPivot>().FirstOrDefault(gridPoint =>
                gridPoint.Level.Id == levelId && gridPoint.Round.Id == roundId);
        }

        /// <summary>
        /// Collection of <see cref="LevelPivot"/> which can be used to compute the specified <see cref="RankingVersionPivot"/>.
        /// </summary>
        internal static List<LevelPivot> GetRankableLevelList(RankingVersionPivot rankingVersion)
        {
            return GetList<GridPointPivot>()
                .Select(gridPoint => gridPoint.Level)
                .Distinct()
                .Where(level => rankingVersion.ContainsRule(RankingRulePivot.IncludingOlympicGames) || !level.IsOlympicGames)
                .ToList();
        }
    }
}
