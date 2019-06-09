using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents an entry in an ATP ranking.
    /// </summary>
    public sealed class AtpRankingPivot
    {
        #region Public properties

        /// <summary>
        /// <see cref="AtpRankingVersionPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public AtpRankingVersionPivot Version { get; private set; }
        /// <summary>
        /// <see cref="PlayerPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public PlayerPivot Player { get; private set; }
        /// <summary>
        /// Ranking date.
        /// </summary>
        /// <remarks>Always a <see cref="DayOfWeek.Monday"/>.</remarks>
        public DateTime Date { get; private set; }
        /// <summary>
        /// Points.
        /// </summary>
        public uint Points { get; private set; }
        /// <summary>
        /// Ranking.
        /// </summary>
        public uint Ranking { get; private set; }
        /// <summary>
        /// Editions played count.
        /// </summary>
        public uint Editions { get; private set; }
        /// <summary>
        /// Inferred; player's name.
        /// </summary>
        public string PlayerName { get { return Player.Name; } }
        /// <summary>
        /// Inferred; player's profile picture path.
        /// </summary>
        public string PlayerProfilePicturePath { get { return Player.ProfilePicturePath; } }

        #endregion

        private AtpRankingPivot(uint versionId, uint playerId, DateTime date, uint points, uint ranking, uint editions)
        {
            Version = AtpRankingVersionPivot.Get(versionId);
            Player = PlayerPivot.Get(playerId);
            Date = date;
            Points = points;
            Ranking = ranking;
            Editions = editions;
        }

        /// <summary>
        /// Creats an instance of <see cref="AtpRankingPivot"/>.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="AtpRankingPivot"/>.</returns>
        internal static AtpRankingPivot Create(MySqlDataReader reader)
        {
            return new AtpRankingPivot(reader.Get<uint>("version_id"), reader.Get<uint>("player_id"), reader.Get<DateTime>("date"),
                reader.Get<uint>("points"), reader.Get<uint>("ranking"), reader.Get<uint>("editions"));
        }
    }
}
