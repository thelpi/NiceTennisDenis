using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a specific version of ATP ranking.
    /// </summary>
    public class AtpRankingVersionPivot : BasePivot
    {
        /// <summary>
        /// Creation date.
        /// </summary>
        public DateTime CreationDate { get; private set; }
        /// <summary>
        /// Collection of <see cref="AtpRankingRulePivot"/>.
        /// </summary>
        public IReadOnlyCollection<AtpRankingRulePivot> Rules { get; private set; }

        private AtpRankingVersionPivot(uint id, DateTime creationDate, IEnumerable<uint> ruleIdList) : base(id, null, null)
        {
            CreationDate = creationDate;
            Rules = ruleIdList.Select(me => (AtpRankingRulePivot)me).ToList();
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

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

        /// <summary>
        /// Gets an <see cref="AtpRankingVersionPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="AtpRankingVersionPivot"/>. <c>Null</c> if not found.</returns>
        public static AtpRankingVersionPivot Get(uint id)
        {
            return Get<AtpRankingVersionPivot>(id);
        }
    }
}
