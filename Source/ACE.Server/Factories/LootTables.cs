using System.Collections.Generic;

using ACE.Entity.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories
{
    public static class LootTables
    {
        /// <summary>
        /// A mapping of MaterialTypes to value modifiers
        /// </summary>
        private static Dictionary<int, double> materialModifier = new Dictionary<int, double>()
        {
            { 1,  1.0 }, // Ceramic
            { 2,  1.5 }, // Porcelain
            { 3,  1.0 }, // Cloth ====
            { 4,  1.0 }, // Linen
            { 5,  1.4 }, // Satin
            { 6,  1.8 }, // Silk
            { 7,  1.8 }, // Velvet
            { 8,  1.0 }, // Wool
            { 10, 1.2 }, // Agate
            { 11, 1.4 }, // Amber
            { 12, 1.6 }, // Amethyst
            { 13, 1.8 }, // Aquamarine
            { 14, 1.2 }, // Azurite
            { 15, 1.6 }, // Black Garnet
            { 16, 2.0 }, // Black Opal
            { 17, 1.4 }, // Bloodstone
            { 18, 1.4 }, // Carnelian
            { 19, 1.4 }, // Citrine
            { 20, 2.5 }, // Diamond
            { 21, 1.2 }, // Emerald
            { 22, 2.0 }, // Fire Opal
            { 23, 1.0 }, // Green Garnet
            { 24, 1.6 }, // Green Jade
            { 25, 1.4 }, // Hematite
            { 26, 2.0 }, // Imperial Topaz
            { 27, 1.6 }, // Jet
            { 28, 1.2 }, // Lapis Lazuli
            { 29, 2.0 }, // Lavender Jade
            { 30, 1.2 }, // Malachite
            { 31, 1.4 }, // Moonstone
            { 32, 1.4 }, // Onyx
            { 33, 1.2 }, // Opal
            { 34, 1.8 }, // Peridot
            { 35, 1.0 }, // Red Garnet
            { 36, 2.0 }, // Red Jade
            { 37, 1.4 }, // Rose Quartz
            { 38, 2.5 }, // Ruby
            { 39, 2.5 }, // Sapphire
            { 40, 1.2 }, // Smokey Quartz
            { 41, 2.0 }, // Sunstone
            { 42, 1.2 }, // Tiger Eye
            { 43, 1.6 }, // Tourmaline
            { 44, 1.2 }, // Turquoise
            { 45, 1.6 }, // White Jade
            { 46, 1.2 }, // White Quartz
            { 47, 2.0 }, // White Sapphire
            { 48, 1.6 }, // Yellow Garnet
            { 49, 2.0 }, // Yellow Topaz
            { 50, 1.5 }, // Zircon
            { 51, 1.0 }, // Ivory
            { 52, 1.0 }, // Leather
            { 53, 1.2 }, // Armoredillo Hide
            { 54, 1.2 }, // Gromnie Hide
            { 55, 1.2 }, // Reedshark Hide
            { 56, 1.0 }, // Metal ====
            { 57, 1.2 }, // Brass
            { 58, 1.2 }, // Bronze
            { 59, 1.1 }, // Copper
            { 60, 1.8 }, // Gold
            { 61, 1.3 }, // Iron
            { 62, 2.0 }, // Pyreal
            { 63, 1.6 }, // Silver
            { 64, 1.4 }, // Steel
            { 65, 1.0 }, // Stone ====
            { 66, 1.4 }, // Alabaster
            { 67, 1.2 }, // Granite
            { 68, 1.6 }, // Marble
            { 69, 1.8 }, // Obsidian
            { 70, 1.0 }, // Sandstone
            { 71, 2.0 }, // Serpentine
            { 72, 1.0 }, // Wood ====
            { 73, 2.0 }, // Ebony
            { 74, 1.8 }, // Mahogany
            { 75, 1.4 }, // Oak
            { 76, 1.0 }, // Pine
            { 77, 1.2 }, // Teak
        };

        public static double getMaterialValueModifier(WorldObject wo)
        {
            if (wo.MaterialType != null && materialModifier.TryGetValue((int)wo.MaterialType, out var materialMod))
                return materialMod;
            else
                return 1.0;
        }

        public static double getGemMaterialValueModifier(WorldObject wo)
        {
            return getMaterialValueModifier(wo);
        }

        public static int[][] DefaultMaterial { get; } =
        {
            new int[] { (int)MaterialType.Copper, (int)MaterialType.Bronze, (int)MaterialType.Iron, (int)MaterialType.Steel, (int)MaterialType.Silver },            // Armor
            new int[] { (int)MaterialType.Oak, (int)MaterialType.Teak, (int)MaterialType.Mahogany, (int)MaterialType.Pine, (int)MaterialType.Ebony },               // Missile
            new int[] { (int)MaterialType.Brass, (int)MaterialType.Ivory, (int)MaterialType.Gold, (int)MaterialType.Steel, (int)MaterialType.Diamond },             // Melee
            new int[] { (int)MaterialType.RedGarnet, (int)MaterialType.Jet, (int)MaterialType.BlackOpal, (int)MaterialType.FireOpal, (int)MaterialType.Emerald },   // Caster
            new int[] { (int)MaterialType.Granite, (int)MaterialType.Ceramic, (int)MaterialType.Porcelain, (int)MaterialType.Alabaster, (int)MaterialType.Marble }, // Dinnerware
            new int[] { (int)MaterialType.Linen, (int)MaterialType.Wool, (int)MaterialType.Velvet, (int)MaterialType.Satin, (int)MaterialType.Silk }                // Clothes
        };

        // for logging epic/legendary drops
        public static HashSet<int> MinorCantrips;
        public static HashSet<int> MajorCantrips;
        public static HashSet<int> EpicCantrips;
        public static HashSet<int> LegendaryCantrips;

        private static List<SpellId[][]> cantripTables { get; } = new List<SpellId[][]>()
        {
            ArmorCantrips.Table,
            JewelryCantrips.Table,
            WandCantrips.Table,
            MeleeCantrips.Table,
            MissileCantrips.Table
        };

        static LootTables()
        {
            BuildCantripsTable(ref MinorCantrips, 0);
            BuildCantripsTable(ref MajorCantrips, 1);
            BuildCantripsTable(ref EpicCantrips, 2);
            BuildCantripsTable(ref LegendaryCantrips, 3);
        }

        private static void BuildCantripsTable(ref HashSet<int> table, int tier)
        {
            table = new HashSet<int>();

            foreach (var cantripTable in cantripTables)
            {
                foreach (var category in cantripTable)
                    table.Add((int)category[tier]);
            }
        }
    }
}
