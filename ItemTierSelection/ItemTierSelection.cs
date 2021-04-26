using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using ItemCatalog = On.RoR2.ItemCatalog;
using EquipmentCatalog = On.RoR2.EquipmentCatalog;

namespace Theray070696
{
    [BepInPlugin("io.github.Theray070696.itemtierselection", "Item Tier Selection", "2.1.4")]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.iDeathHD.ItemLib", BepInDependency.DependencyFlags.SoftDependency)]
    public class ItemTierSelection : BaseUnityPlugin
    {
        private static readonly string[] _invalidConfigChars = new string[8]
        {
            "=",
            "\n",
            "\t",
            "\\",
            "\"",
            "'",
            "[",
            "]"
        };
        
        private const string ModPrefix = "@Theray070696.itemtierselection.textures";
        
        public void Awake()
        {
            ItemCatalog.SetItemDefs += OnSetItemDefs;
            EquipmentCatalog.RegisterEquipment += OnRegisterEquipment;
        }
        
        private void OnSetItemDefs(ItemCatalog.orig_SetItemDefs orig, ItemDef[] itemDefs)
        {
            ItemDef[] newItemDefs = new ItemDef[itemDefs.Length];
            int i = 0;

            foreach(ItemDef itemDef in itemDefs)
            {
                if(itemDef == null)
                {
                    i++;
                    continue;
                }
                
                ItemTier currTier = itemDef.tier;
                int defTier;

                switch(currTier)
                {
                    case ItemTier.Tier1:
                    {
                        defTier = 1;
                        break;
                    }

                    case ItemTier.Tier2:
                    {
                        defTier = 2;
                        break;
                    }

                    case ItemTier.Tier3:
                    {
                        defTier = 3;
                        break;
                    }

                    case ItemTier.Lunar:
                    {
                        defTier = 4;
                        break;
                    }

                    case ItemTier.Boss:
                    {
                        defTier = 5;
                        break;
                    }

                    default:
                    {
                        defTier = 0;
                        break;
                    }
                }
                
                string itemName = itemDef.nameToken;
                if(itemName == null)
                    itemName = itemDef.itemIndex.ToString();

                itemName = itemName.ToUpper(CultureInfo.InvariantCulture);
                foreach(string ch in _invalidConfigChars)
                {
                    itemName = itemName.Replace(ch, string.Empty);
                }

                ConfigWrapper<int> c = Config.Wrap("Item Tiers", itemName + " tier",
                    "Tier of this item. 0 is no tier, 1 is white, 2 is green, 3 is red, 4 is lunar", defTier);

                int newTierNum = c.Value;
                if(newTierNum == defTier)
                {
                    newItemDefs[i] = itemDef;
                    i++;
                    continue;
                }

                switch(newTierNum)
                {
                    case 1:
                    {
                        itemDef.tier = ItemTier.Tier1;
                        break;
                    }

                    case 2:
                    {
                        itemDef.tier = ItemTier.Tier2;
                        break;
                    }

                    case 3:
                    {
                        itemDef.tier = ItemTier.Tier3;
                        break;
                    }

                    case 4:
                    {
                        itemDef.tier = ItemTier.Lunar;
                        break;
                    }

                    default:
                    {
                        if(newTierNum == 5 && currTier == ItemTier.Boss)
                        {
                            break;
                        }

                        itemDef.tier = ItemTier.NoTier;
                        break;
                    }
                }
                
                Logger.LogInfo("Changing tier of " + itemName + ".");

                newItemDefs[i] = itemDef;

                i++;
            }

            orig.Invoke(newItemDefs);
        }

        private void OnRegisterEquipment(EquipmentCatalog.orig_RegisterEquipment orig, EquipmentIndex equipmentIndex, EquipmentDef equipmentDef)
        {
            if(equipmentDef == null || !equipmentDef.canDrop)
            {
                orig.Invoke(equipmentIndex, equipmentDef);
                return;
            }

            int defTier = equipmentDef.isLunar ? 2 : 1;

            string equipmentNameToken = equipmentDef.nameToken;
            if(equipmentNameToken == null)
                equipmentNameToken = equipmentIndex.ToString();

            string upper = equipmentNameToken.ToUpper(CultureInfo.InvariantCulture);

            foreach(string ch in _invalidConfigChars)
            {
                upper = upper.Replace(ch, string.Empty);
            }

            ConfigWrapper<int> c = Config.Wrap("Equipment Tiers", upper + " tier",
                "Tier of this equipment. 0 is no tier, 1 is standard, 2 is lunar", defTier);

            int newTier = c.Value;

            if(newTier == defTier)
            {
                orig.Invoke(equipmentIndex, equipmentDef);
                return;
            }

            switch(newTier)
            {
                case 1:
                {
                    equipmentDef.isLunar = false;
                    equipmentDef.colorIndex = ColorCatalog.ColorIndex.Equipment;
                    break;
                }

                case 2:
                {
                    equipmentDef.isLunar = true;
                    equipmentDef.colorIndex = ColorCatalog.ColorIndex.LunarItem;
                    break;
                }

                default:
                {
                    equipmentDef.canDrop = false;
                    equipmentDef.isLunar = false; // Don't know if this is needed, but better safe than sorry!
                    break;
                }
            }
            
            Logger.LogInfo("Changing tier of " + upper + ".");
            
            orig.Invoke(equipmentIndex, equipmentDef);
        }
    }
}
