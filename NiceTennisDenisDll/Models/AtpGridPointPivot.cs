using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents the scale of ATP ranking points.
    /// </summary>
    /// <remarks><see cref="BasePivot.Id"/> should be ignored.</remarks>
    /// <seealso cref="BasePivot"/>
    public sealed class AtpGridPointPivot : BasePivot
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

        private AtpGridPointPivot(uint levelId, uint roundId, uint points, uint participationPoints) : base(0, null, null)
        {
            Level = LevelPivot.Get(levelId);
            Round = RoundPivot.Get(roundId);
            Points = points;
            ParticipationPoints = participationPoints;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        #region Public methods

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Level.Name} - {Round.Name}";
        }

        #endregion

        /// <summary>
        /// Creates a <see cref="AtpGridPointPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="AtpGridPointPivot"/>.</returns>
        internal static AtpGridPointPivot Create(MySqlDataReader reader)
        {
            return new AtpGridPointPivot(reader.Get<uint>("level_id"),
                reader.Get<uint>("round_id"),
                reader.Get<uint>("points"),
                reader.Get<uint>("participation_points"));
        }

        #region Public static methods

        /// <summary>
        /// Gets a <see cref="AtpGridPointPivot"/> by its level and round.
        /// </summary>
        /// <param name="levelId"><see cref="LevelPivot"/> identifier.</param>
        /// <param name="roundId"><see cref="RoundPivot"/> identifier.</param>
        /// <returns><see cref="AtpGridPointPivot"/>; <c>Null</c> if not found.</returns>
        public static AtpGridPointPivot GetByLevelAndRound(uint levelId, uint roundId)
        {
            return GetList<AtpGridPointPivot>().FirstOrDefault(atpGridPoint =>
                atpGridPoint.Level.Id == levelId && atpGridPoint.Round.Id == roundId);
        }

        /// <summary>
        /// Gets every instance of <see cref="AtpGridPointPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="AtpGridPointPivot"/>.</returns>
        public static IReadOnlyCollection<AtpGridPointPivot> GetList()
        {
            return GetList<AtpGridPointPivot>().ToList();
        }

        /// <summary>
        /// Collection of <see cref="LevelPivot"/> which can be used to compute the specified <see cref="AtpRankingVersionPivot"/>.
        /// </summary>
        public static IReadOnlyCollection<LevelPivot> GetRankableLevelList(AtpRankingVersionPivot atpRankingVersion)
        {
            return GetList<AtpGridPointPivot>()
                .Select(atpGridPoint => atpGridPoint.Level)
                .Distinct()
                .Where(level => atpRankingVersion.ContainsRule(AtpRankingRulePivot.IncludingOlympicGames) || !level.IsOlympicGames)
                .ToList();
        }

        #endregion
    }
}
