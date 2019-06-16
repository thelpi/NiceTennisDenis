using System;
using System.Collections.Generic;

namespace NiceTennisDenis.Models
{
    public class RankingVersionPivot : BasePivot
    {
        public DateTime CreationDate { get; set; }
        public IEnumerable<RankingRulePivot> Rules { get; set; }
    }
}
