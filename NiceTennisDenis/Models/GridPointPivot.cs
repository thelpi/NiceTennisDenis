namespace NiceTennisDenis.Models
{
    public class GridPointPivot : BasePivot
    {
        public LevelPivot Level { get; set; }
        public RoundPivot Round { get; set; }
        public uint Points { get; set; }
        public uint ParticipationPoints { get; set; }
    }
}
