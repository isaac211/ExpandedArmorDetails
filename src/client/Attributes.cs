﻿using System;
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
            Damage,
            ArmorDamage,
            Penetration,
            FragmentationChance,
            RicochetChance
        }

        public static string GetName(this ENewItemAttributeId id)
        {
            switch (id)
            {
                case ENewItemAttributeId.Damage:
                    return "DAMAGE";
                case ENewItemAttributeId.ArmorDamage:
                    return "ARMOR DAMAGE";
                case ENewItemAttributeId.Penetration:
                    return "PENETRATION";
                case ENewItemAttributeId.FragmentationChance:
                    return "FRAGMENTATION CHANCE";
                case ENewItemAttributeId.RicochetChance:
                    return "RICOCHET CHANCE";
                default:
                    return id.ToString();
            }
        }
    }
}
