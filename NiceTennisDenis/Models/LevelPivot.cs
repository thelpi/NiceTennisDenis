namespace NiceTennisDenis.Models
{
    public class LevelPivot : BasePivot
    {
        public uint DisplayOrder { get; set; }
        public bool Mandatory { get; set; }
        public bool IsOlympicGames { get; set; }
    }
}
