using System.Collections.Generic;

namespace NiceTennisDenis.Models
{
    public class BasePivot
    {
        public uint Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            var stringElements = new List<string>();
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
    }
}
