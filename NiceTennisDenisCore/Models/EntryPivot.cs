using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Represents an entry.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class EntryPivot : BasePivot
    {
        private const string QUALIFICATION_CODE = "Q";

        #region Public properties

        /// <summary>
        /// Entry is for qualification y/n.
        /// </summary>
        public bool IsQualification
        {
            get
            {
                return Code == QUALIFICATION_CODE;
            }
        }

        #endregion

        private EntryPivot(uint id, string code, string name) : base(id, code, name) { }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        /// <summary>
        /// Creates an instance of <see cref="EntryPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="otherParameters">Other parameters.</param>
        /// <returns>Instance of <see cref="EntryPivot"/>.</returns>
        internal static EntryPivot Create(MySqlDataReader reader, params object[] otherParameters)
        {
            return new EntryPivot(reader.Get<uint>("id"), reader.GetString("code"), reader.GetString("name"));
        }

        #region Public static methods

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

        #endregion
    }
}
