using ACE.Common;
using ACE.Database;
using ACE.Database.Entity;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Command;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Factories.Enum;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.ShoffsMods;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACE.Server.Network.Handlers
{
    public static class HouseOwnerSeeder
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static TreasureTinker tinkerer = null;
        private static List<uint> questRewardWcids = null;

        private static void Init()
        {
            questRewardWcids = GetQuestRewardWcids();
            tinkerer = new TreasureTinker();
            tinkerer.SetChanceImbueArmor(0.2f);
            tinkerer.SetChanceImbueJewelry(0.8f);
            tinkerer.SetChanceImbueWeapon(1.0f);
            tinkerer.SetChanceToTink(1.0f);
        }

        [CommandHandler("hosrent", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Abandon 5% of owned houses")]
        public static void HandlePayRent(Session session, params string[] parameters)
        {
            var ownedHouses = session.Player.GetMultiHouses();
            List<House> abandonHouses = new List<House>();

            abandonHouses.AddRange(ownedHouses.Take((int)Math.Ceiling(ownedHouses.Count * 0.05)));

            foreach (var abandonHouse in abandonHouses)
            {
                var house = session.Player.GetHouse(abandonHouse.Guid.Full);

                HouseManager.HandleEviction(house, house.HouseOwner ?? 0, true);
            }
            session.Player.SaveBiotaToDatabase();
        }

        [CommandHandler("hostest", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Test the loot randomly generated in /seedhouseowners")]
        public static void HandleSeedLootTest(Session session, params string[] parameters)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int numHouses = 1;
            if (parameters.Length == 1)
            {
                if (int.TryParse(parameters[0], out int num))
                {
                    numHouses = num;
                }
            }
            session.Network.EnqueueSend(new GameMessageSystemChat($"Starting loot generation on {numHouses * 36} items", ChatMessageType.Broadcast));
            if (questRewardWcids == null || tinkerer == null) Init();
            List<IQueryable<WorldObject>> generatedTreasure = new List<IQueryable<WorldObject>>();
            for(int i = 0; i < numHouses; i++)
            {
                generatedTreasure.Add(Task.Factory.StartNew(() => GenerateTreasure(36)).Result);
            }
            for (int i = 0; i < generatedTreasure.Count; i++)
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"--- House #{i+1}:", ChatMessageType.Broadcast));
                foreach (var item in generatedTreasure[i])
                {
                    string msg = $"{item.StackSize ?? 1}x {item.Name} ({item.ItemType})";
                    var rareTier = LootGenerationFactory.GetRareTier(item.WeenieClassId);
                    if (rareTier != 0)
                        msg += $" <--- RARE (tier {rareTier})";
                    if (item.NumTimesTinkered > 0)
                    {
                        msg += $"\n   {string.Join("\n   ",item.LongDesc.Split("\n"))}";
                    }
                    session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
                    item.Destroy();
                    //session.Player.TryCreateInInventoryWithNetworking(item);
                }
            }
            stopwatch.Stop();
            session.Network.EnqueueSend(new GameMessageSystemChat($"Total time to generate {numHouses*36} items: {stopwatch.Elapsed}", ChatMessageType.Broadcast));
        }

        [CommandHandler("hosown", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Buy up most available houses and seed with randomly generated loot")]
        public static void HandleSeedHouseOwners(Session session, params string[] parameters)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            double percentToBuy = 0.001;
            if (parameters.Length == 1)
            {
                if (double.TryParse(parameters[0], out double num))
                {
                    percentToBuy = num;
                }
            }

            HouseList.GetHouseList();
            int numApartments = (int)Math.Floor(HouseList.Available[HouseType.Apartment].Count * percentToBuy);
            int numCottages = (int)Math.Floor(HouseList.Available[HouseType.Cottage].Count * percentToBuy);
            int numVillas = (int)Math.Floor(HouseList.Available[HouseType.Villa].Count * percentToBuy);
            int numMansions = (int)Math.Floor(HouseList.Available[HouseType.Mansion].Count * percentToBuy);

            session.Network.EnqueueSend(new GameMessageSystemChat($"Starting house owner seeding on {numApartments + numCottages + numVillas + numMansions} houses", ChatMessageType.Broadcast));

            var apartments = GetHouseObjects(session, HouseType.Apartment, numApartments);
            var cottages = GetHouseObjects(session, HouseType.Cottage, numCottages);
            var villas = GetHouseObjects(session, HouseType.Villa, numVillas);
            var mansions = GetHouseObjects(session, HouseType.Mansion, numMansions);


            if (questRewardWcids == null || tinkerer == null) Init();

            List<KeyValuePair<SlumLord, IQueryable<WorldObject>>> slumlords = new List<KeyValuePair<SlumLord, IQueryable<WorldObject>>>();
            slumlords.AddRange(from h in apartments select new KeyValuePair<SlumLord, IQueryable<WorldObject>>(h.SlumLord, GenerateTreasure(12)));  // Apartments get (up to) 12 items
            slumlords.AddRange(from h in cottages select new KeyValuePair<SlumLord, IQueryable<WorldObject>>(h.SlumLord, GenerateTreasure(24)));    // Cottages get (up to) 24 items
            slumlords.AddRange(from h in villas select new KeyValuePair<SlumLord, IQueryable<WorldObject>>(h.SlumLord, GenerateTreasure(36)));      // Villas get (up to) 36 items
            slumlords.AddRange(from h in mansions select new KeyValuePair<SlumLord, IQueryable<WorldObject>>(h.SlumLord, GenerateTreasure(60)));    // Mansions get (up to) 60 items

            Player brian = session.Player;

            foreach(var slumlord in slumlords)
            {
                PurchaseHouse(brian, slumlord.Key);
                RandomlyDisperseLoot(slumlord.Key, slumlord.Value);
            }
            brian.SaveBiotaToDatabase();
            stopwatch.Stop();
            session.Network.EnqueueSend(new GameMessageSystemChat($"House owner seeding complete on {numApartments + numCottages + numVillas + numMansions} houses", ChatMessageType.Broadcast));
            session.Network.EnqueueSend(new GameMessageSystemChat($"Total time to seed: {stopwatch.Elapsed}", ChatMessageType.Broadcast));
        }

        [CommandHandler("checkhouses", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Check to see how many houses are loaded")]
        public static void HandleCheckHouses(Session session, params string[] parameters)
        {
            GetLoadedHouses();
        }

        [CommandHandler("loadhl", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, "Loads all housing landblocks")]
        public static void HandleLoadHousingLandblocks(Session session, params string[] parameters)
        {
            List<uint> LandblockRaws = GetLandblockRaws();
            foreach (var raw in LandblockRaws)
            {
                var id = new ACE.Entity.LandblockId(raw);
                _ = LandblockManager.GetLandblock(id, false);
            }
            session.Network.EnqueueSend(new GameMessageSystemChat($"{LandblockManager.GetLoadedLandblocks().Count} landblocks loaded", ChatMessageType.Broadcast));
            //log.Info($"Loaded landblocks: {LandblockManager.GetLoadedLandblocks().Count}");
            //foreach (var xy in landblockXys)
            //{
            //    var id = new ACE.Entity.LandblockId(xy.Item1, xy.Item2);                
            //    var landblock = LandblockManager.GetLandblock(id, false);
            //    if (landblock.Houses.Count == 0)
            //        LandblockManager.AddToDestructionQueue(landblock);
            //}
            //log.Info($"Loaded landblocks: {LandblockManager.GetLoadedLandblocks().Count}");
        }

        private static List<House> GetHouseObjects(Session session, HouseType houseType, int count)
        {
            if (HouseList.Available[houseType].Count < count) throw new ArgumentOutOfRangeException(nameof(count));

            var houses = GetLoadedHouses();

            return houses.Where(x => x.HouseType == houseType && x.HouseOwner == null).Take(count).ToList();

            //foreach(var slum in slums)
            //{
            //    var houseId = slum.BiotaPropertiesDID.FirstOrDefault(i => i.Type == (ushort)PropertyDataId.HouseId);
            //    if (houseId == null)
            //    {
            //        Console.WriteLine($"HouseOwnerSeeder.GetHouseObjects(): couldn't find house id for {slum.Id:X8}");
            //        return;
            //    }
            //    if (HouseManager.HouseIdToGuid.TryGetValue(houseId.Value, out var houseGuids))
            //    {
            //        log.Info($"holy shit we found the house ({houseGuids.Count})");
            //        houseGuids.ForEach(x => HouseManager.GetHouse(x, HandleSlumsLoaded));
            //    }
            //}

            //foreach (var house in houses)
            //{

            //    HouseManager.GetHouse(house.LandblockInstance.Guid, HandleSlumsLoaded);
            //    //var slums =  DatabaseManager.Shard.BaseDatabase.GetBiotasByType(WeenieType.SlumLord);
            //    //slums[0].
            //    //slumLord.HouseInstance = slumLord.BiotaPropertiesDID.FirstOrDefault(i => i.Type == (ushort)PropertyDataId.HouseId); ;          
            //    //if (slumLord != null)
            //    //{
            //    //    log.Info($"found {slumLord.House.Name}");
            //    //    housingSlumlords.Add(slumLord);
            //    //}
            //    //log.Info($"house.Weenie.ClassId: {house.Weenie.ClassId}");
            //    //log.Info($"house.LandblockInstance.Guid: {house.LandblockInstance.Guid}");
            //    //log.Info($"house.LandblockInstance.Landblock: {house.LandblockInstance.Landblock}");
            //    //var landblockId = new ACE.Entity.LandblockId((uint)house.LandblockInstance.Landblock.Value);
            //    //log.Info($"landblockId: {landblockId.ToString()}");
            //    //var landblock = LandblockManager.GetLandblock(landblockId, false);
            //    //log.Info($"LandblockManager.IsLoaded(landblockId): {LandblockManager.IsLoaded(landblockId)}");
            //    //log.Info($"landblock.Houses.Count: {landblock.Houses.Count}");
            //    //if (landblock.Houses.Count > 0)
            //    //{
            //    //    SlumLord slumLord = landblock.Houses.Find(x => x.WeenieClassId == house.Weenie.ClassId).SlumLord;
            //    //    //SlumLord slumLord = landblock.GetObject(house.LandblockInstance.Guid) as SlumLord;
            //    //    if (slumLord != null)
            //    //    {
            //    //        housingSlumlords.Add(slumLord);
            //    //    }
            //    //}                
            //}
        }

        private static List<House> GetLoadedHouses()
        {
            var loadedLandblocks = LandblockManager.GetLoadedLandblocks();

            List<House> houses = new List<House>();
            foreach (var landblock in loadedLandblocks)
            {
                houses.AddRange(landblock.Houses.Where(x => x.SlumLord != null));
            }

            //var houseBiotas = DatabaseManager.Shard.BaseDatabase.GetBiotasByType(WeenieType.House);            

            //List<House> houses = new List<House>();
            //houseBiotas.ForEach(x => houses.Add((House)WorldObjectFactory.CreateWorldObject(x)));

            log.Info($"Found {houses.Count} houses ({houses.Where(x => x.HouseOwner != null).Count()} owned)");
            log.Info($"   {houses.Where(x => x.HouseType == HouseType.Apartment).Count()} Apartment ({houses.Where(x => x.HouseType == HouseType.Apartment && x.HouseOwner != null).Count()} owned)");
            log.Info($"   {houses.Where(x => x.HouseType == HouseType.Cottage).Count()} Cottage ({houses.Where(x => x.HouseType == HouseType.Cottage && x.HouseOwner != null).Count()} owned)");
            log.Info($"   {houses.Where(x => x.HouseType == HouseType.Villa).Count()} Villa ({houses.Where(x => x.HouseType == HouseType.Villa && x.HouseOwner != null).Count()} owned)");
            log.Info($"   {houses.Where(x => x.HouseType == HouseType.Mansion).Count()} Mansion ({houses.Where(x => x.HouseType == HouseType.Mansion && x.HouseOwner != null).Count()} owned)");
            log.Info($"   {houses.Where(x => x.HouseType == HouseType.Undef).Count()} Undef ({houses.Where(x => x.HouseType == HouseType.Undef && x.HouseOwner != null).Count()} owned)");

            return houses;
        }

        private static List<uint> GetLandblockRaws()
        {
            using(var context = new WorldDbContext())
            {
                var query = from weenie in context.Weenie
                            join inst in context.LandblockInstance on weenie.ClassId equals inst.WeenieClassId
                            where weenie.Type == (int)WeenieType.House
                            select inst.ObjCellId;

                var results = query.Distinct().ToList();
                return results;
            }
        }

        private static IQueryable<WorldObject> GenerateTreasure(int count)
        {
            List<WorldObject> rngTreasure = new List<WorldObject>();
            WeenieType[] validWeenieTypes = new WeenieType[]
            {
                WeenieType.Ammunition,
                WeenieType.AugmentationDevice,
                WeenieType.Book,
                WeenieType.Caster,
                WeenieType.Clothing,
                WeenieType.CraftTool,
                WeenieType.Food,
                WeenieType.Gem,
                WeenieType.Key,
                WeenieType.Lockpick,
                WeenieType.ManaStone,
                WeenieType.MeleeWeapon,
                WeenieType.Missile,
                WeenieType.MissileLauncher,
                WeenieType.Scroll,
                WeenieType.SpellComponent,
                WeenieType.Stackable,
            };

            var rngNumItems = ThreadSafeRandom.Next(5, count);
            for (int i = 0; i < rngNumItems; i++)
            {
                var rngWeenieType = ThreadSafeRandom.Next(0, validWeenieTypes.Length);
                WorldObject treasure = null;
                while (treasure == null)
                {
                    if (rngWeenieType < validWeenieTypes.Length)
                    {
                        treasure = LootGenerationFactory.CreateRandomObjectsOfType(validWeenieTypes[rngWeenieType], 1).FirstOrDefault();
                        if (validWeenieTypes[rngWeenieType] == WeenieType.Caster || validWeenieTypes[rngWeenieType] == WeenieType.Clothing ||
                            validWeenieTypes[rngWeenieType] == WeenieType.MeleeWeapon || validWeenieTypes[rngWeenieType] == WeenieType.MissileLauncher)
                        {
                            if (!questRewardWcids.Contains(treasure.WeenieClassId))
                            {
                                treasure = GenerateTinkeredLoot(tinkerer);
                            }
                        }                            
                        if (LootGenerationFactory.GetRareTier(treasure.WeenieClassId) > 0)
                            treasure = GenerateRare();
                    }
                    else
                    {
                        treasure = GenerateRare();
                    }
                    if ((treasure.MaxStackSize ?? 1) > 1)
                    {
                        if (LootGenerationFactory.GetRareTier(treasure.WeenieClassId) != 0)
                        {
                            treasure.SetStackSize(ThreadSafeRandom.Next(1, treasure.MaxStackSize.Value > 10 ? 10 : treasure.MaxStackSize.Value));
                        }
                        else
                        {
                            treasure.SetStackSize(ThreadSafeRandom.Next(1, treasure.MaxStackSize.Value));
                        }
                    }
                }
                rngTreasure.Add(treasure);
            }
            return rngTreasure.AsQueryable();
        }

        private static List<uint> GetQuestRewardWcids()
        {
            using (var context = new WorldDbContext())
            {
                var query = from r in context.Recipe select r.SuccessWCID;
                return query.Distinct().ToList();
            }
        }

        private static WorldObject GenerateTinkeredLoot(TreasureTinker tinkerer)
        {
            TreasureDeath lootProfile = GetRandomLootProfile();
            WorldObject treasure = null;
            while (treasure == null)
            {
                treasure = LootGenerationFactory.CreateRandomLootObjects(lootProfile, TreasureItemCategory.MagicItem);
                if (treasure != null)
                {                    
                    tinkerer.ApplyTinks(treasure);
                }                
            }            
            return treasure;
        }

        private static WorldObject GenerateRare(int tier = 0)
        {
            WorldObject rare = null;
            if (tier == 0)
            {
                while (rare == null)
                {
                    rare = CreateRare();
                }
            }
            else if (tier >= 1 && tier <= 6)
            {
                if (LootGenerationFactory.RareWCIDs.TryGetValue(tier, out var wcids))
                    rare = WorldObjectFactory.CreateNewWorldObject((uint)wcids.ToList()[ThreadSafeRandom.Next(0,wcids.Count-1)]);
            }
            else
                throw new ArgumentOutOfRangeException(nameof(tier));
            return rare;
        }

        private static TreasureDeath GetRandomLootProfile()
        {
            using (var context = new WorldDbContext())
            {
                var query = from t in context.TreasureDeath select t;
                var results = query.ToList();
                return results[ThreadSafeRandom.Next(0, results.Count-1)];
            }
        }

        private static void PurchaseHouse(Player player, WorldObject house)
        {
            var slumlord = house as SlumLord;

            if (slumlord == null)
                return;

            player.SetHouseOwner(slumlord);
            log.Info($"You now own the {slumlord.House.HouseType} at /tele {slumlord.Location.GetMapCoordStr()}");
            var position = slumlord.Location;
        }

        private static void RandomlyDisperseLoot(SlumLord slumLord, IQueryable<WorldObject> rngLoot)
        {
            if (rngLoot.Count() == 0) return;

            int numToHook = ThreadSafeRandom.Next(rngLoot.Where(x => x.HookType.HasValue).Count() / 2, rngLoot.Where(x => x.HookType.HasValue).Count());
            List<WorldObject> dispersedLoot = new List<WorldObject>();
            foreach (var item in rngLoot.Where(x => x.HookType.HasValue).Take(numToHook))
            {
                var availableHooks = slumLord.House.Hooks.Where(x => x.HookType.HasValue).Where(x => x.HookType == item.HookType && !x.HasItem);
                if (availableHooks.Count() > 0)
                {
                    if (availableHooks.ToArray()[ThreadSafeRandom.Next(0, availableHooks.Count()-1)].TryAddToInventory(item))
                        dispersedLoot.Add(item);
                }
            }
            foreach (var item in rngLoot.Where(x => !dispersedLoot.Contains(x)))
            {
                List<Storage> storage = null;
                if (slumLord.House.HasDungeon) // villa or mansion
                {
                    storage = slumLord.House.GetDungeonHouse().Storage;
                }
                else
                {
                    storage = slumLord.House.Storage;
                }
                foreach(var chest in storage)
                {
                    if (chest.TryAddToInventory(item))
                    {
                        dispersedLoot.Add(item);
                        break;
                    }
                }
            }
            foreach(var item in rngLoot.Where(x => !dispersedLoot.Contains(x)))
            {
                log.Info($"{item.Name} could not be placed anywhere!");
            }
            foreach(var item in dispersedLoot)
            {
                var rareTier = LootGenerationFactory.GetRareTier(item.WeenieClassId);
                if (rareTier > 2)
                    log.Warn($"Tier {rareTier} rare ({item.Name}) placed at {slumLord.House.HouseType}        ({slumLord.Location.GetMapCoordStr()})");
            }
        }

        private static Dictionary<int, HashSet<int>> RareWCIDs;

        public static void InitRares()
        {
            RareWCIDs = new Dictionary<int, HashSet<int>>();

            var tier1Rares = new HashSet<int>() { 30183, 30184, 30186, 30187, 30188, 30189, 30194, 30195, 30196, 30197, 30199, 30200, 30202, 30205, 30206, 30209, 30214, 30215, 30216, 30217, 30218, 30221, 30222, 30224, 30225, 30226, 30228, 30229, 30232, 30233, 30234, 30240, 30242, 30245, 30246, 41257, 43407, 45360, 45366, 45367, 45368, 45369 };
            var tier2Rares = new HashSet<int>() { 30107, 30108, 30109, 30181, 30182, 30185, 30190, 30191, 30192, 30193, 30201, 30203, 30204, 30207, 30208, 30210, 30211, 30212, 30213, 30219, 30220, 30227, 30230, 30231, 30235, 30237, 30239, 30241 };
            var tier3Rares = new HashSet<int>() { 30250, 30251, 30252, 30258, 52034 };

            var tier4Rares = new HashSet<int>() { 30352, 30353, 30354, 30355, 30356, 30357, 30358, 30359, 30360, 30361, 30362, 30363, 30364, 30365, 30366, 30367, 30368, 30369, 30370, 30371, 30372, 30373, 30510, 30511, 30512, 30513, 30514, 30515, 30516, 30517, 30518, 30519, 30520, 30521, 30522, 30523, 30524, 30525, 30526, 30527, 30528, 30529, 30530, 30531, 30532, 30533, 30534 };
            var tier5Rares = new HashSet<int>() { 30074, 30075, 30076, 30077, 30078, 30079, 30080, 30081, 30082, 30083, 30084, 30085, 30086, 30087, 30088, 30089, 30090, 30091, 30092, 30093, 30094, 30095, 30096, 30097, 30098, 30099, 30100, 30101, 30102, 30103, 30104, 30105, 30106, 30110, 30111, 30112, 30113, 30114, 30115, 30116, 30117, 30118, 30119, 30120, 30121, 30122, 30123, 30124, 30125, 30126, 30127, 30128, 30129, 30130, 30131, 30132, 30133, 30134, 30135, 30136, 30137, 30139, 30140, 30141, 30142, 30143, 30144, 30145, 30146, 30147, 30148, 30149, 30150, 30151, 30152, 30153, 30154, 30155, 30156, 30157, 30158, 30159, 30160, 30161, 30162, 30163, 30164, 30165, 30166, 30167, 30168, 30169, 30171, 30173, 30174, 30175, 30176, 30179, 30180, 30247, 30248, 30249, 30253, 30254, 30318, 30936 };
            var tier6Rares = new HashSet<int>() { 30302, 30303, 30304, 30305, 30306, 30307, 30308, 30309, 30310, 30311, 30312, 30313, 30314, 30315, 30316, 30317, 30318, 30319, 30320, 30321, 30322, 30323, 30324, 30325, 30326, 30327, 30328, 45461, 30330, 30331, 30332, 30333, 30334, 30335, 30336, 30337, 30338, 30339, 30340, 30341, 30342, 30343, 30344, 30345, 30346, 30347, 30348, 30349, 30350, 30351, 30374, 30375, 30376, 30377, 30378, 42662, 42663, 42664, 42665, 42666, 43848 };

            RareWCIDs.Add(1, tier1Rares);
            RareWCIDs.Add(2, tier2Rares);
            RareWCIDs.Add(3, tier3Rares);
            RareWCIDs.Add(4, tier4Rares);
            RareWCIDs.Add(5, tier5Rares);
            RareWCIDs.Add(6, tier6Rares);
        }

        public static WorldObject CreateRare()
        {
            if (RareWCIDs == null) InitRares();

            int tier = 1;
            if (ThreadSafeRandom.Next(1, 10) == 1)  // 1 in 25,000 chance
            {
                tier = 2;
            }
            if (ThreadSafeRandom.Next(1, 1000) == 1)  // 1 in 250,000 chance
            {
                tier = 3;
            }
            if (ThreadSafeRandom.Next(1, 12500) == 1)  // 1 in 3,120,000 chance
            {
                tier = 4;
            }
            if (ThreadSafeRandom.Next(1, 30170) == 1)  // 1 in 7,542,500 (wiki avg. 7,543,103)
            {
                tier = 5;
            }
            if (ThreadSafeRandom.Next(1, 35000) == 1)  // 1 in 8,750,000 chance
            {
                tier = 6;
            }

            if (tier == 0) return null;

            var tierRares = RareWCIDs[tier].ToList();

            var rng = ThreadSafeRandom.Next(0, tierRares.Count - 1);

            var rareWCID = tierRares[rng];

            var wo = WorldObjectFactory.CreateNewWorldObject((uint)rareWCID);

            if (wo == null)
                log.Error($"LootGenerationFactory_Rare.CreateRare(): failed to generate rare wcid {rareWCID}");

            return wo;
        }
    }
}
