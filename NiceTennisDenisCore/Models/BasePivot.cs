using System.Collections.Generic;
using System.Linq;

namespace NiceTennisDenisCore.Models
{
    /// <summary>
    /// Model for every pivot classes.
    /// </summary>
    public abstract class BasePivot
    {
        private static Dictionary<bool, List<BasePivot>> _instances = new Dictionary<bool, List<BasePivot>>
        {
            { true, new List<BasePivot>() },
            { false, new List<BasePivot>() }
        };

        #region Public properties

        /// <summary>
        /// Identifier.
        /// </summary>
        public uint Id { get; private set; }
        /// <summary>
        /// Code.
        /// </summary>
        /// <remarks>Might be irrelevant for the sub-class.</remarks>
        public string Code { get; private set; }
        /// <summary>
        /// Name.
        /// </summary>
        /// <remarks>Might be irrelevant for the sub-class.</remarks>
        public string Name { get; private set; }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id"><see cref="Id"/></param>
        /// <param name="code"><see cref="Code"/></param>
        /// <param name="name"><see cref="Name"/></param>
        protected BasePivot(uint id, string code, string name)
        {
            Id = id;
            Code = code?.Trim()?.ToUpperInvariant();
            Name = name?.Trim();
            _instances[GlobalAppConfig.IsWtaContext].Add(this);
        }

        /// <summary>
        /// Overriden; do nothing, but prevent inheritance from outside the assembly.
        /// </summary>
        internal abstract void AvoidInheritance();

        #region Public methods

        /// <summary>
        /// Overriden; string representation of the instance.
        /// </summary>
        /// <returns>String representation of the instance.</returns>
        public override string ToString()
        {
            List<string> stringElements = new List<string>();
            if (Id > 0)
            {
                stringElements.Add(Id.ToString());
            }
            if (!string.IsNullOrWhiteSpace(Code))
            {
                stringElements.Add(Code);
            }
            if (!string.IsNullOrWhiteSpace(Name))
            {
                stringElements.Add(Name);
            }

            return string.Join(" - ", stringElements);
        }

        #endregion

        /// <summary>
        /// Gets an <see cref="BasePivot"/> by its subtype and identifier.
        /// </summary>
        /// <typeparam name="T">Subtype of <see cref="BasePivot"/>.</typeparam>
        /// <param name="id">Identifier.</param>
        /// <returns>Instance of <see cref="BasePivot"/>. <c>Null</c> if not found.</returns>
        protected static T Get<T>(uint id) where T : BasePivot
        {
            return (T)_instances[GlobalAppConfig.IsWtaContext]
                .Where(me => me.GetType() == typeof(T))
                .FirstOrDefault(me => me.Id == id);
        }

        /// <summary>
        /// Gets an <see cref="BasePivot"/> by its subtype and code.
        /// </summary>
        /// <typeparam name="T">Subtype of <see cref="BasePivot"/>.</typeparam>
        /// <param name="code">Code.</param>
        /// <returns>Instance of <see cref="BasePivot"/>. <c>Null</c> if not found.</returns>
        protected static T Get<T>(string code) where T : BasePivot
        {
            return (T)_instances[GlobalAppConfig.IsWtaContext]
                .Where(me => me.GetType() == typeof(T))
                .FirstOrDefault(t => t.Code.Equals(code?.Trim()?.ToUpperInvariant()));
        }

        /// <summary>
        /// Gets every instances from a given subtype.
        /// </summary>
        /// <typeparam name="T">Subtype of <see cref="BasePivot"/>.</typeparam>
        /// <returns>Collection of <see cref="BasePivot"/>.</returns>
        protected static IReadOnlyCollection<T> GetList<T>() where T : BasePivot
        {
            return _instances[GlobalAppConfig.IsWtaContext]
                .Where(me => me.GetType() == typeof(T))
                .Cast<T>()
                .ToList();
        }
    }
}
