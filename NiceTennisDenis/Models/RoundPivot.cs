namespace NiceTennisDenis.Models
{
    public class RoundPivot : BasePivot
    {
        public uint PlayersCount { get; set; }
        public uint Importance { get; set; }
        public bool IsBronzeReward { get; set; }
        public bool IsRoundRobin { get; set; }
        public bool IsFinal { get; set; }
    }
}
