using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a competition level.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class LevelPivot : BasePivot
    {
        private const string OLYMPIC_GAMES_CODE = "O";

        #region Public properties

        /// <summary>
        /// Display order.
        /// </summary>
        public uint DisplayOrder { get; private set; }
        /// <summary>
        /// Mandatory for ranking.
        /// </summary>
        public bool Mandatory { get; private set; }
        /// <summary>
        /// Inferred; level is olympic games y/n.
        /// </summary>
        public bool IsOlympicGames { get { return Code == OLYMPIC_GAMES_CODE; } }

        #endregion

        private LevelPivot(uint id, string code, string name, uint displayOrder, bool mandatory) : base(id, code, name)
        {
            DisplayOrder = displayOrder;
            Mandatory = mandatory;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="LevelPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="LevelPivot"/>.</returns>
        internal static LevelPivot Create(MySqlDataReader reader)
        {
            return new LevelPivot(reader.Get<uint>("id"), reader.GetString("code"), reader.GetString("name"),
                reader.Get<uint>("display_order"), reader.Get<byte>("mandatory") > 0);
        }

        #region Public static methods

        /// <summary>
        /// Gets an <see cref="LevelPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="LevelPivot"/>. <c>Null</c> if not found.</returns>
        public static LevelPivot Get(uint id)
        {
            return Get<LevelPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="LevelPivot"/> by its code.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="LevelPivot"/>. <c>Null</c> if not found.</returns>
        public static LevelPivot Get(string code)
        {
            return Get<LevelPivot>(code);
        }

        /// <summary>
        /// Gets every instance of <see cref="LevelPivot"/>.
        /// </summary>
        /// <remarks>Instances are sorted by ascending <see cref="DisplayOrder"/>.</remarks>
        /// <returns>Collection of <see cref="LevelPivot"/>.</returns>
        public static IReadOnlyCollection<LevelPivot> GetList()
        {
            return GetList<LevelPivot>().OrderBy(level => level.DisplayOrder).ToList();
        }

        #endregion
    }
}
