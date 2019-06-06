﻿using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a tournament's edition.
    /// </summary>
    public class EditionPivot : BasePivot
    {
        private readonly List<MatchPivot> _matches;
        private uint? _realDrawSize = null;

        /// <summary>
        /// Year.
        /// </summary>
        public uint Year { get; private set; }
        /// <summary>
        /// <see cref="TournamentPivot"/>.
        /// </summary>
        public TournamentPivot Tournament { get; private set; }
        /// <summary>
        /// <see cref="SlotPivot"/>.
        /// </summary>
        public SlotPivot Slot { get; private set; }
        /// <summary>
        /// Draw size.
        /// </summary>
        public uint? DrawSize { get; private set; }
        /// <summary>
        /// <see cref="SurfacePivot"/>.
        /// </summary>
        public SurfacePivot? Surface { get; private set; }
        /// <summary>
        /// Indoor court y/n.
        /// </summary>
        public bool Indoor { get; private set; }
        /// <summary>
        /// <see cref="LevelPivot"/>.
        /// </summary>
        public LevelPivot Level { get; private set; }
        /// <summary>
        /// Beginning date.
        /// </summary>
        public DateTime DateBegin { get; private set; }
        /// <summary>
        /// Ending date.
        /// </summary>
        public DateTime DateEnd { get; private set; }
        /// <summary>
        /// Collection of <see cref="MatchPivot"/>.
        /// </summary>
        public IReadOnlyCollection<MatchPivot> Matches { get { return _matches; } }

        /// <summary>
        /// Inferred; gets if the instance is mandatory for ATP ranking.
        /// </summary>
        public bool MandatoryAtp { get { return Level.MandatoryAtp && (Slot == null || Slot.Id != SlotPivot.MONTE_CARLO_SLOT_ID); } }

        /// <summary>
        /// Gets the real draw size.
        /// </summary>
        /// <returns>Draw size.</returns>
        public uint DrawSizeReal()
        {
            if (DrawSize.HasValue)
            {
                return DrawSize.Value;
            }
            else if (_matches.Count == 0)
            {
                return DrawSize.GetValueOrDefault(0);
            }
            else
            {
                if (!_realDrawSize.HasValue)
                {
                    var firstRound = _matches.OrderByDescending(me => me.Round.PlayersCount).First().Round;

                    if (firstRound.IsGroupStage)
                    {
                        // Masters (typically).
                        _realDrawSize = firstRound.PlayersCount;
                    }
                    else if (firstRound.Code == RoundPivot.BRONZE_REWARD)
                    {
                        // Very weird case of 2 semi-finals, one final and one "third place" match.
                        _realDrawSize = firstRound.PlayersCount * 2;
                    }
                    else if (firstRound.PlayersCount == 2)
                    {
                        // Final only.
                        _realDrawSize = firstRound.PlayersCount;
                    }
                    else
                    {
                        var countFirstRoundMatches = _matches.Count(me => me.Round == firstRound);
                        if (countFirstRoundMatches * 2 > firstRound.PlayersCount)
                        {
                            // Another very weird scenario.
                            _realDrawSize = firstRound.PlayersCount;
                        }
                        else if (countFirstRoundMatches * 2 == firstRound.PlayersCount)
                        {
                            // Most freqent scenario.
                            _realDrawSize = firstRound.PlayersCount;
                        }
                        else
                        {
                            // the first round contains exemptions.
                            // In that case, we take players count from the second round + "losers" from the first round
                            var secondRound = RoundPivot.GetByPlayersCount(firstRound.PlayersCount / 2);
                            _realDrawSize = secondRound.PlayersCount + (uint)countFirstRoundMatches;
                        }
                    }
                }

                return _realDrawSize.Value;
            }
        }

        private EditionPivot(uint id, uint year, string name, uint tournamentId, uint? slotId, uint? drawSize, uint? surfaceId,
            bool indoor, uint levelId, DateTime dateBegin, DateTime dateEnd) : base(id, null, name)
        {
            Year = year;
            Tournament = Get<TournamentPivot>(tournamentId);
            Slot = !slotId.HasValue ? null : Get<SlotPivot>(slotId.Value);
            drawSize = DrawSize;
            Surface = (SurfacePivot?)surfaceId;
            Indoor = indoor;
            Level = Get<LevelPivot>(levelId);
            DateBegin = dateBegin;
            DateEnd = dateEnd;
            _matches = new List<MatchPivot>();
        }

        /// <summary>
        /// Adds a <see cref="MatchPivot"/> to <see cref="_matches"/>.
        /// </summary>
        /// <param name="match"><see cref="MatchPivot"/> to add.</param>
        internal void AddMatch(MatchPivot match)
        {
            if (match?.Edition == this && !_matches.Contains(match))
            {
                _matches.Add(match);
            }
        }

        /// <summary>
        /// Checks if the specified <see cref="PlayerPivot"/> is involved in this <see cref="EditionPivot"/> instance.
        /// </summary>
        /// <param name="player">The <see cref="PlayerPivot"/> to check.</param>
        /// <returns><c>True</c> if involved in this edition; <c>False</c> otherwise.</returns>
        public bool InvolvePlayer(PlayerPivot player)
        {
            return player != null && Matches.Any(me => me.Players.Contains(player));
        }

        /// <summary>
        /// Checks if the specified <see cref="PlayerPivot"/> has played as a qualifier in this <see cref="EditionPivot"/> instance.
        /// </summary>
        /// <param name="player">The <see cref="PlayerPivot"/> to check.</param>
        /// <returns><c>True</c> if has played as a qualifier in this edition; <c>False</c> otherwise.</returns>
        public bool PlayerIsQualified(PlayerPivot player)
        {
            return player != null && Matches.Any(me =>
                (me.Winner == player && me.WinnerEntry?.Code == EntryPivot.QUALIFICATION_CODE)
                || (me.Loser == player && me.LoserEntry?.Code == EntryPivot.QUALIFICATION_CODE)
            );
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="EditionPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="EditionPivot"/>.</returns>
        internal static EditionPivot Create(MySqlDataReader reader)
        {
            return new EditionPivot(reader.Get<uint>("id"), reader.Get<uint>("year"), reader.GetString("name"), reader.Get<uint>("tournament_id"),
                reader.GetNull<uint>("slot_id"), reader.GetNull<uint>("draw_size"), reader.GetNull<uint>("surface_id"), reader.Get<byte>("indoor") > 0,
                reader.Get<uint>("level_id"), reader.Get<DateTime>("date_begin"), reader.Get<DateTime>("date_end"));
        }

        /// <summary>
        /// Gets an <see cref="EditionPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="EditionPivot"/>. <c>Null</c> if not found.</returns>
        public static EditionPivot Get(uint id)
        {
            return Get<EditionPivot>(id);
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> GetList()
        {
            return GetList<EditionPivot>();
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/> for a specified year.
        /// </summary>
        /// <param name="year">Year.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> GetListByYear(uint year)
        {
            return GetList().Where(me => me.Year == year).ToList();
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/> for a specified <see cref="TournamentPivot"/>.
        /// </summary>
        /// <param name="tournamentId"><see cref="TournamentPivot"/> identifier.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> GetListByTournament(uint tournamentId)
        {
            return GetList().Where(me => me.Tournament.Id == tournamentId).ToList();
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/> for a specified <see cref="SlotPivot"/>.
        /// </summary>
        /// <param name="slotId"><see cref="SlotPivot"/> identifier.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> GetListBySlot(uint slotId)
        {
            return GetList().Where(me => me.Slot?.Id == slotId).ToList();
        }

        /// <summary>
        /// Gets the ending date of the latest edition.
        /// </summary>
        /// <returns>Ending date of the latest edition; <c>Null</c> if no edition loaded.</returns>
        public static DateTime? GetLatestsEditionDateEnding()
        {
            return GetList().OrderByDescending(x => x.DateEnd).FirstOrDefault()?.DateEnd;
        }

        /// <summary>
        /// Gets a collection of <see cref="EditionPivot"/> to compute the ATP ranking at a specified date.
        /// </summary>
        /// <param name="atpRankingVersion"><see cref="AtpRankingVersionPivot"/></param>
        /// <param name="date">Ranking date; if not a monday, no results returned.</param>
        /// <param name="involvedPlayers">Out; involved <see cref="PlayerPivot"/> for the collection of <see cref="EditionPivot"/> returned.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> EditionsForAtpRankingAtDate(AtpRankingVersionPivot atpRankingVersion, DateTime date,
            out IReadOnlyCollection<PlayerPivot> involvedPlayers)
        {
            if (date.DayOfWeek != DayOfWeek.Monday)
            {
                involvedPlayers = new List<PlayerPivot>();
                return new List<EditionPivot>();
            }

            var editionsRollingYear = GetList().Where(me =>
                me.DateEnd < date
                && me.DateEnd >= date.AddDays(-52 * 7) // Days in a week * weeks in a year.
                && AtpGridPointPivot.GetRankableLevelList(atpRankingVersion).Contains(me.Level)).ToList();

            if (atpRankingVersion.Rules.Contains(AtpRankingRulePivot.ExcludingRedundantTournaments))
            {
                // No redundant tournament, when slot is unknown (take the latest).
                // No redundant slot, regarding of the tournament (take the latest).
                editionsRollingYear = editionsRollingYear.Where(me =>
                    !editionsRollingYear.Any(you =>
                        you.Slot == me.Slot
                        && (you.Slot != null || (you.Tournament == me.Tournament))
                        && you.DateEnd > me.DateEnd
                    )
                ).ToList();
            }

            involvedPlayers = editionsRollingYear.SelectMany(me => me.Matches.SelectMany(you => you.Players)).ToList();
            return editionsRollingYear;
        }
    }
}
