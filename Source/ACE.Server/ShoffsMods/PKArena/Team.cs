using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.Server.ShoffsMods.PKArena
{
    public class Team
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<PKArenaParticipant> Participants { get; set; } = new List<PKArenaParticipant>();
        public uint Rating { get; set; }

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
    }
}
