using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity.Chess;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Linq;

namespace ACE.Server.ShoffsMods.PKArena
{
    public class Match
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Team TeamOne { get; set; }
        public Team TeamTwo { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Team Winner { get; set; }
        public MatchLocation FightLocation { get; set; }
        public State CurrentState { get; set; } = State.InvitePending;
        public TimeSpan TimeLimit { get; set; } = TimeSpan.FromSeconds(1200);


        private Dictionary<ObjectGuid, Position> SpectatorsAndTheirPriorLocation;

        private List<WorldObject> BarrierObjects;

        public bool IsPlayerSpectator(ObjectGuid pGuid)
        {
            if (SpectatorsAndTheirPriorLocation == null) return false;
            return SpectatorsAndTheirPriorLocation.ContainsKey(pGuid);
        }

        public TimeSpan? GetDuration()
        {
            if (StartTime != null && EndTime != null)
            {
                return EndTime - StartTime;
            }
            else
                return null;
        }

        public float GetBarrierRadius()
        {
            return GetAllParticipants().Count > 2 ? FightLocation.Radius * 2.0f : FightLocation.Radius;
        }

        public void SpawnBarriers()
        {
            uint numBarrierObjects = 18;
            float barrierRadius = GetBarrierRadius();

            // determine barrier guardians based on max participant rating
            uint maxRating = GetMaxParticipantRating();
            List<uint> packDolls = new List<uint>();
            List<uint> wisps = new List<uint>();
            switch (maxRating)
            {
                case var r when r >= 1800:
                    packDolls.AddRange(new uint[] { 29918, 29916, 29917 }); // Gaerlan, Asheron, Bael'Zharon
                    wisps.AddRange(new uint[] { 35059 }); // Red Wisp
                    break;
                case var r when r >= 1600:
                    packDolls.AddRange(new uint[] { 35296 }); // Pack Tower Guardian 
                    wisps.AddRange(new uint[] { 35059 }); // Red Wisp
                    break;
                case var r when r >= 1400:
                    packDolls.AddRange(new uint[] { 9169 }); // Plush Tusker
                    wisps.AddRange(new uint[] { 35090 }); // Blue Wisp
                    break;
                case var r when r >= 1200:
                    packDolls.AddRange(new uint[] { 9172 }); // Pack Drudge
                    wisps.AddRange(new uint[] { 35089 }); // Green Wisp
                    break;
                case var r when r < 1200:
                    packDolls.AddRange(new uint[] { 32794 }); // Rare Pink Pack Idol
                    wisps.AddRange(new uint[] { 35089 }); // Green Wisp
                    break;
            }


            BarrierObjects = new List<WorldObject>();
            for (int i = 0; i < numBarrierObjects; i++)
            {
                uint pdWcid = packDolls.Count == 1 ? packDolls[0] : packDolls[i % packDolls.Count];
                var packDoll = Factories.WorldObjectFactory.CreateNewWorldObject(pdWcid);
                var wisp = Factories.WorldObjectFactory.CreateNewWorldObject(pdWcid);

                uint wispWcid = wisps.Count == 1 ? wisps[0] : wisps[i % wisps.Count];
                var wispObject = Factories.WorldObjectFactory.CreateNewWorldObject(wispWcid);
                wisp.SetupTableId = wispObject.SetupTableId;
                wispObject.Destroy();

                // set barrier object properties
                packDoll.Ethereal = true;
                packDoll.IgnoreCollisions = true;
                packDoll.Stuck = true;
                packDoll.TimeToRot = TimeLimit.TotalSeconds;
                packDoll.Name = "Barrier Guardian";

                wisp.Ethereal = true;
                wisp.IgnoreCollisions = true;
                wisp.Stuck = true;
                wisp.TimeToRot = TimeLimit.TotalSeconds;
                wisp.GravityStatus = false;
                wisp.SetProperty(PropertyString.LongDesc, "");
                wisp.SetProperty(PropertyInt.Mass, 0);
                wisp.Name = "Barrier Guardian";

                // set locations
                packDoll.Location = new Position(FightLocation.MidPoint);
                wisp.Location = new Position(FightLocation.MidPoint);

                var angle = i / (float)numBarrierObjects * 360.0f;
                var xy = PointOnCircle(barrierRadius, angle, new PointF(FightLocation.MidPoint.PositionX, FightLocation.MidPoint.PositionY));

                packDoll.Location.PositionX = xy.X;
                packDoll.Location.PositionY = xy.Y;
                wisp.Location.PositionX = xy.X;
                wisp.Location.PositionY = xy.Y;
                wisp.Location.PositionZ += 1.5f;

                // face middle
                var dir = Vector3.Normalize(packDoll.Location.Pos - FightLocation.MidPoint.Pos);
                packDoll.Location.Rotate(dir);
                wisp.Location.Rotate(dir);

                // place in world
                packDoll.EnterWorld();
                wisp.EnterWorld();
                BarrierObjects.Add(packDoll);
                BarrierObjects.Add(wisp);
            }
        }

        private uint GetMaxParticipantRating()
        {
            uint maxRating = 0;
            GetAllParticipants().ForEach(x => maxRating = (x.Player.ChessRank ?? 1400) > maxRating ? (uint)(x.Player.ChessRank ?? 1400) : maxRating);
            return maxRating;
        }

        public void AddSpectator(Player p)
        {
            if (p != null)
            {
                if (SpectatorsAndTheirPriorLocation == null)
                    SpectatorsAndTheirPriorLocation = new Dictionary<ObjectGuid, Position>();
                SpectatorsAndTheirPriorLocation.Add(p.Guid, p.Location);

                WorldManager.ThreadSafeTeleport(p, new Position(FightLocation.MidPoint));

                string msg = $"[PvP Queue] You are now spectating: {string.Join(", ", TeamOne.Participants.Select(x => x.Player?.Name))} vs {string.Join(", ", TeamTwo.Participants.Select(x => x.Player?.Name))}.";
                p.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
            }
        }

        public static PointF PointOnCircle(float radius, float angleInDegrees, PointF origin)
        {
            // Convert from degrees to radians via multiplication by PI/180        
            float x = (float)(radius * Math.Cos(angleInDegrees * Math.PI / 180F)) + origin.X;
            float y = (float)(radius * Math.Sin(angleInDegrees * Math.PI / 180F)) + origin.Y;

            return new PointF(x, y);
        }

        private void DestroyBarriers()
        {
            if (BarrierObjects != null)
            {
                foreach(var obj in BarrierObjects)
                {
                    if (!obj.IsDestroyed)
                    {
                        obj.Destroy();
                    }
                }
            }
        }

        public List<PKArenaParticipant> GetAllParticipants()
        {
            List<PKArenaParticipant> pKArenaParticipants = new List<PKArenaParticipant>();
            if (TeamOne.Participants != null)
                pKArenaParticipants.AddRange(TeamOne.Participants);
            if (TeamTwo.Participants != null)
                pKArenaParticipants.AddRange(TeamTwo.Participants);
            return pKArenaParticipants;
        }

        public bool AllPlayersAcceptedMatchInvite()
        {            
            foreach (var player in GetAllParticipants())
            {
                if (!player.AcceptedMatchInvite)
                {                    
                    return false;
                }
            }
            return true;
        }

        public void HandleMatchCompleted(bool? teamOneWon)
        {
            EndTime = DateTime.Now;
            FightLocation.InUse = false;

            // handle ratings
            if (teamOneWon.HasValue)
            {
                bool DisableMatchingSameIp = PropertyManager.GetBool("disable_matching_same_ip").Item;
                if (TeamOne.Participants.Count == 1 && TeamTwo.Participants.Count == 1 && (!DisableMatchingSameIp || TeamOne.GetMatchingIpCount(TeamTwo) == 0))
                {
                    ChessMatch.AdjustPlayerRanks(TeamOne.Participants[0].PlayerGuid, TeamTwo.Participants[0].PlayerGuid, teamOneWon.Value ? TeamOne.Participants[0].PlayerGuid : TeamTwo.Participants[0].PlayerGuid);
                }
            }

            // handle spectators
            if (SpectatorsAndTheirPriorLocation != null)
            {
                Thread.Sleep(2000);
                foreach(var spec in SpectatorsAndTheirPriorLocation)
                {
                    var player = PlayerManager.GetOnlinePlayer(spec.Key);
                    if (player != null)
                    {
                        WorldManager.ThreadSafeTeleport(player, new Position(spec.Value));
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat("You are being transported back to your previous location.", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        var offlinePlayer = PlayerManager.GetOfflinePlayer(spec.Key);
                        offlinePlayer.Biota.SetPosition(PositionType.Location, new Position(spec.Value), offlinePlayer.BiotaDatabaseLock);
                    }
                }
            }

            // clean up
            DestroyBarriers();

            if (teamOneWon.HasValue)
            {
                foreach (var participant in TeamOne.Participants)
                {
                    if (teamOneWon.Value)
                        participant.HandleWin();
                    else
                        participant.HandleDefeat();
                }
                foreach (var participant in TeamTwo.Participants)
                {
                    if (teamOneWon.Value)
                        participant.HandleDefeat();
                    else
                        participant.HandleWin();
                }
            }
            else
            {
                foreach (var participant in GetAllParticipants())
                {
                    participant.HandleDraw();
                }
            }

            CurrentState = teamOneWon.HasValue ? State.Completed : State.Canceled;
        }
        
        public class MatchLocation
        {
            public Position TeamOnePos { get => GetTeamOnePos(); }
            public Position TeamTwoPos { get => GetTeamTwoPos(); }
            public List<uint> ValidBlockCellIDs { get; set; }
            public bool InUse { get; set; } = false;
            public Position MidPoint { get; set; }
            public float Radius { get; set; } = 18.0f;
            public LocationType Type { get; set; }
            public string Description { get; set; }

            public enum LocationType
            {
                OneOnOne    = 0x01,
                Team        = 0x02,
                TeamOnly    = 0x04
            }

            public MatchLocation(Position midPoint, LocationType type, string description)
            {
                MidPoint = midPoint;
                Description = description;
                Type = type;
            }

            private Position GetTeamOnePos()
            {
                if (MidPoint == null) return null;
                var pos = MidPoint.InFrontOf(5.0f);
                pos.RotationW = 0.0f;
                pos.RotationZ = 1.0f; // W,Z --> (0.0, 1.0) = south; (-0.5, 0.5) = east; (0.5, 0.5) = west; (1.0, 0.0) = north
                return new Position(pos);
            }

            private Position GetTeamTwoPos()
            {
                if (MidPoint == null) return null;
                var pos = MidPoint.InFrontOf(-5.0f);
                pos.RotationW = 1.0f;
                pos.RotationZ = 0.0f; // W,Z --> (0.0, 1.0) = south; (-0.5, 0.5) = east; (0.5, 0.5) = west; (1.0, 0.0) = north
                return new Position(pos);
            }

            public List<uint> GetAdjacentCells()
            {
                if (MidPoint == null) return null;
                return GetAdjacentCells(MidPoint.Cell);
            }

            public static List<uint> GetAdjacentCells(uint blockCellID)
            {
                //log.Info($"blockCellID: {blockCellID}");
                List<uint> adjacentCells = new List<uint>();

                var cellID = blockCellID & 0xFFFF;
                //log.Info($"cellID: {cellID}");

                // outdoor cells
                if (cellID < 0x100)
                {
                    var landblock = blockCellID - cellID;
                    var northCell = cellID + 0x1;
                    var southCell = cellID - 0x1;
                    var eastCell = cellID + 0x8;
                    var westCell = cellID - 0x8;
                    //log.Info($"landblockId: {landblock}");
                    //log.Info($"northCell: {northCell}");
                    //log.Info($"southCell: {southCell}");
                    //log.Info($"eastCell: {eastCell}");
                    //log.Info($"westCell: {westCell}");

                    if (cellID % 8 == 0)
                    {
                        northCell -= 0x8;
                        northCell += landblock + 0x10000;
                    }
                    else
                    {
                        northCell += landblock;
                    }
                    if (cellID % 8 == 1)
                    {
                        southCell += 0x8;
                        southCell += landblock - 0x10000;
                    }
                    else
                    {
                        southCell += landblock;
                    }
                    if (eastCell > 64)
                    {
                        eastCell -= 0x40;
                        eastCell += landblock + 0x1000000;
                    }
                    else
                    {
                        eastCell += landblock;
                    }
                    if (westCell < 1)
                    {
                        westCell += 0x40;
                        westCell += landblock - 0x1000000;
                    }
                    else
                    {
                        westCell += landblock;
                    }
                    //log.Info($"(final)northCell: {northCell}");
                    //log.Info($"(final)southCell: {southCell}");
                    //log.Info($"(final)eastCell: {eastCell}");
                    //log.Info($"(final)westCell: {westCell}");
                    adjacentCells.Add(northCell);
                    adjacentCells.Add(southCell);
                    adjacentCells.Add(eastCell);
                    adjacentCells.Add(westCell);
                }

                return adjacentCells;
            }
        }

        public enum State
        {
            InvitePending,
            InProgress,
            Completed,
            Canceled
        }
    }
}
