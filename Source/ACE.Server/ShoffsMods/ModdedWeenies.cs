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
                //new KeyValuePair<uint, int>(6600, 100),      /* GSA Legs */
                //new KeyValuePair<uint, int>(6606, 100),      /* GSA Coat */
                new KeyValuePair<uint, int>(20630, 4),         /* MMD */
                new KeyValuePair<uint, int>(21747201, 1),      /* Deadly Fire Arrowheads */
                new KeyValuePair<uint, int>(21747202, 1),      /* Deadly Frost Arrowheads */
                new KeyValuePair<uint, int>(21747203, 1),      /* Deadly Acid Arrowheads */
                new KeyValuePair<uint, int>(21747204, 1),      /* Deadly Lightning Arrowheads */
                new KeyValuePair<uint, int>(44366, 1),         /* Deadly Armor Piercing Arrowheads */
                new KeyValuePair<uint, int>(44367, 1),         /* Deadly Frog Crotch Arrowheads */
                new KeyValuePair<uint, int>(44224, 1),         /* Deadly Blunt Arrowheads */
            });
    }
}
