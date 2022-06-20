using Aki.Common;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ExpandedArmorDetails.Attributes;
using ItemAttribute = GClass2023;

namespace ExpandedArmorDetails.Patches
{
	public class PatchManager
	{
		public PatchManager()
		{
			this._patches = new PatchList
			{
				new ExpandedArmorDetails_CachedAttributesPatch(),
                new ExpandedArmorDetails_StaticIconsPatch()
			};
		}

		public void RunPatches()
		{
			this._patches.EnableAll();
		}

		private readonly PatchList _patches;
	}

    class ExpandedArmorDetails_StaticIconsPatch : Patch
    {
        public ExpandedArmorDetails_StaticIconsPatch() : base(typeof(ExpandedArmorDetails_StaticIconsPatch), null, "PatchPrefix", null, null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool PatchPrefix(ref Sprite __result, Enum id)
        {
            if (id == null || !ExpandedArmorDetails.iconCache.ContainsKey(id)) return true;

            Sprite sprite = ExpandedArmorDetails.iconCache[id];

            if (sprite != null)
            {
                __result = sprite;
                return false; //Skip the default getter
            }
            return true; //Continue with default getter
        }
    }

    class ExpandedArmorDetails_CachedAttributesPatch : Patch
    {
        public ExpandedArmorDetails_CachedAttributesPatch() : base(typeof(ExpandedArmorDetails_CachedAttributesPatch), null, null, "PatchPostfix", null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void PatchPostfix(ref AmmoTemplate __instance, ref List<ItemAttribute> __result)
        {
            bool converted = __result.Any(a => (ENewItemAttributeId)a.Id == ENewItemAttributeId.Damage); //Damage is pretty much guaranteed
            if (!converted) //If it has any of the custom attributes, it has all of them (the ones that apply ofc)
            {
                ExpandedArmorDetails.FormatExistingAttributes(ref __result, __instance);
                ExpandedArmorDetails.AddNewAttributes(ref __result, __instance);
            }
        }
    }
}
