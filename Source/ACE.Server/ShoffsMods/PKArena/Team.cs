using ACE.Entity;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACE.Server.ShoffsMods.PKArena
{
    public class Team
    {
        public List<PKArenaParticipant> Participants { get; set; } = new List<PKArenaParticipant>();
        public bool DoFellowship { get; set; } = false;

        public override bool Equals(object obj)
        {
            if (obj is Team other)
            {
                foreach (var p in Participants)
                {
                    bool playerFound = false;
                    foreach (var o in other.Participants)
                    {
                        if (p.Player.Guid == o.Player.Guid)
                        {
                            playerFound = true;
                            break;
                        }
                    }
                    if (!playerFound) return false;
                }
                return true;
            }
            return false;
        }

        public uint GetMatchingIpCount(Team other)
        {
            uint count = 0;
            foreach(var ip in from o in other.Participants select o.Player.Session.EndPointC2S.Address)
            {
                foreach(var myIp in from p in Participants select p.Player.Session.EndPointC2S.Address)
                {
                    if (myIp.Equals(ip))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public List<ObjectGuid> GetOfflineMembers()
        {
            return Participants.Where(x => x.Player == null).Select(x => x.PlayerGuid).ToList();
        }

        private uint GetTeamRating()
        {
            uint rating = 0;
            if (Participants.Count == 1)
            {
                Participants.ForEach(x => rating += (uint)(x.Player.ChessRank ?? 1400));
            }
            return rating;
        }

        public bool AllPlayersAcceptedQueue()
        {
            foreach (var participant in Participants)
            {
                if (!participant.AcceptedQueue)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
