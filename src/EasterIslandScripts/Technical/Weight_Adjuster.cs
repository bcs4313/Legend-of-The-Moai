using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    // adjusts weights of books on easter island so they appear no 
    // matter how many mods are on
    class Weight_Adjuster
    {
        // Volcanic Heart: 23529
        // Quantum Forces: 23526
        // Strange Painting: 843982
        private static int[] itemIds = { 23529, 23526, 843982, 234618, 234699, 654021 };  // ids of the weighted items
        private static float[] percents = { 0.04f, 0.04f, 0.025f, 0.025f, 0.025f, 0.025f };
        public static int totalRarity = 0;
        public static int newRarity = 0;

        // LETHALLEVELLOADER IS SO ANNOYING
        // now I must fix their save load bug that DUPLICATES my items.
        public static List<int> currentIds = new List<int>();

        public static float getItemRaritySum(SelectableLevel level)
        {
            List<SpawnableItemWithRarity> scraps = level.spawnableScrap;
            totalRarity = 0;
            for(int i = 0; i < scraps.Count; i++)
            {
                SpawnableItemWithRarity scrap = scraps[i];
                Item item = scrap.spawnableItem;
                if (!itemIds.Contains(item.itemId))
                {
                    totalRarity += scrap.rarity;
                }
            }

            return totalRarity;
        }

        public static void adjustRarities(SelectableLevel level)
        {
            currentIds.Clear();
            float totalRarity = getItemRaritySum(level);    

            List<SpawnableItemWithRarity> scraps = level.spawnableScrap;
            for (int i = 0; i < scraps.Count; i++)
            {
                SpawnableItemWithRarity scrap = scraps[i];
                Item item = scrap.spawnableItem;


                // LETHALLEVELLOADER FIX
                if (itemIds.Contains(item.itemId) && currentIds.Contains(item.itemId))
                {
                    scrap.rarity = 0;
                }

                if (itemIds.Contains(item.itemId) && !currentIds.Contains(item.itemId))
                {
                    scrap.rarity = (int)(totalRarity * percents[Array.IndexOf(itemIds, item.itemId)]);
                    currentIds.Add(item.itemId);
                }
            }
        }
    }
}
