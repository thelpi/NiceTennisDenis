using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents a tournament.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class TournamentPivot : BasePivot
    {
        #region Public properties

        /// <summary>
        /// Collection of known codes.
        /// </summary>
        public List<string> KnownCodes { get; }

        #endregion

        private TournamentPivot(uint id, string name, IEnumerable<string> knownCodes) : base(id, null, name)
        {
            KnownCodes = new List<string>(knownCodes);
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="TournamentPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="TournamentPivot"/>.</returns>
        internal static TournamentPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            IEnumerable<string> codes = reader.GetString("known_codes")
                .Split(';')
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant());

            return new TournamentPivot(reader.Get<uint>("id"), reader.GetString("name"), codes);
        }
    }
}
