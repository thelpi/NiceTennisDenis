using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a calendar slot.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class SlotPivot : BasePivot
    {
        private const uint MONTE_CARLO_SLOT_ID = 11;

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
        /// Inferred; Monte-Carlo Master 1000 slot y/n.
        /// </summary>
        public bool IsMonteCarlo { get { return Id == MONTE_CARLO_SLOT_ID; } }

        #endregion

        private SlotPivot(uint id, string name, uint displayOrder, uint levelId) : base(id, null, name)
        {
            DisplayOrder = displayOrder;
            Level = LevelPivot.Get(levelId);
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="SlotPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="SlotPivot"/>.</returns>
        internal static SlotPivot Create(MySqlDataReader reader)
        {
            return new SlotPivot(reader.Get<uint>("id"), reader.GetString("name"), reader.Get<uint>("display_order"), reader.Get<uint>("level_id"));
        }

        #region Static public methods

        /// <summary>
        /// Gets an <see cref="SlotPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="SlotPivot"/>. <c>Null</c> if not found.</returns>
        public static SlotPivot Get(uint id)
        {
            return Get<SlotPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="SlotPivot"/> by its code.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="SlotPivot"/>. <c>Null</c> if not found.</returns>
        public static SlotPivot Get(string code)
        {
            return Get<SlotPivot>(code);
        }

        /// <summary>
        /// Gets every instance of <see cref="SlotPivot"/>.
        /// </summary>
        /// <remarks>Instances are sorted by ascending <see cref="DisplayOrder"/>.</remarks>
        /// <returns>Collection of <see cref="SlotPivot"/>.</returns>
        public static IReadOnlyCollection<SlotPivot> GetList()
        {
            return GetList<SlotPivot>().OrderBy(slot => slot.DisplayOrder).ToList();
        }

        #endregion
    }
}
