namespace NiceTennisDenisLib.Models
{
    /// <summary>
    /// Enumeration of statistic types for a match. 
    /// </summary>
    public enum StatisticPivot
    {
        /// <summary>
        /// Aces.
        /// </summary>
        Ace,
        /// <summary>
        /// Double faults.
        /// </summary>
        DoubleFault,
        /// <summary>
        /// Points on serve.
        /// </summary>
        ServePoint,
        /// <summary>
        /// First services in.
        /// </summary>
        FirstServeIn,
        /// <summary>
        /// Points won on first serve.
        /// </summary>
        FirstServeWon,
        /// <summary>
        /// Points won on second serve.
        /// </summary>
        SecondServeWon,
        /// <summary>
        /// Serve games won.
        /// </summary>
        ServeGame,
        /// <summary>
        /// Break point saved.
        /// </summary>
        BreakPointSaved,
        /// <summary>
        /// Break points faced.
        /// </summary>
        BreakPointFaced
    }
}
