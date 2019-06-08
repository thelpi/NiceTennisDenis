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
    public sealed class AtpQualificationPivot : BasePivot
    {
        #region Public properties

        /// <summary>
        /// <see cref="LevelPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public LevelPivot Level { get; private set; }
        /// <summary>
        /// Minimal draw size.
        /// </summary>
        public uint MinimalDrawSize { get; private set; }
        /// <summary>
        /// Points.
        /// </summary>
        public uint Points { get; private set; }

        #endregion

        private AtpQualificationPivot(uint levelId, uint minimalDrawSize, uint points) : base(0, null, null)
        {
            Level = LevelPivot.Get(levelId);
            MinimalDrawSize = minimalDrawSize;
            Points = points;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        #region Public methods

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Level.Name}" + (MinimalDrawSize > 0 ? $" ({MinimalDrawSize})" : string.Empty);
        }

        #endregion

        /// <summary>
        /// Creates a <see cref="AtpQualificationPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="AtpQualificationPivot"/>.</returns>
        internal static AtpQualificationPivot Create(MySqlDataReader reader)
        {
            return new AtpQualificationPivot(reader.Get<uint>("level_id"),
                reader.Get<uint>("draw_size_min"),
                reader.Get<uint>("points"));
        }

        /// <summary>
        /// Gets every instance of <see cref="AtpQualificationPivot"/>.
        /// </summary>
        /// <remarks>Order by descending <see cref="MinimalDrawSize"/>.</remarks>
        /// <returns>Collection of <see cref="AtpGridPointPivot"/>.</returns>
        public static IReadOnlyCollection<AtpQualificationPivot> GetList()
        {
            return GetList<AtpQualificationPivot>().OrderByDescending(atpQualification => atpQualification.MinimalDrawSize).ToList();
        }

        /// <summary>
        /// Gets an <see cref="AtpQualificationPivot"/> by its key.
        /// </summary>
        /// <param name="levelId"><see cref="LevelPivot"/> identifier.</param>
        /// <param name="drawSize">Edition draw size.</param>
        /// <returns>Instance of <see cref="AtpQualificationPivot"/>. <c>Null</c> if not found.</returns>
        public static AtpQualificationPivot GetByLevelAndDrawSize(uint levelId, uint drawSize)
        {
            // Uses "GetList" method (instead of "GetList<AtpQualificationPivot>") to keep the OrderBy.
            return GetList().FirstOrDefault(atpQualification => atpQualification.Level.Id == levelId && atpQualification.MinimalDrawSize <= drawSize);
        }
    }
}
