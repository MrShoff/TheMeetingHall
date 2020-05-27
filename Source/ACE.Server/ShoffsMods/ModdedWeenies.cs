using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.Server.ShoffsMods
{
    public static class ModdedWeenies
    {
        public static readonly uint[] Vendors = new uint[] { 21747010 /* Jiminey (DD) */, 21747011 /* Ares Morae (AH) */, 21747012 /* Tomato (Wager) */, 21747013 /* Bag of Foy */, 21747014 /* Mohabi */ };
        public static readonly uint[] DailyDungeonPortals = new uint[] { 21747001 /* Daily Dungeon - Low */, 21747002 /* Daily Dungeon - Mid */, 21747003 /* Daily Dungeon - High */ };

        public static readonly Dictionary<uint, int> JimineysSalePrices = new Dictionary<uint, int>(new List<KeyValuePair<uint, int>>()
            {
                new KeyValuePair<uint, int>(20630, 4),         /* MMD */

                new KeyValuePair<uint, int>(6606, 3000), // GSA Pants
                new KeyValuePair<uint, int>(6600, 3000), // GSA Coat
                new KeyValuePair<uint, int>(6609, 3000), // GSC Legs
                new KeyValuePair<uint, int>(6603, 1000), // GSC Girth
                new KeyValuePair<uint, int>(6594, 1000), // GSC Chest
                new KeyValuePair<uint, int>(6615, 1000), // GSC Arms
                new KeyValuePair<uint, int>(6597, 2000), // GSK Chest
                new KeyValuePair<uint, int>(6618, 2000), // GSK Arms
                new KeyValuePair<uint, int>(6612, 2000), // GSK Legs

                new KeyValuePair<uint, int>(30316, 2000000), // Black Thsitle
                new KeyValuePair<uint, int>(19738, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19739, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19740, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19741, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19742, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19743, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19744, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19745, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19746, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19747, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19748, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19749, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19750, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19751, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19752, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19753, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19754, 6000), // Commemorative Bronze Statue
                new KeyValuePair<uint, int>(19219, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19221, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19223, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19225, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19227, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19229, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19231, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19233, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19235, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19237, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19239, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19241, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19243, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19245, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(19247, 1000), // Decorative Bronze Statue
                new KeyValuePair<uint, int>(15883, 4000), // Bronze Battle Axe
                new KeyValuePair<uint, int>(15896, 7000), // Ben Ten's Tachi
                new KeyValuePair<uint, int>(15884, 6000), // Cragstone's Axe
                new KeyValuePair<uint, int>(15892, 6000), // Zharilim's Simi
                new KeyValuePair<uint, int>(15869, 10000), // Bronze Tower Shield
                new KeyValuePair<uint, int>(15893, 4000), // Bronze Spear
                new KeyValuePair<uint, int>(15895, 6000), // Bronze Short Sword
                new KeyValuePair<uint, int>(15868, 7000), // Bronze Round Shield
                new KeyValuePair<uint, int>(15891, 8000), // Bronze Quarter Staff
                new KeyValuePair<uint, int>(15890, 5000), // Bronze Morning Star
                new KeyValuePair<uint, int>(15886, 8000), // Bronze Longbow
                new KeyValuePair<uint, int>(15867, 7000), // Bronze Kite Shield
                new KeyValuePair<uint, int>(15888, 6000), // Bronze Crossbow
                new KeyValuePair<uint, int>(15889, 7000), // Bronze Dagger
                new KeyValuePair<uint, int>(15887, 8000), // Bronze Cestus
                new KeyValuePair<uint, int>(15882, 6000), // Bronze Atlatl
                new KeyValuePair<uint, int>(1270, 2500), // Bandit Shield
                new KeyValuePair<uint, int>(26452, 3000), // Bath Robe
                new KeyValuePair<uint, int>(4981, 4000), // PP Ice Heaume of Frore
                new KeyValuePair<uint, int>(6061, 6000), // PP Gelidite Robe



                new KeyValuePair<uint, int>(5937, 8000), // PP Impious Staff
                new KeyValuePair<uint, int>(25374, 8000), // PP Energy Crystal
                new KeyValuePair<uint, int>(8792, 2000), // Helm of the Lightbringer
                new KeyValuePair<uint, int>(8809, 2000), // Herald's Helm of the Lightbringer
                new KeyValuePair<uint, int>(8806, 2000), // Fenmalain Helm of the Lightbringer
                new KeyValuePair<uint, int>(8807, 2000), // Caulnalain Helm of the Lightbringer
                new KeyValuePair<uint, int>(8808, 2000), // Shendolain Helm of the Lightrbringer
                new KeyValuePair<uint, int>(8804, 2000), // Greatwork Helm of the Lightbringer
                new KeyValuePair<uint, int>(8805, 2000), // Nexus Helm of the Lightbringer
                new KeyValuePair<uint, int>(8799, 4000), // Greatwork Staff of the Lightbringer
                new KeyValuePair<uint, int>(8791, 3000), // Staff of the Lightbringer
                new KeyValuePair<uint, int>(8800, 3000), // Nexus Staff of the Lightbringer


                new KeyValuePair<uint, int>(8803, 5000), // Herald's Staff of the Lightbringer

                new KeyValuePair<uint, int>(6804, 1600), // Nexus Celdon Sleeves
                new KeyValuePair<uint, int>(6797, 1600), // Nexus Celdon Breastplate
                new KeyValuePair<uint, int>(6800, 1600), // Nexus Celdon Girth
                new KeyValuePair<uint, int>(6802, 3200), // Nexus Celdon Leggings
                new KeyValuePair<uint, int>(6799, 4000), // Nexus Amuli Coat
                new KeyValuePair<uint, int>(6801, 4000), // Nexus Amuli Leggings
                new KeyValuePair<uint, int>(6798, 2000), // Nexus Koujia Breastplate
                new KeyValuePair<uint, int>(6805, 2000), // Nexus Koujia Sleeves
                new KeyValuePair<uint, int>(6803, 4000), // Nexus Koujia Leggings

                new KeyValuePair<uint, int>(3715, 3000), // PP Olthoi Helm
                new KeyValuePair<uint, int>(12252, 2000), // Obsidian Director's Mask
                new KeyValuePair<uint, int>(6033, 4000), // Hamud's Pyreal Katar
                new KeyValuePair<uint, int>(1427, 4000), // Sword of Lost Light
                new KeyValuePair<uint, int>(8959, 4000), // Sword of Lost Hope



                new KeyValuePair<uint, int>(30736, 500), // Bottle of Crystal Champagne
                new KeyValuePair<uint, int>(24358, 500), // Olthoi Resurgent
                new KeyValuePair<uint, int>(25952, 500), // Homecoming Pennant
                new KeyValuePair<uint, int>(30737, 1000), // Yard Balloons
                new KeyValuePair<uint, int>(30735, 1000), // Fireworks
                new KeyValuePair<uint, int>(28073, 1000), // Blueprint for a Burun Fortress (Housing Wall Item)
                new KeyValuePair<uint, int>(27525, 400), // Burun Idol




                new KeyValuePair<uint, int>(6171, 4000), // Peerless Atlan Claw
                new KeyValuePair<uint, int>(6132, 4000), // Peerless Atlan Staff
                new KeyValuePair<uint, int>(6199, 4000), // Peerless Atlan Dagger
                new KeyValuePair<uint, int>(6253, 3000), // Peerless Atlan Spear

                new KeyValuePair<uint, int>(2496, 3000), // Overlord's Sword
                new KeyValuePair<uint, int>(30732, 8000), // Staff of the Weeping Witness
                new KeyValuePair<uint, int>(4914, 2000), // Aluvian Wand
                new KeyValuePair<uint, int>(4915, 2000), // Sho Wand
                new KeyValuePair<uint, int>(4916, 2500), // Gharu'ndim Wand
                new KeyValuePair<uint, int>(8670, 4000), // Dark Heart



                new KeyValuePair<uint, int>(22014, 2000), // Virindi Profatrix Mask





                new KeyValuePair<uint, int>(7658, 2500), // Post-patch GSA Coat
                new KeyValuePair<uint, int>(7689, 2500), // Post-patch GSA Legs
                new KeyValuePair<uint, int>(7643, 1000), // Greater Koujia Shadow Breastplate
                new KeyValuePair<uint, int>(7720, 3000), // Greater Koujia Shadow Leggings
                new KeyValuePair<uint, int>(7750, 1000), // Greater Koujia Shadow Sleeves
                new KeyValuePair<uint, int>(7628, 1000), // Greater Celdon Shadow Breastplate
                new KeyValuePair<uint, int>(7674, 1000), // Greater Celdon Shadow Girth
                new KeyValuePair<uint, int>(7705, 2000), // Greater Celdon Shadow Leggings
                new KeyValuePair<uint, int>(7735, 1000), // Greater Celdon Shadow Sleeves

                new KeyValuePair<uint, int>(10705, 3000), // Niffis Pearl

                new KeyValuePair<uint, int>(27250, 2000), // Realaidain Raiment

                new KeyValuePair<uint, int>(32216, 1000), // Pack Gold Remoran
                new KeyValuePair<uint, int>(29919, 1000), // Pack Burun Kukuur
                new KeyValuePair<uint, int>(29918, 1000), // Pack Gaerlan
                new KeyValuePair<uint, int>(29916, 1000), // Pack Asheron
                new KeyValuePair<uint, int>(29917, 1000), // Pack Bael'Zharon

                new KeyValuePair<uint, int>(12269, 2000), // Shroud of Levistras

                new KeyValuePair<uint, int>(5878, 2000), // Tremblant's Ivory Staff
            });
    }
}
