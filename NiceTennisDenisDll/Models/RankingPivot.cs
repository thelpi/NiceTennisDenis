using System;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents an entry in a ranking.
    /// </summary>
    public sealed class RankingPivot
    {
        #region Public properties

        /// <summary>
        /// <see cref="RankingVersionPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public RankingVersionPivot Version { get; private set; }
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

        private RankingPivot(uint versionId, uint playerId, DateTime date, uint points, uint ranking, uint editions)
        {
            Version = RankingVersionPivot.Get(versionId);
            Player = PlayerPivot.Get(playerId);
            Date = date;
            Points = points;
            Ranking = ranking;
            Editions = editions;
        }

        /// <summary>
        /// Creats an instance of <see cref="RankingPivot"/>.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="RankingPivot"/>.</returns>
        internal static RankingPivot Create(MySqlDataReader reader)
        {
            return new RankingPivot(reader.Get<uint>("version_id"), reader.Get<uint>("player_id"), reader.Get<DateTime>("date"),
                reader.Get<uint>("points"), reader.Get<uint>("ranking"), reader.Get<uint>("editions"));
        }
    }
}
