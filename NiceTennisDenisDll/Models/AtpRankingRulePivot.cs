namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Enumeration of rules used to compute ATP ranking.
    /// </summary>
    public enum AtpRankingRulePivot
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
        /// Six best performances only.
        /// </summary>
        SixBestPerformancesOnly,
        /// <summary>
        /// Excluding redundant tournaments.
        /// </summary>
        ExcludingRedundantTournaments
    }
}
