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
    }
}
