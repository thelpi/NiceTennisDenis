﻿using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents a round.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class RoundPivot : BasePivot
    {
        private const string BRONZE_REWARD = "BR";
        private const string ROUND_ROBIN = "RR";
        private const string FINAL = "F";
        private const string QUARTER_FINAL = "QF";

        #region Public properties

        /// <summary>
        /// Theoretical players count.
        /// </summary>
        public uint PlayersCount { get; private set; }
        /// <summary>
        /// Round importance.
        /// </summary>
        /// <remarks>
        /// From 1 (final) to 7 (first round).
        /// Round robin and bronze reward are 8 and 9.
        /// </remarks>
        public uint Importance { get; private set; }
        /// <summary>
        /// Inferred; Is bronze reward y/n.
        /// </summary>
        public bool IsBronzeReward
        {
            get
            {
                return Code == BRONZE_REWARD;
            }
        }
        /// <summary>
        /// Inferred; Is round robin y/n.
        /// </summary>
        public bool IsRoundRobin
        {
            get
            {
                return Code == ROUND_ROBIN;
            }
        }
        /// <summary>
        /// Inferred; Is final y/n.
        /// </summary>
        public bool IsFinal
        {
            get
            {
                return Code == FINAL;
            }
        }

        #endregion

        private RoundPivot(uint id, string code, string name, uint playersCount, uint importance) : base(id, code, name)
        {
            PlayersCount = playersCount;
            Importance = importance;
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="RoundPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>.</returns>
        internal static RoundPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            return new RoundPivot(reader.Get<uint>("id"), reader.GetString("code"), reader.GetString("name"),
                reader.Get<uint>("players_count"), reader.Get<uint>("importance"));
        }

        /// <summary>
        /// Gets an <see cref="RoundPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        internal static RoundPivot Get(uint id)
        {
            return Get<RoundPivot>(id);
        }

        /// <summary>
        /// Gets quarter-final <see cref="RoundPivot"/>.
        /// </summary>
        /// <returns>Quarter-final <see cref="RoundPivot"/>.</returns>
        internal static RoundPivot GetQuarterFinal()
        {
            return Get<RoundPivot>(QUARTER_FINAL);
        }

        /// <summary>
        /// Gets an <see cref="RoundPivot"/> by its importance.
        /// </summary>
        /// <param name="importance">Importance.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        internal static RoundPivot GetByImportance(uint importance)
        {
            return GetList<RoundPivot>().FirstOrDefault(round => round.Importance == importance);
        }

        /// <summary>
        /// Gets a round by its players count.
        /// </summary>
        /// <remarks>Excludes round robin and bronze reward.</remarks>
        /// <param name="playersCount">Players count.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        internal static RoundPivot GetByPlayersCount(uint playersCount)
        {
            return GetList<RoundPivot>().FirstOrDefault(round =>
                round.PlayersCount == playersCount && !round.IsRoundRobin && !round.IsBronzeReward);
        }
    }
}
