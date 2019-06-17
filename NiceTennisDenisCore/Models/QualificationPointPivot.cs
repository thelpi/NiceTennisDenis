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
    public sealed class QualificationPointPivot : BasePivot
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

        private QualificationPointPivot(uint levelId, uint minimalDrawSize, uint points) : base(0, null, null)
        {
            Level = LevelPivot.Get(levelId);
            MinimalDrawSize = minimalDrawSize;
            Points = points;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates a <see cref="QualificationPointPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="QualificationPointPivot"/>.</returns>
        internal static QualificationPointPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            return new QualificationPointPivot(reader.Get<uint>("level_id"),
                reader.Get<uint>("draw_size_min"),
                reader.Get<uint>("points"));
        }

        /// <summary>
        /// Gets an <see cref="QualificationPointPivot"/> by its key.
        /// </summary>
        /// <param name="levelId"><see cref="LevelPivot"/> identifier.</param>
        /// <param name="drawSize">Edition draw size.</param>
        /// <returns>Instance of <see cref="QualificationPointPivot"/>. <c>Null</c> if not found.</returns>
        internal static QualificationPointPivot GetByLevelAndDrawSize(uint levelId, uint drawSize)
        {
            return GetList<QualificationPointPivot>()
                .OrderByDescending(qualification => qualification.MinimalDrawSize)
                .FirstOrDefault(qualification => qualification.Level.Id == levelId && qualification.MinimalDrawSize <= drawSize);
        }
    }
}
