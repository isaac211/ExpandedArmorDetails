using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using EFT.UI;
using EFT.InventoryLogic;
using UnityEngine.Networking;
using System.Threading.Tasks;

using ItemAttribute = GClass2023;
using ItemAttributeCharacteristic = GClass2025;
using Comfort.Common;
using System.Reflection;
using ExpandedArmorDetails;
using static ExpandedArmorDetails.Attributes;
using ServerSettings = GClass1064;
using System.IO;
using ExpandedArmorDetails.Patches;
using Logger = BepInEx.Logging.Logger;

namespace ExpandedArmorDetails
{
    public class ExpandedArmorDetails
    {
        private static ModInformation _modInfo;
        public static ModInformation ModInfo
        {
            private set
            {
                _modInfo = value;
            }
            get
            {
                if (_modInfo == null)
                    _modInfo = ModInformation.Load();
                return _modInfo;
            }
        }

        public static Dictionary<Enum, Sprite> iconCache = new Dictionary<Enum, Sprite>();
        public static List<ItemAttribute> penAttributes = new List<ItemAttribute>();    // For refreshing armor class rating
        public static string modName = ModInfo.name;

        private static void Main()
        {
            new PatchManager().RunPatches();
            CacheIcons();
        }

        public static void CacheIcons()
        {
            _ = LoadTexture(ENewItemAttributeId.DurabilityFactor, Path.Combine(ModInfo.path, "res/armorDamage.png"));
            _ = LoadTexture(ENewItemAttributeId.EffectiveDurability, Path.Combine(ModInfo.path, "res/ricochet.png"));
        }

        public static async Task LoadTexture(Enum id, string path)
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
        static public void AddNewAttributes(ref List<ItemAttribute> attributes, ItemTemplate template)
        {

            if (template.Weight > 0)
            {
                ItemAttribute at_durabilityfact = new ItemAttribute(ENewItemAttributeId.DurabilityFactor.ToString(), template)
                {
                    Name = ENewItemAttributeId.DurabilityFactor.GetName(),
                    Base = () => template.DurabilityFactor,
                    StringValue = () => $"{(( template.DurabilityFactor+1) * 100).ToString()}%",
                    DisplayType = () => EItemAttributeDisplayType.Compact
                };


                ItemAttribute at_durabilityeff = new ItemAttribute(ENewItemAttributeId.EffectiveDurability.ToString(), template)
                {
                    Name = ENewItemAttributeId.EffectiveDurability.GetName(),
                    Base = () => template.DurabilityFactor,
                    StringValue = () =>
                    {
                        if (template.TotalArmorHealthPoints > 0)
                        {
                            return $"{(int)(template.TotalArmorHealthPoints / template.DurabilityFactor)}";
                        }

                        return 0;
                    },
                    DisplayType = () => EItemAttributeDisplayType.Compact
                };
                
                attributes.Add(at_durabilityfact);
                attributes.Add(at_durabilityeff);
            }

        }
    }
}