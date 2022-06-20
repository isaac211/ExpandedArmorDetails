using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedArmorDetails
{
    public static class Attributes
    {
        public enum ENewItemAttributeId
        {
            DurabilityFactor,
            EffectiveDurability
        }

        public static string GetName(this ENewItemAttributeId id)
        {
            switch (id)
            {
                case ENewItemAttributeId.DurabilityFactor:
                    return "DURABILITY FACTOR";
                case ENewItemAttributeId.EffectiveDurability:
                    return "EFFECTIVE DURABILITY";
                default:
                    return id.ToString();
            }
        }
    }
}
