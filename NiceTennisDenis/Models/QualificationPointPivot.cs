namespace NiceTennisDenis.Models
{
    public class QualificationPointPivot : BasePivot
    {
        public LevelPivot Level { get; private set; }
        public uint MinimalDrawSize { get; private set; }
        public uint Points { get; private set; }

        public override string ToString()
        {
            return $"{Level.Name}" + (MinimalDrawSize > 0 ? $" ({MinimalDrawSize})" : string.Empty);
        }
    }
}
