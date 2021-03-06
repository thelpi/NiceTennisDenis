﻿using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents a tournament's edition.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class EditionPivot : BasePivot
    {
        private uint? _realDrawSize = null;
        private uint? _drawSizeStored = null;
        private RoundPivot _firstRound = null;

        #region Public properties

        /// <summary>
        /// Year.
        /// </summary>
        public uint Year { get; private set; }
        /// <summary>
        /// <see cref="TournamentPivot"/>.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public TournamentPivot Tournament { get; private set; }
        /// <summary>
        /// <see cref="SlotPivot"/>.
        /// </summary>
        /// <remarks>Can be <c>Null</c>.</remarks>
        public SlotPivot Slot { get; private set; }
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
        /// <remarks>Can't be <c>Null</c>.</remarks>
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
        /// <remarks>Can't be <c>Null</c>, but can be empty if matches not loaded.</remarks>
        public List<MatchPivot> Matches { get; }
        /// <summary>
        /// Inferred; mandatory for ranking y/n.
        /// </summary>
        public bool Mandatory { get { return (Level.Mandatory && Slot?.Mandatory != false) || Slot?.Mandatory == true; } }
        /// <summary>
        /// Computed; Draw size.
        /// </summary>
        /// <remarks>
        /// The database field can be <c>Null</c> (and is, in mose cases).
        /// In that case, <see cref="RoundPivot.PlayersCount"/> is used to guess the draw size.
        /// Matches of the edition must be loaded to do that.
        /// </remarks>
        public uint DrawSize
        {
            get
            {
                if (_drawSizeStored.HasValue)
                {
                    _realDrawSize = _drawSizeStored.Value;
                }
                else if (Matches.Count == 0)
                {
                    return 0;
                }
                else
                {
                    if (!_realDrawSize.HasValue)
                    {
                        var firstRound = Matches.OrderByDescending(me => me.Round.PlayersCount).First().Round;

                        if (firstRound.IsRoundRobin)
                        {
                            // Masters group stage.
                            _realDrawSize = firstRound.PlayersCount;
                        }
                        else if (firstRound.IsBronzeReward)
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
                            var countFirstRoundMatches = Matches.Count(me => me.Round == firstRound);
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
                }

                return _realDrawSize.Value;
            }
        }
        /// <summary>
        /// Computed; First <see cref="RoundPivot"/>.
        /// </summary>
        /// <remarks><c>Null</c> while <see cref="Matches"/> has not been loaded.</remarks>
        public RoundPivot FirstRound
        {
            get
            {
                if (Matches.Count == 0)
                {
                    return null;
                }

                if (_firstRound == null)
                {
                    _firstRound = Matches.OrderByDescending(match => match.Round.Importance).First().Round;
                }

                return _firstRound;
            }
        }
        /// <summary>
        /// Inferred; final <see cref="MatchPivot"/>.
        /// </summary>
        /// <remarks><c>Null</c> if matches are not loaded.</remarks>
        public MatchPivot Final
        {
            get
            {
                return Matches.FirstOrDefault(match => match.Round.IsFinal);
            }
        }

        #endregion

        private EditionPivot(uint id, uint year, string name, uint tournamentId, uint? slotId, uint? drawSize, uint? surfaceId,
            bool indoor, uint levelId, DateTime dateBegin, DateTime dateEnd) : base(id, null, name)
        {
            Year = year;
            Tournament = Get<TournamentPivot>(tournamentId);
            Slot = slotId.HasValue ? Get<SlotPivot>(slotId.Value) : null;
            _drawSizeStored = drawSize;
            Surface = (SurfacePivot?)surfaceId;
            Indoor = indoor;
            Level = Get<LevelPivot>(levelId);
            DateBegin = dateBegin;
            DateEnd = dateEnd;
            Matches = new List<MatchPivot>();
        }

        private bool PlayerIsQualified(PlayerPivot player)
        {
            return player != null && Matches.Any(match =>
                (match.Winner == player && match.WinnerEntry?.IsQualification == true)
                || (match.Loser == player && match.LoserEntry?.IsQualification == true)
            );
        }

        /// <summary>
        /// Adds a <see cref="MatchPivot"/> to <see cref="Matches"/>.
        /// </summary>
        /// <param name="match"><see cref="MatchPivot"/> to add.</param>
        internal void AddMatch(MatchPivot match)
        {
            if (match?.Edition == this && !Matches.Contains(match))
            {
                Matches.Add(match);
            }
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Checks if the specified player is involved in this edition.
        /// </summary>
        /// <param name="player">The <see cref="PlayerPivot"/> to check.</param>
        /// <returns><c>True</c> if involved in this edition; <c>False</c> otherwise.</returns>
        internal bool InvolvePlayer(PlayerPivot player)
        {
            return player != null && Matches.Any(match => match.Players.Contains(player));
        }

        /// <summary>
        /// Gets the number of points gained by a specified player for this edition. The gain might vary regarding of the ruleset.
        /// </summary>
        /// <param name="player">A <see cref="PlayerPivot"/></param>
        /// <param name="rankingVersion">A <see cref="RankingRulePivot"/> (ruleset of current ranking).</param>
        /// <returns>Number of points for this player at this edition; 0 if any argument is <c>Null</c>.</returns>
        internal uint GetPlayerPoints(PlayerPivot player, RankingVersionPivot rankingVersion)
        {
            uint points = 0;

            if (player == null || rankingVersion == null)
            {
                return points;
            }

            // If qualifcation rule applies and player comes from qualifications for this edition.
            if (rankingVersion.ContainsRule(RankingRulePivot.IncludingQualificationBonus) && PlayerIsQualified(player))
            {
                points = QualificationPointPivot.GetByLevelAndDrawSize(Level.Id, DrawSize)?.Points ?? 0;
            }

            // Cumulable points (round robin).
            points += (uint)Matches
                .Where(match => match.Winner == player && match.Round.IsRoundRobin)
                .Sum(match => match.PointGrid?.Points ?? 0);

            var bestWin = Matches
                .Where(match => match.Winner == player && !match.Round.IsRoundRobin && !match.Round.IsBronzeReward)
                .OrderBy(match => match.Round.Importance)
                .FirstOrDefault();
            var bestLose = Matches
                .Where(match => match.Loser == player && !match.Round.IsRoundRobin && !match.Round.IsBronzeReward)
                .OrderBy(match => match.Round.Importance)
                .FirstOrDefault();

            if (Matches.Any(match => match.Round.IsBronzeReward && match.Players.Contains(player)))
            {
                if (Matches.Any(match => match.Winner == player && match.Round.IsBronzeReward))
                {
                    // Third place points.
                    points += Matches
                        .First(match => match.Winner == player && match.Round.IsBronzeReward)
                        .PointGrid?.Points ?? 0;
                }
                else
                {
                    // Fourth place points.
                    points += GridPointPivot.GetByLevelAndRound(Level.Id, RoundPivot.GetQuarterFinal().Id)?.Points ?? 0;
                }
            }
            else
            {
                // Unable to detect a lose by walkover the next round than a win by walkover.
                // In that case, points from the win by walkover are ignored.
                if (bestLose == null)
                {
                    points += bestWin?.PointGrid?.Points ?? 0;
                }
                else if (bestWin != null)
                {
                    var lastWinRound = RoundPivot.GetByImportance(bestLose.Round.Importance + 1);
                    var grid = bestWin.PointGrid;
                    if (bestWin.Round.Importance > lastWinRound.Importance)
                    {
                        grid = GridPointPivot.GetByLevelAndRound(Level.Id, lastWinRound.Id);
                    }
                    points += grid?.Points ?? 0;
                }
                else
                {
                    points += bestLose.PointGrid?.ParticipationPoints ?? 0;
                }
            }

            return points;
        }

        /// <summary>
        /// Creates an instance of <see cref="EditionPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="EditionPivot"/>.</returns>
        internal static EditionPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            return new EditionPivot(reader.Get<uint>("id"), reader.Get<uint>("year"), reader.GetString("name"), reader.Get<uint>("tournament_id"),
                reader.GetNull<uint>("slot_id"), reader.GetNull<uint>("draw_size"), reader.GetNull<uint>("surface_id"), reader.Get<byte>("indoor") > 0,
                reader.Get<uint>("level_id"), reader.Get<DateTime>("date_begin"), reader.Get<DateTime>("date_end"));
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/> between two years (both included).
        /// </summary>
        /// <param name="yearBegin">Year to begin.</param>
        /// <param name="yearEnd">Year to end.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        internal static List<EditionPivot> GetEditionsBetwwenTwoYears(uint yearBegin, uint yearEnd)
        {
            return GetList<EditionPivot>().Where(e => e.Year >= yearBegin && e.Year <= yearEnd).ToList();
        }

        /// <summary>
        /// Gets the ending date of the latest edition.
        /// </summary>
        /// <returns>Ending date of the latest edition; <c>Null</c> if no edition loaded.</returns>
        internal static DateTime? GetLatestEditionDateEnding()
        {
            return GetList<EditionPivot>().OrderByDescending(edition => edition.DateEnd).FirstOrDefault()?.DateEnd;
        }

        /// <summary>
        /// Gets a collection of <see cref="EditionPivot"/> to compute the ranking at a specified date.
        /// </summary>
        /// <param name="rankingVersion"><see cref="RankingVersionPivot"/></param>
        /// <param name="date">Ranking date; if not a monday, no results returned.</param>
        /// <param name="involvedPlayers">Out; involved <see cref="PlayerPivot"/> for the collection of <see cref="EditionPivot"/> returned.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        internal static List<EditionPivot> EditionsForRankingAtDate(RankingVersionPivot rankingVersion, DateTime date, out List<PlayerPivot> involvedPlayers)
        {
            if (date.DayOfWeek != DayOfWeek.Monday)
            {
                involvedPlayers = new List<PlayerPivot>();
                return new List<EditionPivot>();
            }

            var editionsRollingYear = GetList<EditionPivot>().Where(edition =>
                edition.DateEnd < date
                && edition.DateEnd >= date.AddDays(-1 * ConfigurationPivot.Default.RankingWeeksCount * 7)
                && GridPointPivot.GetRankableLevelList(rankingVersion).Contains(edition.Level)).ToList();

            if (rankingVersion.ContainsRule(RankingRulePivot.ExcludingRedundantTournaments))
            {
                // No redundant tournament, when slot is unknown (take the latest).
                // No redundant slot, regarding of the tournament (take the latest).
                editionsRollingYear = editionsRollingYear.Where(edition =>
                    !editionsRollingYear.Any(otherEdition =>
                        otherEdition.Slot == edition.Slot
                        && (otherEdition.Slot != null || (otherEdition.Tournament == edition.Tournament))
                        && otherEdition.DateEnd > edition.DateEnd
                    )
                ).ToList();
            }

            involvedPlayers = editionsRollingYear
                .SelectMany(edition =>
                    edition.Matches.SelectMany(match => match.Players))
                .Distinct()
                .Where(player => !player.IsJohnDoe)
                .ToList();

            return editionsRollingYear;
        }
    }
}
