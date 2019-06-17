using System;
using System.Collections.Generic;

namespace NiceTennisDenis.Models
{
    public class EditionPivot : BasePivot
    {
        public uint Year { get; set; }
        public TournamentPivot Tournament { get; set; }
        public SlotPivot Slot { get; set; }
        public SurfacePivot? Surface { get; set; }
        public bool Indoor { get; set; }
        public LevelPivot Level { get; set; }
        public DateTime DateBegin { get; set; }
        public DateTime DateEnd { get; set; }
        public IEnumerable<MatchPivot> Matches { get; set; }
        public bool Mandatory { get; set; }
        public uint DrawSize { get; set; }
        public RoundPivot FirstRound { get; set; }
        public MatchPivot Final { get; set; }

        public override string ToString()
        {
            return $"{Id} - {Year} - {Name} - {Level.Name}";
        }
    }
}
