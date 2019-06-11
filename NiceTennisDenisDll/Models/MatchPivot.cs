﻿using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a match.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class MatchPivot : BasePivot
    {
        private static readonly Dictionary<StatisticPivot, string> STATISTIC_COLUMNS = new Dictionary<StatisticPivot, string>
        {
            { StatisticPivot.Ace, "ace" },
            { StatisticPivot.BreakPointFaced, "bp_faced" },
            { StatisticPivot.BreakPointSaved, "bp_saved" },
            { StatisticPivot.DoubleFault, "df" },
            { StatisticPivot.FirstServeIn, "1st_in" },
            { StatisticPivot.FirstServeWon, "1st_won" },
            { StatisticPivot.SecondServeWon, "2nd_won" },
            { StatisticPivot.ServeGame, "sv_gms" },
            { StatisticPivot.ServePoint, "sv_pt" }
        };

        #region Public properties

        /// <summary>
        /// <see cref="EditionPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public EditionPivot Edition { get; private set; }
        /// <summary>
        /// Match number.
        /// </summary>
        public uint MatchNumber { get; private set; }
        /// <summary>
        /// <see cref="BestOfPivot"/>
        /// </summary>
        /// <remarks></remarks>
        public BestOfPivot BestOf { get; private set; }
        /// <summary>
        /// <see cref="RoundPivot"/>
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public RoundPivot Round { get; private set; }
        /// <summary>
        /// Minutes.
        /// </summary>
        public uint? Minutes { get; private set; }
        /// <summary>
        /// Winner <see cref="PlayerPivot"/>.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public PlayerPivot Winner { get; private set; }
        /// <summary>
        /// Winner seed.
        /// </summary>
        public uint? WinnerSeed { get; private set; }
        /// <summary>
        /// Winner <see cref="EntryPivot"/>.
        /// </summary>
        /// <remarks>Can be <c>Null</c>.</remarks>
        public EntryPivot WinnerEntry { get; private set; }
        /// <summary>
        /// Winner rank.
        /// </summary>
        public uint? WinnerRank { get; private set; }
        /// <summary>
        /// Winner points.
        /// </summary>
        public uint? WinnerRankPoints { get; private set; }
        /// <summary>
        /// Loser <see cref="PlayerPivot"/>.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public PlayerPivot Loser { get; private set; }
        /// <summary>
        /// Loser seed.
        /// </summary>
        public uint? LoserSeed { get; private set; }
        /// <summary>
        /// Loser <see cref="EntryPivot"/>.
        /// </summary>
        /// <remarks>Can be <c>Null</c>.</remarks>
        public EntryPivot LoserEntry { get; private set; }
        /// <summary>
        /// Loser rank.
        /// </summary>
        public uint? LoserRank { get; private set; }
        /// <summary>
        /// Loser points.
        /// </summary>
        public uint? LoserRankPoints { get; private set; }
        /// <summary>
        /// Walkover y/n.
        /// </summary>
        public bool Walkover { get; private set; }
        /// <summary>
        /// Retirement y/n.
        /// </summary>
        public bool Retirement { get; private set; }
        /// <summary>
        /// Disqualification y/n.
        /// </summary>
        public bool Disqualification { get; private set; }
        /// <summary>
        /// Unfinished y/n.
        /// </summary>
        public bool Unfinished { get; private set; }
        /// <summary>
        /// Sets detail.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>, but can be empty.</remarks>
        public IReadOnlyCollection<SetPivot> Sets { get; private set; }
        /// <summary>
        /// Winner statistics.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>, but can be empty.</remarks>
        public IReadOnlyDictionary<StatisticPivot, uint?> WinnerStatistics { get; private set; }
        /// <summary>
        /// Loser statistics.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>, but can be empty.</remarks>
        public IReadOnlyDictionary<StatisticPivot, uint?> LoserStatistics { get; private set; }
        /// <summary>
        /// Super tie-break raw value.
        /// </summary>
        /// <remarks>As provided by the database.</remarks>
        public string RawSuperTieBreak { get; private set; }
        /// <summary>
        /// <see cref="GridPointPivot"/> (for winner).
        /// </summary>
        /// <remarks>Can be <c>Null</c>.</remarks>
        public GridPointPivot PointGrid { get; private set; }
        /// <summary>
        /// Inferred; <see cref="PlayerPivot"/> involved.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public IReadOnlyCollection<PlayerPivot> Players { get { return new List<PlayerPivot> { Winner, Loser }; } }

        #endregion

        private MatchPivot(uint id, uint editionId, uint matchNumber, uint bestOf, uint roundId, uint? minutes,
            uint winnerId, uint? winnerSeed, uint? winnerEntryId, uint? winnerRank, uint? winnerRankPoints,
            uint loserId, uint? loserSeed, uint? loserEntryId, uint? loserRank, uint? loserRankPoints,
            bool walkover, bool retirement, bool disqualification, bool unfinished, string rawSuperTieBreak) : base(id, null, null)
        {
            Edition = Get<EditionPivot>(editionId);
            MatchNumber = matchNumber;
            BestOf = (BestOfPivot)bestOf;
            Round = Get<RoundPivot>(roundId);
            Minutes = minutes;
            Winner = Get<PlayerPivot>(winnerId);
            WinnerSeed = winnerSeed;
            WinnerEntry = !winnerEntryId.HasValue ? null : Get<EntryPivot>(winnerEntryId.Value);
            WinnerRank = winnerRank;
            WinnerRankPoints = winnerRankPoints;
            Loser = Get<PlayerPivot>(loserId);
            LoserSeed = loserSeed;
            LoserEntry = !loserEntryId.HasValue ? null : Get<EntryPivot>(loserEntryId.Value);
            LoserRank = loserRank;
            LoserRankPoints = loserRankPoints;
            Walkover = walkover;
            Retirement = retirement;
            Disqualification = disqualification;
            Unfinished = unfinished;
            RawSuperTieBreak = rawSuperTieBreak;
            PointGrid = GridPointPivot.GetList().FirstOrDefault(me => me.Round == Round && me.Level == Edition.Level);
            Edition.AddMatch(this);
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        #region Public methods

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Id} - {Edition.ToString()} - {Round.Name} - {Winner.Name} - {Loser.Name}";
        }

        #endregion

        /// <summary>
        /// Creates an instance of <see cref="MatchPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="MatchPivot"/>.</returns>
        internal static MatchPivot Create(MySqlDataReader reader, object[] otherParameters)
        {
            return new MatchPivot(reader.Get<uint>("id"), reader.Get<uint>("edition_id"), reader.Get<uint>("match_num"),
                reader.Get<uint>("best_of"), reader.Get<uint>("round_id"), reader.GetNull<uint>("minutes"),
                reader.Get<uint>("winner_id"), reader.GetNull<uint>("winner_seed"), reader.GetNull<uint>("winner_entry_id"),
                reader.GetNull<uint>("winner_rank"), reader.GetNull<uint>("winner_rank_points"),
                reader.Get<uint>("loser_id"), reader.GetNull<uint>("loser_seed"), reader.GetNull<uint>("loser_entry_id"),
                reader.GetNull<uint>("loser_rank"), reader.GetNull<uint>("loser_rank_points"),
                reader.Get<byte>("walkover") > 0, reader.Get<byte>("retirement") > 0,
                reader.Get<byte>("disqualification") > 0, reader.Get<byte>("unfinished") > 0,
                reader.IsDBNull("super_tb") ? null : reader.GetString("super_tb"))
            {
                Sets = Enumerable.Range(1, 5)
                                    .Select(me => SetPivot.Create(reader, me))
                                    .ToList(),
                WinnerStatistics = STATISTIC_COLUMNS
                                    .Select(me => new { me.Key, Value = reader.GetNull<uint>(string.Concat("w_", me.Value)) })
                                    .ToDictionary(me => me.Key, me => me.Value),
                LoserStatistics = STATISTIC_COLUMNS
                                    .Select(me => new { me.Key, Value = reader.GetNull<uint>(string.Concat("l_", me.Value)) })
                                    .ToDictionary(me => me.Key, me => me.Value)
            };
        }

        #region Public static methods

        /// <summary>
        /// Gets an <see cref="MatchPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="MatchPivot"/>. <c>Null</c> if not found.</returns>
        public static MatchPivot Get(uint id)
        {
            return Get<MatchPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="MatchPivot"/> by its identifier.
        /// </summary>
        /// <param name="editionId"><see cref="EditionPivot"/> identifier.</param>
        /// <param name="number"><see cref="MatchNumber"/></param>
        /// <returns>Instance of <see cref="MatchPivot"/>. <c>Null</c> if not found.</returns>
        public static MatchPivot GetByEditionAndNumber(uint editionId, uint number)
        {
            return GetList<MatchPivot>().FirstOrDefault(match => match.Edition.Id == editionId && match.MatchNumber == number);
        }

        /// <summary>
        /// Gets every instance of <see cref="MatchPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="MatchPivot"/>.</returns>
        public static IReadOnlyCollection<MatchPivot> GetList()
        {
            return GetList<MatchPivot>();
        }

        #endregion
    }
}
