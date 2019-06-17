using System;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Configuration pivot.
    /// </summary>
    public sealed class ConfigurationPivot
    {
        #region Public properties

        /// <summary>
        /// Count of best performances used to compute ranking.
        /// </summary>
        public uint BestPerformancesCountForRanking { get; private set; }
        /// <summary>
        /// Count of weeks included in ranking.
        /// </summary>
        public uint RankingWeeksCount { get; private set; }

        #endregion

        private static ConfigurationPivot _default = null;

        /// <summary>
        /// Singleton instance of <see cref="ConfigurationPivot"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Messages.ConfigurationNotInitializedMessage"/></exception>
        internal static ConfigurationPivot Default
        {
            get
            {
                if (_default == null)
                {
                    throw new InvalidOperationException();
                }

                return _default;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="ConfigurationPivot"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static ConfigurationPivot Initialize(MySqlDataReader reader)
        {
            _default = new ConfigurationPivot
            {
                BestPerformancesCountForRanking = reader.Get<uint>("best_performances_count_for_ranking"),
                RankingWeeksCount = reader.Get<uint>("ranking_weeks_count")
            };

            return _default;
        }
    }
}
