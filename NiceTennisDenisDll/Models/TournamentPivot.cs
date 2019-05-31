using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a tournament.
    /// </summary>
    public class TournamentPivot : BasePivot
    {
        private readonly List<string> _knownCodes = new List<string>();

        /// <summary>
        /// Collection of known codes.
        /// </summary>
        public IReadOnlyCollection<string> KnownCodes { get { return _knownCodes; } }

        private TournamentPivot(uint id, string name, IEnumerable<string> knownCodes) : base(id, null, name)
        {
            _knownCodes = new List<string>(knownCodes);
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="TournamentPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="TournamentPivot"/>.</returns>
        internal static TournamentPivot Create(MySqlDataReader reader)
        {
            IEnumerable<string> codes = reader.GetString("known_codes")
                .Split(';')
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant());

            return new TournamentPivot(reader.Get<uint>("id"), reader.GetString("name"), codes);
        }

        /// <summary>
        /// Gets an <see cref="TournamentPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="TournamentPivot"/>. <c>Null</c> if not found.</returns>
        public static TournamentPivot Get(uint id)
        {
            return Get<TournamentPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="TournamentPivot"/> by one of its code.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="TournamentPivot"/>. <c>Null</c> if not found.</returns>
        public static TournamentPivot Get(string code)
        {
            return GetList().FirstOrDefault(me => me.Code.Equals(code?.Trim()?.ToUpperInvariant()));
        }

        /// <summary>
        /// Gets every instance of <see cref="TournamentPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="TournamentPivot"/>.</returns>
        public static IReadOnlyCollection<TournamentPivot> GetList()
        {
            return GetList<TournamentPivot>();
        }
    }
}
