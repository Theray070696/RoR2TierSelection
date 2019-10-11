using System;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using ItemCatalog = On.RoR2.ItemCatalog;
using EquipmentCatalog = On.RoR2.EquipmentCatalog;

namespace Theray070696
{
    [BepInPlugin("io.github.Theray070696.itemtierselection", "Item Tier Selection", "2.0.0")]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("dev.iDeathHD.ItemLib", BepInDependency.DependencyFlags.SoftDependency)]
    public class ItemTierSelection : BaseUnityPlugin
    {
        public void Awake()
        {
            ItemCatalog.RegisterItem += OnRegisterItem;
            EquipmentCatalog.RegisterEquipment += OnRegisterEquipment;
        }
        
        private void OnRegisterItem(ItemCatalog.orig_RegisterItem orig, ItemIndex itemIndex, ItemDef itemDef)
        {
            if(itemDef == null || itemDef.tier == ItemTier.NoTier)
            {
                orig.Invoke(itemIndex, itemDef);
                return;
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

            string itemName = itemDef.name;
            
            if(itemName == null)
                itemName = itemIndex.ToString();

            string upper = itemName.ToUpper(CultureInfo.InvariantCulture);
            itemName = string.Format((IFormatProvider) CultureInfo.InvariantCulture, "ITEM_{0}_NAME", (object) upper);

            ConfigWrapper<int> c = Config.Wrap("Item Tiers", itemName + " tier",
                "Tier of this item. 0 is no tier, 1 is white, 2 is green, 3 is red, 4 is lunar", defTier);

            int newTierNum = c.Value;

            if(newTierNum == defTier)
            {
                orig.Invoke(itemIndex, itemDef);
                return;
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
            
            orig.Invoke(itemIndex, itemDef);
        }

        private void OnRegisterEquipment(EquipmentCatalog.orig_RegisterEquipment orig, EquipmentIndex equipmentIndex, EquipmentDef equipmentDef)
        {
            if(equipmentDef == null || !equipmentDef.canDrop)
            {
                orig.Invoke(equipmentIndex, equipmentDef);
                return;
            }

            int defTier = equipmentDef.isLunar ? 2 : 1;
            
            string equipmentName = equipmentDef.name;
            
            if(equipmentName == null)
                equipmentName = equipmentIndex.ToString();

            string upper = equipmentName.ToUpper(CultureInfo.InvariantCulture);
            equipmentName = string.Format((IFormatProvider) CultureInfo.InvariantCulture, "EQUIPMENT_{0}_NAME", (object) upper);

            ConfigWrapper<int> c = Config.Wrap("Equipment Tiers", equipmentDef.nameToken + " tier",
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
            
            Logger.LogInfo("Changing tier of " + equipmentName + ".");
            
            orig.Invoke(equipmentIndex, equipmentDef);
        }
    }
}
