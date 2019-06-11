using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a specific version of ranking.
    /// </summary>
    /// <seealso cref="BasePivot"/>
    public sealed class RankingVersionPivot : BasePivot
    {
        /// <summary>
        /// Begin date of Open era (first monday).
        /// </summary>
        public static readonly DateTime OPEN_ERA_BEGIN = new DateTime(1968, 1, 1);

        #region Public properties

        /// <summary>
        /// Creation date.
        /// </summary>
        public DateTime CreationDate { get; private set; }
        /// <summary>
        /// Collection of <see cref="RankingRulePivot"/>.
        /// </summary>
        /// <remarks>Can't be <c>Null</c>.</remarks>
        public IReadOnlyCollection<RankingRulePivot> Rules { get; private set; }

        #endregion

        private RankingVersionPivot(uint id, DateTime creationDate, IEnumerable<uint> ruleIdList) : base(id, null, null)
        {
            CreationDate = creationDate;
            Rules = ruleIdList.Select(me => (RankingRulePivot)me).ToList();
        }

        /// <inheritdoc />
        internal override void AvoidInheritance() { }

        #region Public methods

        /// <summary>
        /// Checks if a specified ranking rule is applied to this ranking version.
        /// </summary>
        /// <param name="rule">The <see cref="RankingRulePivot"/> to check.</param>
        /// <returns><c>True</c> if contained; <c>False</c> otherwise.</returns>
        public bool ContainsRule(RankingRulePivot rule)
        {
            return Rules.Contains(rule);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Concat(Id, " - (", string.Join("|", Rules), ")");
        }

        #endregion

        /// <summary>
        /// Creates a <see cref="RankingVersionPivot"/> instance.
        /// </summary>
        /// <param name="reader">Opened <see cref="MySqlDataReader"/>.</param>
        /// <returns>Instance of <see cref="RankingVersionPivot"/>.</returns>
        internal static RankingVersionPivot Create(MySqlDataReader reader)
        {
            return new RankingVersionPivot(reader.Get<uint>("id"),
                reader.Get<DateTime>("creation_date"),
                reader.IsDBNull("rules_concat") ?
                    new List<uint>() :
                    reader.GetString("rules_concat").Split(',').Select(me => Convert.ToUInt32(me)));
        }

        #region Public static methods

        /// <summary>
        /// Gets an <see cref="RankingVersionPivot"/> by its identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="RankingVersionPivot"/>. <c>Null</c> if not found.</returns>
        public static RankingVersionPivot Get(uint id)
        {
            return Get<RankingVersionPivot>(id);
        }

        /// <summary>
        /// Gets every instance of <see cref="RankingVersionPivot"/>.
        /// </summary>
        /// <returns>Collection of <see cref="RankingVersionPivot"/>.</returns>
        public static IReadOnlyCollection<RankingVersionPivot> GetList()
        {
            return GetList<RankingVersionPivot>();
        }

        #endregion
    }
}
