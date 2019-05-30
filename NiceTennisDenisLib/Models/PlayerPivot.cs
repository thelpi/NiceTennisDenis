using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisLib.Models
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    public class PlayerPivot : BasePivot
    {
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
        /// Inferred; player's name.
        /// </summary>
        public new string Name { get { return string.Concat(FirstName, " ", LastName); } }

        private PlayerPivot(uint id, string firstName, string lastName, string hand, DateTime? birthDate, string countryCode, uint? height)
            : base(id, null, null)
        {
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Hand = ToHandPivot(hand?.Trim()?.ToUpperInvariant());
            BirthDate = birthDate;
            CountryCode = countryCode.Trim().ToUpperInvariant();
            Height = height;
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
            return GetList().Where(me => me.CountryCode.Equals(countryCode?.Trim()?.ToUpperInvariant())).ToList();
        }
    }
}
