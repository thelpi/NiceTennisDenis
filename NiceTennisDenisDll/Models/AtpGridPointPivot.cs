using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents the scale of ATP ranking points.
    /// </summary>
    /// <remarks><see cref="BasePivot.Id"/> should be ignored.</remarks>
    public class AtpGridPointPivot : BasePivot
    {
        /// <summary>
        /// <see cref="LevelPivot"/>
        /// </summary>
        public LevelPivot Level { get; private set; }
        /// <summary>
        /// <see cref="RoundPivot"/>
        /// </summary>
        public RoundPivot Round { get; private set; }
        /// <summary>
        /// Points.
        /// </summary>
        public uint Points { get; private set; }

        private AtpGridPointPivot(uint levelId, uint roundId, uint points) : base(0, null, null)
        {
            Level = LevelPivot.Get(levelId);
            Round = RoundPivot.Get(roundId);
            Points = points;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates a <see cref="AtpGridPointPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="AtpGridPointPivot"/>.</returns>
        internal static AtpGridPointPivot Create(MySqlDataReader reader)
        {
            return new AtpGridPointPivot(reader.GetUInt32("level_id"),
                reader.GetUInt32("round_id"),
                reader.GetUInt32("points"));
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
        internal static IReadOnlyCollection<LevelPivot> GetRankableLevelList(AtpRankingVersionPivot atpRankingVersion)
        {
            var baseList = GetList<AtpGridPointPivot>().Select(me => me.Level).Distinct().ToList();
            if (!atpRankingVersion.Rules.Contains(AtpRankingRulePivot.IncludingOlympicGames))
            {
                baseList.Remove(LevelPivot.Get(LevelPivot.OLYMPIC_GAMES_CODE));
            }

            return baseList;
        }
    }
}
