using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents an entry in an ATP ranking.
    /// </summary>
    /// <remarks><see cref="BasePivot.Id"/> should be ignroed.</remarks>
    /// <seealso cref="BasePivot"/>
    public sealed class AtpRankingPivot : BasePivot
    {
        private static List<AtpRankingPivot> _rankings = new List<AtpRankingPivot>();

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

        #endregion

        private AtpRankingPivot(uint versionId, uint playerId, DateTime date, uint points, uint ranking, uint editions)
            : base(0, null, null)
        {
            Version = AtpRankingVersionPivot.Get(versionId);
            Player = PlayerPivot.Get(playerId);
            Date = date;
            Points = points;
            Ranking = ranking;
            Editions = editions;
            _rankings.Add(this);
        }

        /// <inheritdoc />
        internal override void AvoidInheritance()
        {
            throw new NotImplementedException();
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

        #region Public static methods

        /// <summary>
        /// Gets the ranking at a specified date.
        /// </summary>
        /// <param name="versionId"><see cref="AtpRankingVersionPivot"/> identifier.</param>
        /// <param name="date">Ranking date. If not a monday, takes the previous monday.</param>
        /// <returns>Ranking at date, sorted by ranking position.</returns>
        public static IReadOnlyCollection<AtpRankingPivot> GetRankingAtDate(uint versionId, DateTime date)
        {
            date = date.DayOfWeek == DayOfWeek.Monday ? date :
                (date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-6) : date.AddDays(-((int)date.DayOfWeek - 1)));

            return _rankings
                .Where(ranking => ranking.Version.Id == versionId && ranking.Date.Date == date.Date)
                .OrderBy(ranking => ranking.Ranking)
                .ToList();
        }

        #endregion
    }
}
