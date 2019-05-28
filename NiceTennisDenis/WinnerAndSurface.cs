namespace NiceTennisDenis
{
    /// <summary>
    /// Represents the winner of a tournament.
    /// </summary>
    public struct WinnerAndSurface
    {
        /// <summary>
        /// Player's firstname.
        /// </summary>
        public string FirstName { get; private set; }
        /// <summary>
        /// Player's lastname.
        /// </summary>
        public string LastName { get; private set; }
        /// <summary>
        /// Tournament's surface identifier.
        /// </summary>
        public uint? SurfaceId { get; private set; }
        /// <summary>
        /// Indoor tournamenent.
        /// </summary>
        public bool Indoor { get; private set; }

        /// <summary>
        /// Inferred; Player's full name.
        /// </summary>
        public string Name { get { return string.Concat(FirstName, " ", LastName); } }
        /// <summary>
        /// Inferred; Player's profile pic.
        /// </summary>
        public string ProfileFileName { get { return string.Concat(CleanName(FirstName), "_", CleanName(LastName), ".jpg"); } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="firstName"><see cref="FirstName"/></param>
        /// <param name="lastName"><see cref="LastName"/></param>
        /// <param name="surfaceId"><see cref="SurfaceId"/></param>
        /// <param name="indoor"><see cref="Indoor"/></param>
        public WinnerAndSurface(string firstName, string lastName, uint? surfaceId, bool indoor)
        {
            FirstName = firstName;
            LastName = lastName;
            SurfaceId = surfaceId;
            Indoor = indoor;
        }

        // Cleans player's name for filename construction.
        private string CleanName(string name)
        {
            return name.Trim().ToLowerInvariant().Replace(" ", "_");
        }
    }
}
