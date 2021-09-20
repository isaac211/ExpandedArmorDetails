using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine.Networking;
using System.Threading.Tasks;

using ItemAttribute = GClass1758;
using ItemAttributeCharacteristic = GClass1760;
using static NewItemAttributes;
using Logger = Aki.Common.Utils.Logger;
using Comfort.Common;
using Aki.Common.Utils.Patching;
using System.Reflection;

public static class NewItemAttributes
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

public class MunitionsExpert
{
    public static Dictionary<Enum, Sprite> iconCache = new Dictionary<Enum, Sprite>();
    public static List<ItemAttribute> penAttributes = new List<ItemAttribute>();    // For refreshing armor class rating
    public static string modName = "Faupi-MunitionsExpert";

    private static void Main()
    {
        PatcherUtil.Patch<MunitionsExpert_CachedAttributesPatch>();
        PatcherUtil.Patch<MunitionsExpert_StaticIconsPatch>();
        CacheIcons();
    }

    public static void CacheIcons()
    {
        iconCache.Add(ENewItemAttributeId.Damage, Resources.Load<Sprite>("characteristics/icons/icon_info_damage"));
        iconCache.Add(ENewItemAttributeId.FragmentationChance, Resources.Load<Sprite>("characteristics/icons/icon_info_shrapnelcount"));
        iconCache.Add(EItemAttributeId.LightBleedingDelta, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
        iconCache.Add(EItemAttributeId.HeavyBleedingDelta, Resources.Load<Sprite>("characteristics/icon_info_hydration"));
        iconCache.Add(ENewItemAttributeId.Penetration, Resources.Load<Sprite>("characteristics/icon_info_penetration"));
        _ = LoadImage(ENewItemAttributeId.ArmorDamage, $"{Aki.SinglePlayer.Utils.Config.BackendUrl}/files/armorDamage.png");    // Get this one from the server 
        _ = LoadImage(ENewItemAttributeId.RicochetChance, $"{Aki.SinglePlayer.Utils.Config.BackendUrl}/files/ricochet.png");    // Get this one from the server 
        // ^ (there's an argument that could be made about this being a pure client mod so including additional files on the server is probably dumb)
    }

    public static async Task LoadImage(Enum id, string path)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
        {
            uwr.SendWebRequest();

            while (!uwr.isDone)
                await Task.Delay(5);

            if (uwr.responseCode != 200)
            {
                Logger.LogError($"[{modName}] Request error {uwr.responseCode}: {uwr.error}");
            }
            else
            {
                // Get downloaded asset bundle
                Logger.LogInfo($"[{modName}] Retrieved texture! {id.ToString()} from {path}");
                Texture2D cachedTexture = DownloadHandlerTexture.GetContent(uwr);
                iconCache.Add(id, Sprite.Create(cachedTexture, new Rect(0, 0, cachedTexture.width, cachedTexture.height), new Vector2(0, 0)));
            }
        }
    }

    public static string RemoveSpaceBetweenValueAndPercent(string str)
    {
        return Regex.Replace(str, "(?<=[0-9]+)\\ (?=%)", string.Empty);
    }

    public static void FormatExistingAttributes(ref List<ItemAttribute> attributes, AmmoTemplate template)
    {
        foreach (ItemAttribute attr in attributes)
        {

            if ((EItemAttributeId)attr.Id == EItemAttributeId.CenterOfImpact)
            {
                float num = template.ammoAccr;
                ((ItemAttributeCharacteristic)attr).LessIsGood = false; // More accuracy = better
                attr.LabelVariations = EItemAttributeLabelVariations.Colored; // Red if bad, green if good
                attr.StringValue = () => $"{num}%"; // Pointless but at least it'll be consistent lol
            }

            if ((EItemAttributeId)attr.Id == EItemAttributeId.Recoil)
            {
                float num = template.ammoRec;
                ((ItemAttributeCharacteristic)attr).LessIsGood = true; //Less recoil = better
                attr.LabelVariations = EItemAttributeLabelVariations.Colored; //Red if bad, green if good
                attr.StringValue = () => $"{num}%"; // Missing percent sign
            }

            string str = RemoveSpaceBetweenValueAndPercent(attr.StringValue());
            attr.StringValue = () => str;
        }
    }

