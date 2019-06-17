using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents a calendar slot.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class SlotPivot : BasePivot
    {
        #region Public properties

        /// <summary>
        /// Display order.
        /// </summary>
        public uint DisplayOrder { get; private set; }
        /// <summary>
        /// <see cref="LevelPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public LevelPivot Level { get; private set; }
        /// <summary>
        /// Mandatory slot for ranking (<c>Null</c> by default to follow the <see cref="LevelPivot"/> round).
        /// </summary>
        public bool? Mandatory { get; private set; }

        #endregion

        private SlotPivot(uint id, string name, uint displayOrder, uint levelId, bool? mandatory) : base(id, null, name)
        {
            DisplayOrder = displayOrder;
            Level = LevelPivot.Get(levelId);
            Mandatory = mandatory;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="SlotPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="SlotPivot"/>.</returns>
        internal static SlotPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            var mandatoryByte = reader.GetNull<byte>("mandatory");

            return new SlotPivot(reader.Get<uint>("id"),
                reader.GetString("name"),
                reader.Get<uint>("display_order"),
                reader.Get<uint>("level_id"),
                mandatoryByte.HasValue ? mandatoryByte.Value > 0 : (bool?)null);
        }

        /// <summary>
        /// Gets every instance of <see cref="SlotPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="SlotPivot"/>.</returns>
        internal static List<SlotPivot> GetList()
        {
            return GetList<SlotPivot>();
        }
    }
}
