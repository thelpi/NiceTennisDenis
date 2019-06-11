namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Enumeration of rules used to compute ranking.
    /// </summary>
    public enum RankingRulePivot
    {
        /// <summary>
        /// Including olympic games.
        /// </summary>
        IncludingOlympicGames = 1,
        /// <summary>
        /// Including challenger matches.
        /// </summary>
        IncludingChallengerMatches,
        /// <summary>
        /// Including qualification bonus.
        /// </summary>
        IncludingQualificationBonus,
        /// <summary>
        /// Best performances only.
        /// </summary>
        BestPerformancesOnly,
        /// <summary>
        /// Excluding redundant tournaments.
        /// </summary>
        ExcludingRedundantTournaments
    }
}
