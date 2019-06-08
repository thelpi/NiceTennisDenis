using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a tournament's edition.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class EditionPivot : BasePivot
    {
        private readonly List<MatchPivot> _matches;
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
        public IReadOnlyCollection<MatchPivot> Matches { get { return _matches; } }
        /// <summary>
        /// Inferred; mandatory for ATP ranking y/n.
        /// </summary>
        public bool MandatoryAtp { get { return Level.MandatoryAtp && Slot?.IsMonteCarlo != true; } }
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
                else if (_matches.Count == 0)
                {
                    return 0;
                }
                else
                {
                    if (!_realDrawSize.HasValue)
                    {
                        var firstRound = _matches.OrderByDescending(me => me.Round.PlayersCount).First().Round;

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

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        #region Public methods

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Id} - {Year} - {Name} - {Level.Name}";
        }

        /// <summary>
        /// Checks if the specified player is involved in this edition.
        /// </summary>
        /// <param name="player">The <see cref="PlayerPivot"/> to check.</param>
        /// <returns><c>True</c> if involved in this edition; <c>False</c> otherwise.</returns>
        public bool InvolvePlayer(PlayerPivot player)
        {
            return player != null && Matches.Any(match => match.Players.Contains(player));
        }

        /// <summary>
        /// Checks if the specified player has played as a qualifier in this edition.
        /// </summary>
        /// <param name="player">The <see cref="PlayerPivot"/> to check.</param>
        /// <returns><c>True</c> if has played as a qualifier in this edition; <c>False</c> otherwise.</returns>
        public bool PlayerIsQualified(PlayerPivot player)
        {
            return player != null && Matches.Any(match =>
                (match.Winner == player && match.WinnerEntry?.IsQualification == true)
                || (match.Loser == player && match.LoserEntry?.IsQualification == true)
            );
        }

        /// <summary>
        /// Gets the number of points gained by a specified player for this edition. The gain might vary regarding of the ruleset.
        /// </summary>
        /// <param name="player">A <see cref="PlayerPivot"/></param>
        /// <param name="atpRankingVersion">A <see cref="AtpRankingRulePivot"/> (ruleset of current ATP ranking).</param>
        /// <returns>Number of points for this player at this edition; 0 if any argument is <c>Null</c>.</returns>
        public uint GetPlayerPoints(PlayerPivot player, AtpRankingVersionPivot atpRankingVersion)
        {
            uint points = 0;

            if (player == null || atpRankingVersion == null)
            {
                return points;
            }

            // If qualifcation rule applies and player comes from qualifications for this edition.
            if (atpRankingVersion.ContainsRule(AtpRankingRulePivot.IncludingQualificationBonus) && PlayerIsQualified(player))
            {
                points = AtpQualificationPivot.GetByLevelAndDrawSize(Level.Id, DrawSize)?.Points ?? 0;
            }

            // Loop matches played by the current player, take the first by importance + every round robin matches.
            bool bestRoundDone = false;
            foreach (var match in Matches.Where(match => match.Winner == player).OrderBy(match => match.Round.Importance))
            {
                if (!bestRoundDone || match.Round.IsRoundRobin)
                {
                    points += match.AtpPointGrid?.Points ?? 0;
                    bestRoundDone = true;
                }
            }

            // No win : checks if the lose has participation points.
            if (!bestRoundDone)
            {
                var lose = Matches.Single(match => match.Loser == player && !match.Round.IsRoundRobin);
                points += lose.AtpPointGrid?.ParticipationPoints ?? 0;
            }

            return points;
        }

        #endregion

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

        #region Public static methods

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
            return GetList<EditionPivot>().Where(edition => edition.Year == year).ToList();
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/> for a specified <see cref="TournamentPivot"/>.
        /// </summary>
        /// <param name="tournamentId"><see cref="TournamentPivot"/> identifier.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> GetListByTournament(uint tournamentId)
        {
            return GetList<EditionPivot>().Where(edition => edition.Tournament.Id == tournamentId).ToList();
        }

        /// <summary>
        /// Gets every instance of <see cref="EditionPivot"/> for a specified <see cref="SlotPivot"/>.
        /// </summary>
        /// <param name="slotId"><see cref="SlotPivot"/> identifier.</param>
        /// <returns>Collection of <see cref="EditionPivot"/>.</returns>
        public static IReadOnlyCollection<EditionPivot> GetListBySlot(uint slotId)
        {
            return GetList<EditionPivot>().Where(edition => edition.Slot?.Id == slotId).ToList();
        }

        /// <summary>
        /// Gets the ending date of the latest edition.
        /// </summary>
        /// <returns>Ending date of the latest edition; <c>Null</c> if no edition loaded.</returns>
        public static DateTime? GetLatestsEditionDateEnding()
        {
            return GetList<EditionPivot>().OrderByDescending(edition => edition.DateEnd).FirstOrDefault()?.DateEnd;
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

            var editionsRollingYear = GetList().Where(edition =>
                edition.DateEnd < date
                && edition.DateEnd >= date.AddDays(-52 * 7) // Days in a week * weeks in a year.
                && AtpGridPointPivot.GetRankableLevelList(atpRankingVersion).Contains(edition.Level)).ToList();

            if (atpRankingVersion.ContainsRule(AtpRankingRulePivot.ExcludingRedundantTournaments))
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

            involvedPlayers = editionsRollingYear.SelectMany(edition => edition.Matches.SelectMany(match => match.Players)).Distinct().ToList();
            return editionsRollingYear;
        }

        #endregion
    }
}
