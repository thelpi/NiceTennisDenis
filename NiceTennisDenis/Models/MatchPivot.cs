using System.Collections.Generic;

namespace NiceTennisDenis.Models
{
    public class MatchPivot : BasePivot
    {
        public uint EditionId { get; set; }
        public uint MatchNumber { get; set; }
        public BestOfPivot BestOf { get; set; }
        public RoundPivot Round { get; set; }
        public uint? Minutes { get; set; }
        public PlayerPivot Winner { get; set; }
        public uint? WinnerSeed { get; set; }
        public EntryPivot WinnerEntry { get; set; }
        public uint? WinnerRank { get; set; }
        public uint? WinnerRankPoints { get; set; }
        public PlayerPivot Loser { get; set; }
        public uint? LoserSeed { get; set; }
        public EntryPivot LoserEntry { get; set; }
        public uint? LoserRank { get; set; }
        public uint? LoserRankPoints { get; set; }
        public bool Walkover { get; set; }
        public bool Retirement { get; set; }
        public bool Disqualification { get; set; }
        public bool Unfinished { get; set; }
        public IEnumerable<SetPivot> Sets { get; set; }
        public IDictionary<StatisticPivot, uint?> WinnerStatistics { get; set; }
        public IDictionary<StatisticPivot, uint?> LoserStatistics { get; set; }
        public string RawSuperTieBreak { get; set; }
        public GridPointPivot PointGrid { get; set; }
        public IEnumerable<PlayerPivot> Players { get; set; }

        public override string ToString()
        {
            return $"{Id} - {EditionId} - {Round.Name} - {Winner.Name} - {Loser.Name}";
        }
    }
}
