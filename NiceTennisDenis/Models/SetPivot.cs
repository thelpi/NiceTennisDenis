namespace NiceTennisDenis.Models
{
    public class SetPivot
    {
        public uint WinnerGame { get; set; }
        public uint LoserGame { get; set; }
        public uint? TieBreak { get; set; }

        public override string ToString()
        {
            return $"{WinnerGame}-{LoserGame}" + (TieBreak.HasValue ? $" {TieBreak.Value}" : string.Empty);
        }
    }
}
