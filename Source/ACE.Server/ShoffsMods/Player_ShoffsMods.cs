using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        public bool IsInDailyDungeon
        {
            get
            {
                bool playerIsInDailyDungeon = false;
                uint playerLandblock = GetPosition(PositionType.Location).Landblock;
                foreach (uint wcid in ShoffsMods.ModdedWeenies.DailyDungeonPortals)
                {
                    if (DatabaseManager.World.GetCachedWeenie(wcid).PropertiesPosition.TryGetValue(PositionType.Destination, out PropertiesPosition ddDest))
                    {
                        var lowDungeonLandblock = new Position(ddDest);
                        playerIsInDailyDungeon = playerLandblock == lowDungeonLandblock.Landblock;
                        if (playerIsInDailyDungeon) break;
                    }
                }
                return playerIsInDailyDungeon;
            }
        }
    }
}
