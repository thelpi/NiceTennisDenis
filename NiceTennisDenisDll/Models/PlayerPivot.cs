using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class PlayerPivot : BasePivot
    {
        private const string JOHN_DOE_PROFILE_PIC_NAME = "unknown.jpg";

        #region Public properties

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; private set; }
        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; private set; }
        /// <summary>
        /// Laterality.
        /// </summary>
        public HandPivot? Hand { get; private set; }
        /// <summary>
        /// Birth date.
        /// </summary>
        public DateTime? BirthDate { get; private set; }
        /// <summary>
        /// Country code.
        /// </summary>
        public string CountryCode { get; private set; }
        /// <summary>
        /// Height.
        /// </summary>
        /// <remarks>In centimeters</remarks>
        public uint? Height { get; private set; }
        /// <summary>
        /// Path to profile picture.
        /// </summary>
        public string ProfilePicturePath { get; private set; }
        /// <summary>
        /// Inferred; player's name.
        /// </summary>
        public new string Name { get { return string.Concat(FirstName, " ", LastName); } }
        /// <summary>
        /// Inferred; unknown player y/n.
        /// </summary>
        public bool IsJohnDoe { get { return LastName.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase); } }
        /// <summary>
        /// Inferred; profile picture is a path to unknown y/n.
        /// </summary>
        public bool IsJohnDoeProfilePicture { get { return ProfilePicturePath.EndsWith(JOHN_DOE_PROFILE_PIC_NAME); } }

        #endregion

        private PlayerPivot(uint id, string firstName, string lastName, string hand, DateTime? birthDate, string countryCode, uint? height)
            : base(id, null, null)
        {
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Hand = ToHandPivot(hand?.Trim()?.ToUpperInvariant());
            BirthDate = birthDate;
            CountryCode = countryCode.Trim().ToUpperInvariant();
            Height = height;

            string profilePicturePath = System.IO.Path.Combine(DataMapper.Default.DatasDirectory, DataMapper.PROFILE_PIC_FOLDER_NAME,
                string.Concat(CleanForPicUri(FirstName), "_", CleanForPicUri(LastName), ".jpg"));

            ProfilePicturePath = System.IO.File.Exists(profilePicturePath) ? profilePicturePath :
                System.IO.Path.Combine(DataMapper.Default.DatasDirectory, DataMapper.PROFILE_PIC_FOLDER_NAME, JOHN_DOE_PROFILE_PIC_NAME);
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        private HandPivot? ToHandPivot(string hand)
        {
            switch (hand)
            {
                case "R":
                    return HandPivot.Right;
                case "L":
                    return HandPivot.Left;
                case "A":
                    return HandPivot.Ambidextrous;
                default:
                    return null;
            }
        }
        
        private string CleanForPicUri(string name)
        {
            return name.Trim().ToLowerInvariant().Replace(" ", "_");
        }

        #region Public methods

        /// <summary>
        /// Computes the age of the player at a given date.
        /// </summary>
        /// <param name="date">A <see cref="DateTime"/> greater than player's birth date.</param>
        /// <returns>Player's age at <paramref name="date"/>;
        /// <c>Null</c> if <see cref="BirthDate"/> has no value or if <paramref name="date"/> is lower than <see cref="BirthDate"/>.</returns>
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

        #endregion

        /// <summary>
        /// Creates an instance of <see cref="PlayerPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="PlayerPivot"/>.</returns>
        internal static PlayerPivot Create(MySqlDataReader reader)
        {
            return new PlayerPivot(reader.Get<uint>("id"), reader.GetString("first_name"), reader.GetString("last_name"),
                reader.IsDBNull("hand") ? null : reader.GetString("hand"),
                reader.GetNull<DateTime>("birth_date"), reader.GetString("country"), reader.GetNull<uint>("height"));
        }

        #region Public static methods

        /// <summary>
        /// Gets an <see cref="PlayerPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="PlayerPivot"/>. <c>Null</c> if not found.</returns>
        public static PlayerPivot Get(uint id)
        {
            return Get<PlayerPivot>(id);
        }

        /// <summary>
        /// Gets every instance of <see cref="PlayerPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="PlayerPivot"/>.</returns>
        public static IReadOnlyCollection<PlayerPivot> GetList()
        {
            return GetList<PlayerPivot>();
        }

        /// <summary>
        /// Gets every instance of <see cref="PlayerPivot"/> for a specified country code.
        /// </summary>
        /// <param name="countryCode">Country code.</param>
        /// <returns>Collection of <see cref="PlayerPivot"/>.</returns>
        public static IReadOnlyCollection<PlayerPivot> GetListByCountry(string countryCode)
        {
            return GetList<PlayerPivot>().Where(player => player.CountryCode.Equals(countryCode?.Trim()?.ToUpperInvariant())).ToList();
        }

        #endregion
    }
}
