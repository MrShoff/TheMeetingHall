using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.Server.ShoffsMods
{
    public static class DailyDungeonProperties
    {
        public enum DailyDungeon : uint
        {
            Low = 21747001,
            Mid = 21747002,
            High = 21747003
        }

        public static bool DestinationIsDailyDungeon(Portal port)
        {
            foreach (var wcid in GetDailyDungeonWcids())
            {
                var ddWeenie = DatabaseManager.World.GetCachedWeenie(wcid);
                if (ddWeenie.PropertiesPosition.TryGetValue(PositionType.Destination, out var dest))
                {
                    if (new Position(dest).Equals(port.Destination))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public const uint DailyDungeonGeneratorWcid = 21747009;
        public const uint MaxAllegiancePlayerLimit = 9;

        public static float GetMyDailyDungeonXpMultiplier(Player player)
        {
            float lower_bound = 2.0f;
            float upper_bound = 5.0f;

            var playersInDungeon = GetPlayers(player.Location.LandblockId);

            float xpMult = MathF.Round(playersInDungeon.Count / 2.0f, MidpointRounding.AwayFromZero); // 2 = 1, 3 = 2
            xpMult = xpMult < lower_bound ? lower_bound : xpMult;
            xpMult = xpMult > upper_bound ? upper_bound : xpMult;

            return xpMult;
        }

        public static void DoAllegianceCheck(Player player)
        {
            var players = GetPlayers(player.CurrentLandblock.Id);
            if (players.Count > MaxAllegiancePlayerLimit)
            {
                List<Player> playersInSameGuild = new List<Player>();
                foreach (var p in players)
                {
                    if (p.Allegiance != null && player.Allegiance != null && p.Allegiance.MonarchId == player.Allegiance.MonarchId)
                    {
                        playersInSameGuild.Add(p);
                    }
                }
                Console.WriteLine($"{player.Name}'s allegiance has {playersInSameGuild.Count} members in the daily dungeon.");
                if (playersInSameGuild.Count > MaxAllegiancePlayerLimit)
                {
                    foreach (var p in playersInSameGuild)
                    {
                        if (!p.IsAdmin && !p.IsSentinel)
                        {
                            WorldManager.ThreadSafeTeleport(p, new Position(p.Sanctuary));
                            p.Session.Network.EnqueueSend(new GameMessageSystemChat("You have been removed from the Daily Dungeon due the allegiance player limit.", ChatMessageType.Broadcast));
                        }
                    }
                }
            }
        }

        public static void DoPklStorm(Player player)
        {
            // unnecessary on PK/PKL server
            if (!PropertyManager.GetBool("pk_server").Item && !PropertyManager.GetBool("pkl_server").Item)
            {
                var playersOnCurLandblock = GetPlayers(player.Location.LandblockId);

                if (playersOnCurLandblock.Count == 3)
                {
                    foreach (var p in playersOnCurLandblock)
                    {
                        if (p.PlayerKillerStatus != PlayerKillerStatus.PK && p.PlayerKillerStatus != PlayerKillerStatus.PKLite && p.PkLevel != PKLevel.PK)
                        {
                            p.Session.Network.EnqueueSend(new GameMessageSystemChat("[Daily Dungeon] A PKL storm is a'brewin!", ChatMessageType.Broadcast));
                            p.ApplyVisualEffects(PlayScript.PortalStorm);
                        }
                    }
                }

                if (playersOnCurLandblock.Count > 3)
                {
                    foreach (var p in playersOnCurLandblock)
                    {
                        if (p.PlayerKillerStatus != PlayerKillerStatus.PK && p.PlayerKillerStatus != PlayerKillerStatus.PKLite && p.PkLevel != PKLevel.PK)
                        {
                            p.PlayerKillerStatus = PlayerKillerStatus.PKLite;
                            p.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(p, PropertyInt.PlayerKillerStatus, (int)p.PlayerKillerStatus));
                            p.Session.Network.EnqueueSend(new GameMessageSystemChat("[Daily Dungeon] You've been struck by the PKL storm! Everyone in this dungeon is now PKL!", ChatMessageType.Broadcast));
                            p.ApplyVisualEffects(PlayScript.EnchantUpPurple);
                        }
                    }
                }
            }
        }

        public static List<uint> GetDailyDungeonWcids()
        {
            List<uint> ddWcids = new List<uint>();
            var wcidArray = Enum.GetValues(typeof(DailyDungeon));
            foreach(var wcid in wcidArray)
            {
                ddWcids.Add((uint)wcid);
            }
            return ddWcids;
        }

        public static List<Player> GetPlayers(DailyDungeon dd)
        {
            List<Player> playersOnCurLandblock = new List<Player>();

            // get portal destination
            var ddPortal = DatabaseManager.World.GetCachedWeenie((uint)dd);
            if (ddPortal != null && ddPortal.PropertiesPosition != null)
            {
                if (ddPortal.PropertiesPosition.TryGetValue(PositionType.Destination, out PropertiesPosition ddDest))
                {
                    // get landblock
                    var landblockId = new LandblockId(ddDest.ObjCellId);
                    var curLandblock = LandblockManager.GetLandblock(landblockId, false);

                    // get players in daily dungeon landblock
                    PlayerManager.GetAllOnline().ForEach(x => playersOnCurLandblock.Add(curLandblock.GetObject(x.Guid) as Player));
                    playersOnCurLandblock.RemoveAll(x => x == null);
                }
            }
            
            return playersOnCurLandblock;
        }

        public static List<Player> GetPlayers(LandblockId lb)
        {
            List<Player> playersOnCurLandblock = new List<Player>();

            // get landblock
            var curLandblock = LandblockManager.GetLandblock(lb, false);

            // get player objects in landblock
            PlayerManager.GetAllOnline().ForEach(x => playersOnCurLandblock.Add(curLandblock.GetObject(x.Guid) as Player));
            playersOnCurLandblock.RemoveAll(x => x == null);

            return playersOnCurLandblock;
        }
    }
}
