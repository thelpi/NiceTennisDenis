namespace NiceTennisDenis.Models
{
    public class SlotPivot : BasePivot
    {
        public uint DisplayOrder { get; set; }
        public LevelPivot Level { get; set; }
        public bool? Mandatory { get; set; }
    }
}
