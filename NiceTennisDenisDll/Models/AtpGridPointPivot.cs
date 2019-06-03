using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents the scale of ATP ranking points.
    /// </summary>
    public class AtpGridPointPivot : BasePivot
    {
        private static List<uint> _rankedLevelIdList = null;

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
        /// <summary>
        /// Combinable y/n.
        /// </summary>
        public bool Combinable { get; private set; }

        private AtpGridPointPivot(uint levelId, uint roundId, uint points, bool combinable) : base(0, null, null)
        {
            Level = LevelPivot.Get(levelId);
            Round = RoundPivot.Get(roundId);
            Points = points;
            Combinable = combinable;
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
                reader.GetUInt32("points"),
                reader.GetByte("combinable") > 0);
        }

        /// <summary>
        /// Collection of <see cref="LevelPivot"/> identifiers used to compute ATP ranking.
        /// </summary>
        internal static IReadOnlyCollection<uint> RankedLevelIdList
        {
            get
            {
                if (_rankedLevelIdList == null)
                {
                    _rankedLevelIdList = GetList<AtpGridPointPivot>().Select(me => me.Level.Id).Distinct().ToList();
                }
                return _rankedLevelIdList;
            }
        }

        /// <summary>
        /// Gets every instance of <see cref="AtpGridPointPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="AtpGridPointPivot"/>.</returns>
        public static IReadOnlyCollection<AtpGridPointPivot> GetList()
        {
            return GetList<AtpGridPointPivot>().ToList();
        }
    }
}
