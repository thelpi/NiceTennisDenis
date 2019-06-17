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

        public int? Age(DateTime date)
        {
            if (!BirthDate.HasValue || BirthDate.Value > date)
            {
                return null;
            }

            int age = date.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > date.AddYears(-age))
            {
                age--;
            }

            return age;
        }

    }
}
