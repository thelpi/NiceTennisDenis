namespace NiceTennisDenis
{
    /// <summary>
    /// Represents a tournament slot.
    /// </summary>
    public struct Slot
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        public uint Id { get; private set; }
        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Competition level's name.
        /// </summary>
        public string LevelName { get; private set; }
        /// <summary>
        /// Display order.
        /// </summary>
        public uint DisplayOrder { get; private set; }

        /// <summary>
        /// Inferred; Full description.
        /// </summary>
        public string Description { get { return string.Concat(Name, "(", LevelName, ")"); } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id"><see cref="Id"/></param>
        /// <param name="name"><see cref="Name"/></param>
        /// <param name="levelName"><see cref="LevelName"/></param>
        /// <param name="displayOrder"><see cref="DisplayOrder"/></param>
        public Slot(uint id, string name, string levelName, uint displayOrder)
        {
            Id = id;
            Name = name;
            LevelName = levelName;
            DisplayOrder = displayOrder;
        }
    }
}
