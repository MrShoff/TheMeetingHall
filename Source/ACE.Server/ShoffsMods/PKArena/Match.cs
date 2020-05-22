using ACE.Entity;
using ACE.Server.Entity.Chess;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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
        public State CurrentState { get; set; } = State.InQueue;
        public TimeSpan TimeLimit { get; set; } = TimeSpan.FromSeconds(1200);


        private List<WorldObject> BarrierObjects;

        public TimeSpan? GetDuration()
        {
            if (StartTime != null && EndTime != null)
            {
                return EndTime - StartTime;
            }
            else
                return null;
        }

        public void SpawnBarriers()
        {
            BarrierObjects = new List<WorldObject>();
            for(int i = 0; i < 8; i++)
            {
                var barrierObject = Factories.WorldObjectFactory.CreateNewWorldObject(29918); // Pack Gaerlan    8974); // Celdiseth's Portal Gem
                barrierObject.Ethereal = true;
                barrierObject.IgnoreCollisions = true;
                barrierObject.Stuck = true;
                barrierObject.TimeToRot = TimeLimit.TotalSeconds;

                barrierObject.Location = new Position(FightLocation.MidPoint);
                barrierObject.Location.RotationW = 1.0f - i * 0.25f; // 1.0, 0.75, 0.5, 0.25, 0, -0.25, -0.5, -0.75
                barrierObject.Location.RotationZ = 1.0f - MathF.Abs(barrierObject.Location.RotationW); // 0, 0.25, 0.5, 0.75, 1, 0.75, 0.5, 0.25
                barrierObject.Location = barrierObject.Location.InFrontOf(FightLocation.Radius, true);
                barrierObject.Name = $"W:{MathF.Round(barrierObject.Location.RotationW, 1)}; Z:{MathF.Round(barrierObject.Location.RotationZ, 1)}";

                var dir = Vector3.Normalize(barrierObject.Location.Pos - FightLocation.MidPoint.Pos);
                barrierObject.Location.Rotate(dir);

                barrierObject.EnterWorld();
                BarrierObjects.Add(barrierObject);
            }            
        }

        private void DestroyBarriers()
        {
            if (BarrierObjects != null)
            {
                foreach(var obj in BarrierObjects)
                {
                    obj.Destroy();
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

        public bool AllPlayersConfirmed()
        {            
            foreach (var player in GetAllParticipants())
            {
                if (!player.AcceptedMatch)
                {                    
                    return false;
                }
            }
            return true;
        }

        public void HandleMatchCompleted(bool? teamOneWon)
        {
            EndTime = DateTime.Now;
            CurrentState = teamOneWon.HasValue ? State.Completed : State.Canceled;
            FightLocation.InUse = false;

            if (teamOneWon.HasValue)
            {
                if (TeamOne.Participants.Count == 1 && TeamTwo.Participants.Count == 1)
                {
                    ChessMatch.AdjustPlayerRanks(TeamOne.Participants[0].Player.Guid, TeamTwo.Participants[0].Player.Guid, teamOneWon.Value ? TeamOne.Participants[0].Player.Guid : TeamTwo.Participants[0].Player.Guid);
                }
            }

            DestroyBarriers();
        }


        public class MatchLocation
        {
            public Position TeamOnePos { get => GetTeamOnePos(); }
            public Position TeamTwoPos { get => GetTeamTwoPos(); }
            public List<uint> ValidBlockCellIDs { get; set; }
            public bool InUse { get; set; } = false;
            public Position MidPoint { get; set; }
            public float Radius { get; set; } = 15.0f;

            public MatchLocation(Position midPoint)
            {
                MidPoint = midPoint;
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
            InQueue,
            InProgress,
            Completed,
            Canceled
        }
    }
}
