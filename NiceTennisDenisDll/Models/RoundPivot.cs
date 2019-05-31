using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a round.
    /// </summary>
    public class RoundPivot : BasePivot
    {
        public const string FINAL = "F";

        private RoundPivot(uint id, string code, string name) : base(id, code, name) { }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="RoundPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>.</returns>
        internal static RoundPivot Create(MySqlDataReader reader)
        {
            return new RoundPivot(reader.Get<uint>("id"), reader.GetString("code"), reader.GetString("name"));
        }

        /// <summary>
        /// Gets an <see cref="RoundPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        public static RoundPivot Get(uint id)
        {
            return Get<RoundPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="RoundPivot"/> by its code.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="RoundPivot"/>. <c>Null</c> if not found.</returns>
        public static RoundPivot Get(string code)
        {
            return Get<RoundPivot>(code);
        }

        /// <summary>
        /// Gets every instance of <see cref="RoundPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="RoundPivot"/>.</returns>
        public static IReadOnlyCollection<RoundPivot> GetList()
        {
            return GetList<RoundPivot>();
        }
    }
}
