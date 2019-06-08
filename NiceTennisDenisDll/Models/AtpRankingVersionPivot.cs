using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a specific version of ATP ranking.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class AtpRankingVersionPivot : BasePivot
    {
        #region Public properties

        /// <summary>
        /// Creation date.
        /// </summary>
        public DateTime CreationDate { get; private set; }
        /// <summary>
        /// Collection of <see cref="AtpRankingRulePivot"/>.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public IReadOnlyCollection<AtpRankingRulePivot> Rules { get; private set; }

        #endregion

        private AtpRankingVersionPivot(uint id, DateTime creationDate, IEnumerable<uint> ruleIdList) : base(id, null, null)
        {
            CreationDate = creationDate;
            Rules = ruleIdList.Select(me => (AtpRankingRulePivot)me).ToList();
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        #region Public methods

        /// <summary>
        /// Checks if a specified ranking rule is applied to this ranking version.
        /// </summary>
        /// <param name="rule">The <see cref="AtpRankingRulePivot"/> to check.</param>
        /// <returns><c>True</c> if contained; <c>False</c> otherwise.</returns>
        public bool ContainsRule(AtpRankingRulePivot rule)
        {
            return Rules.Contains(rule);
        }

        #endregion

        /// <summary>
        /// Creates a <see cref="AtpRankingVersionPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="AtpRankingVersionPivot"/>.</returns>
        internal static AtpRankingVersionPivot Create(MySqlDataReader reader)
        {
            return new AtpRankingVersionPivot(reader.Get<uint>("id"),
                reader.Get<DateTime>("creation_date"),
                reader.IsDBNull("rules_concat") ?
                    new List<uint>() :
                    reader.GetString("rules_concat").Split(',').Select(me => Convert.ToUInt32(me)));
        }

        #region Public static methods

        /// <summary>
        /// Gets an <see cref="AtpRankingVersionPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="AtpRankingVersionPivot"/>. <c>Null</c> if not found.</returns>
        public static AtpRankingVersionPivot Get(uint id)
        {
            return Get<AtpRankingVersionPivot>(id);
        }

        #endregion
    }
}
