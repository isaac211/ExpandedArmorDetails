using Aki.Common;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection;
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
            this._patches = new List<ModulePatch>
			{
				new ExpandedArmorDetails_CachedAttributesPatch(),
                new ExpandedArmorDetails_StaticIconsPatch()
			};
		}
        
		public void RunPatches()
		{
			foreach(ModulePatch patch in _patches)
            {
                patch.Enable();
            }
		}

		private readonly List<ModulePatch> _patches;
	}

    class ExpandedArmorDetails_StaticIconsPatch : ModulePatch
    {
        // public ExpandedArmorDetails_StaticIconsPatch() : base(typeof(ExpandedArmorDetails_StaticIconsPatch), null, "PatchPrefix", null, null, null) { }
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Instance | BindingFlags.Public);
        }

        protected override PatchPrefix()
        {
            
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

    class ExpandedArmorDetails_CachedAttributesPatch : ModulePatch
    {
        public ExpandedArmorDetails_CachedAttributesPatch() : base(typeof(ExpandedArmorDetails_CachedAttributesPatch), null, null, "PatchPostfix", null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void PatchPostfix(ref ItemTemplate __instance, ref List<ItemAttribute> __result)
        {
            // TODO verify if we can only retrieve the material type, then fetch that instead and create a table that matches
            // known info on wikis for durability factor if it is the cause
            bool converted = __result.Any(a => (ENewItemAttributeId)a.Id == ENewItemAttributeId.DurabilityFactor); // FIXME Assume durability factor is guaranteed
            if (!converted) //If it has any of the custom attributes, it has all of them (the ones that apply ofc)
            {
                ExpandedArmorDetails.AddNewAttributes(ref __result, __instance);
            }
        }
    }
}
