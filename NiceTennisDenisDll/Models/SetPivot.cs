using MySql.Data.MySqlClient;

namespace NiceTennisDenisDll.Models
{
    /// <summary>
    /// Represents a set.
    /// </summary>
    public sealed class SetPivot
    {
        #region Public properties

        /// <summary>
        /// Games count by the winner of the match.
        /// </summary>
        public uint WinnerGame { get; private set; }
        /// <summary>
        /// Games count by the loser of the match.
        /// </summary>
        public uint LoserGame { get; private set; }
        /// <summary>
        /// Tie-break.
        /// </summary>
        /// <remarks>Points mark for the loser of the set.</remarks>
        public uint? TieBreak { get; private set; }

        #endregion

        private SetPivot(uint winnerGame, uint loserGame, uint? tieBreak)
        {
            WinnerGame = winnerGame;
            LoserGame = loserGame;
            TieBreak = tieBreak;
        }

        /// <summary>
        /// Creates an instance of <see cref="SetPivot"/>.
        /// </summary>
        /// <param name="reader">Opened data reader.</param>
        /// <param name="setNo">Set numero.</param>
        /// <returns>Instance of <see cref="SetPivot"/>. <c>Null</c> if there is no data for this set.</returns>
        internal static SetPivot Create(MySqlDataReader reader, int setNo)
        {
            uint? wGame = reader.Get<uint>($"w_set_{setNo}");
            uint? lGame = reader.Get<uint>($"l_set_{setNo}");
            if (!lGame.HasValue || !wGame.HasValue)
            {
                return null;
            }

            return new SetPivot(wGame.Value, lGame.Value, reader.GetNull<uint>($"tb_set_{setNo}"));
        }

        #region Public methods

        /// <summary>
        /// String representation of the instance.
        /// </summary>
        /// <returns>String representation of the instance.</returns>
        public override string ToString()
        {
            return $"{WinnerGame}-{LoserGame}" + (TieBreak.HasValue ? $" {TieBreak.Value}" : string.Empty);
        }

        #endregion
    }
}
