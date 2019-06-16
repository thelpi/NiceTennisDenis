using System;

namespace NiceTennisDenis.Models
{
    public class PlayerPivot : BasePivot
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public HandPivot? Hand { get; set; }
        public DateTime? BirthDate { get; set; }
        public string CountryCode { get; set; }
        public uint? Height { get; set; }
        public string ProfilePicturePath { get; set; }
        public bool IsJohnDoe { get; set; }
        public bool IsJohnDoeProfilePicture { get; set; }
    }
}