    static public void AddNewAttributes(ref List<ItemAttribute> attributes, AmmoTemplate template)
    {
        int projCount = template.ProjectileCount;
        int totalDamage = template.Damage * template.ProjectileCount;

        string damageStr = totalDamage.ToString(); // Total damage
        if (template.ProjectileCount > 1)
        {
            damageStr += $" ({template.Damage} x {template.ProjectileCount})";  // Add the "damage calculation" after total damage (damage per pellet * pellet count)
        }

        ItemAttribute at_damage = new ItemAttribute(ENewItemAttributeId.Damage)
        {
            Name = ENewItemAttributeId.Damage.GetName(),
            Base = () => totalDamage,
            StringValue = () => damageStr,
            DisplayType = () => EItemAttributeDisplayType.Compact
        };
        attributes.Add(at_damage);

        if (template.ArmorDamage > 0)
        {
            ItemAttribute at_armordmg = new ItemAttribute(ENewItemAttributeId.ArmorDamage)
            {
                Name = ENewItemAttributeId.ArmorDamage.GetName(),
                Base = () => template.ArmorDamage,
                StringValue = () => $"{(template.ArmorDamage).ToString()}%",
                DisplayType = () => EItemAttributeDisplayType.Compact
            };
            attributes.Add(at_armordmg);
        }

        if (template.PenetrationPower > 0)
        {
            string getStringValue()
            {
                int ratedClass = 0;

                if (!Singleton<GClass826>.Instantiated) { return $"CLASS_DATA_MISSING {template.PenetrationPower.ToString()}"; }
                GClass826.GClass842.GClass843[] classes = Singleton<GClass826>.Instance.Armor.ArmorClass;
                for (int i = 0; i < classes.Length; i++)
                {
                    if (classes[i].Resistance > template.PenetrationPower) continue;
                    ratedClass = Math.Max(ratedClass, i);
                }

                return $"{(ratedClass > 0 ? $"{"ME_class".Localized()} {ratedClass}" : "ME_noarmor".Localized())} ({template.PenetrationPower.ToString()})";
            }

            ItemAttribute at_pen = new ItemAttribute(ENewItemAttributeId.Penetration)
            {
                Name = ENewItemAttributeId.Penetration.GetName(),
                Base = () => template.PenetrationPower,
                StringValue = getStringValue,
                DisplayType = () => EItemAttributeDisplayType.Compact
            };
            attributes.Add(at_pen);
        }

        if (template.FragmentationChance > 0)
        {
            ItemAttribute at_frag = new ItemAttribute(ENewItemAttributeId.FragmentationChance)
            {
                Name = ENewItemAttributeId.FragmentationChance.GetName(),
                Base = () => template.FragmentationChance,
                StringValue = () => $"{(template.FragmentationChance * 100).ToString()}%",
                DisplayType = () => EItemAttributeDisplayType.Compact
            };
            attributes.Add(at_frag);
        }

        if (template.RicochetChance > 0)
        {
            ItemAttribute at_ricochet = new ItemAttribute(ENewItemAttributeId.RicochetChance)
            {
                Name = ENewItemAttributeId.RicochetChance.GetName(),
                Base = () => template.RicochetChance,
                StringValue = () => $"{(template.RicochetChance * 100).ToString()}%",
                DisplayType = () => EItemAttributeDisplayType.Compact
            };
            attributes.Add(at_ricochet);
        }
    }
}

class MunitionsExpert_StaticIconsPatch : GenericPatch<MunitionsExpert_StaticIconsPatch>
{
    public MunitionsExpert_StaticIconsPatch() : base("PatchPrefix", null, null, null) { }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(StaticIcons).GetMethod("GetAttributeIcon", BindingFlags.Instance | BindingFlags.Public);
    }

    private static bool PatchPrefix(ref Sprite __result, Enum id)
    {
        if (id == null || !MunitionsExpert.iconCache.ContainsKey(id)) return true;

        Sprite sprite = MunitionsExpert.iconCache[id];

        if (sprite != null)
        {
            __result = sprite;
            return false; //Skip the default getter
        }
        return true; //Continue with default getter
    }
}

class MunitionsExpert_CachedAttributesPatch : GenericPatch<MunitionsExpert_CachedAttributesPatch>
{
    public MunitionsExpert_CachedAttributesPatch() : base(null, "PatchPostfix", null, null) { }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
    }

    private static void PatchPostfix(ref AmmoTemplate __instance, ref List<ItemAttribute> __result)
    {
        bool converted = __result.Any(a => (ENewItemAttributeId)a.Id == ENewItemAttributeId.Damage); //Damage is pretty much guaranteed
        if (!converted) //If it has any of the custom attributes, it has all of them (the ones that apply ofc)
        {
            Logger.LogInfo($"[{MunitionsExpert.modName}] Adding attributes to ammo: {__instance.casingName}");
            MunitionsExpert.FormatExistingAttributes(ref __result, __instance);
            MunitionsExpert.AddNewAttributes(ref __result, __instance);
        }
    }
}