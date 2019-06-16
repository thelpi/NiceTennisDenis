using System.Collections.Generic;

namespace NiceTennisDenis.Models
{
    public class TournamentPivot : BasePivot
    {
        public IEnumerable<string> KnownCodes { get; set; }
    }
}
