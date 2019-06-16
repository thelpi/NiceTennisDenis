using System;

namespace NiceTennisDenis.Models
{
    public class RankingPivot
    {
        public RankingVersionPivot Version { get; set; }
        public PlayerPivot Player { get; set; }
        public DateTime Date { get; set; }
        public uint Points { get; set; }
        public uint Ranking { get; set; }
        public uint Editions { get; set; }
        public string PlayerName { get; set; }
        public string PlayerProfilePicturePath { get; set; }
    }
}
