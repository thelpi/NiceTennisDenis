using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a round.
    /// </summary>
    public class RoundPivot : BasePivot
    {
        /// <summary>
        /// Code for final round.
        /// </summary>
        public const string FINAL = "F";
        /// <summary>
        /// Code for bronze reward.
        /// </summary>
        public const string BRONZE_REWARD = "BR";

        /// <summary>
        /// Group stage y/n.
        /// </summary>
        public bool IsGroupStage { get; private set; }
        /// <summary>
        /// Players count.
        /// </summary>
        public uint PlayersCount { get; private set; }

        /// <summary>
        /// Inferred; standard round y/n.
        /// </summary>
        public bool Standard { get { return !IsGroupStage && Code != BRONZE_REWARD; } }

        private RoundPivot(uint id, string code, string name, bool isGroupStage, uint playersCount) : base(id, code, name)
        {
            IsGroupStage = isGroupStage;
            PlayersCount = playersCount;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="RoundPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>.</returns>
        internal static RoundPivot Create(MySqlDataReader reader)
        {
            return new RoundPivot(reader.Get<uint>("id"), reader.GetString("code"), reader.GetString("name"),
                reader.Get<byte>("is_group_stage") > 0, reader.Get<uint>("players_count"));
        }

        /// <summary>
        /// Gets an <see cref="RoundPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        public static RoundPivot Get(uint id)
        {
            return Get<RoundPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="RoundPivot"/> by its code.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        public static RoundPivot Get(string code)
        {
            return Get<RoundPivot>(code);
        }

        /// <summary>
        /// Gets a <see cref="RoundPivot"/> by its players count.
        /// </summary>
        /// <remarks>Excludes group stage rounds and bronze reward.</remarks>
        /// <param name="playersCount">Players count.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        public static RoundPivot GetByPlayersCount(uint playersCount)
        {
            return GetList().FirstOrDefault(me => me.PlayersCount == playersCount && me.Standard);
        }

        /// <summary>
        /// Gets every instance of <see cref="RoundPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="RoundPivot"/>.</returns>
        public static IReadOnlyCollection<RoundPivot> GetList()
        {
            return GetList<RoundPivot>();
        }
    }
}
