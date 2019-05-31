using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents an entry.
    /// </summary>
    public class EntryPivot : BasePivot
    {
        private EntryPivot(uint id, string code, string name) : base(id, code, name) { }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="EntryPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <returns>Instance of <see cref="EntryPivot"/>.</returns>
        internal static EntryPivot Create(MySqlDataReader reader)
        {
            return new EntryPivot(reader.Get<uint>("id"), reader.GetString("code"), reader.GetString("name"));
        }

        /// <summary>
        /// Gets an <see cref="EntryPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="EntryPivot"/>. <c>Null</c> if not found.</returns>
        public static EntryPivot Get(uint id)
        {
            return Get<EntryPivot>(id);
        }

        /// <summary>
        /// Gets an <see cref="EntryPivot"/> by its code.
        /// </summary>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="EntryPivot"/>. <c>Null</c> if not found.</returns>
        public static EntryPivot Get(string code)
        {
            return Get<EntryPivot>(code);
        }

        /// <summary>
        /// Gets every instance of <see cref="EntryPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="EntryPivot"/>.</returns>
        public static IReadOnlyCollection<EntryPivot> GetList()
        {
            return GetList<EntryPivot>();
        }
    }
}
