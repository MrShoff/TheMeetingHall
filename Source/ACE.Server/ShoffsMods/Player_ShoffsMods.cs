using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using System;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        public bool IsInDailyDungeon
        {
            get
            {
                bool playerIsInDailyDungeon = false;
                foreach (var dd in Enum.GetValues(typeof(ShoffsMods.DailyDungeonProperties.DailyDungeon)))
                {
                    if (DatabaseManager.World.GetCachedWeenie((uint)dd).PropertiesPosition.TryGetValue(PositionType.Destination, out PropertiesPosition ddDest))
                    {
                        var dailyDungeonLandblock = new Position(ddDest);
                        playerIsInDailyDungeon = Location.Landblock == dailyDungeonLandblock.Landblock;
                        if (playerIsInDailyDungeon) break;
                    }
                }
                return playerIsInDailyDungeon;
            }
        }
    }
}
