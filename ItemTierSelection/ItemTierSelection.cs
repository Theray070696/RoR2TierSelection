using BepInEx;
using BepInEx.Configuration;
using RoR2;

namespace Theray070696
{
    [BepInPlugin("io.github.Theray070696.itemtierselection", "Item Tier Selection", "1.0.0")]
    public class ItemTierSelection : BaseUnityPlugin
    {
        public void Awake()
        {
            for(int i = (int)ItemIndex.Syringe; i < (int)ItemIndex.Count; i++)
            {
                ItemDef item = ItemCatalog.GetItemDef((ItemIndex) i);

                if(item.tier == ItemTier.NoTier)
                {
                    continue;
                }

                ItemTier currTier = item.tier;
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

                ConfigWrapper<int> c = Config.Wrap("Item Tiers", item.nameToken + " tier", "Tier of this item. 0 is no tier, 1 is white, 2 is green, 3 is red, 4 is lunar", defTier);

                int newTierNum = c.Value;
                
                switch(newTierNum)
                {
                    case 1:
                    {
                        item.tier = ItemTier.Tier1;
                        break;
                    }
                    
                    case 2:
                    {
                        item.tier = ItemTier.Tier2;
                        break;
                    }
                    
                    case 3:
                    {
                        item.tier = ItemTier.Tier3;
                        break;
                    }
                    
                    case 4:
                    {
                        item.tier = ItemTier.Lunar;
                        break;
                    }

                    default:
                    {
                        if(newTierNum == 5 && currTier == ItemTier.Boss)
                        {
                            break;
                        }
                        
                        item.tier = ItemTier.NoTier;
                        break;
                    }
                }
            }
            
            Logger.LogInfo("Config loaded!");
        }
    }
}
