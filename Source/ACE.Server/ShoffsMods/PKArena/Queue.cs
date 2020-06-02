using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Command.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACE.Server.ShoffsMods.PKArena
{
    public class Queue
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public bool DoTeamMatchmaking { get; set; } = false;


        private bool DisableMatchingSameIp = PropertyManager.GetBool("disable_matching_same_ip").Item;
        // entrant holder
        private List<Entrant> queue = new List<Entrant>();

        public bool Dequeue(ObjectGuid requestorGuid)
        {
            bool dequeueableFound = false;
            var requestor = PlayerManager.FindByGuid(requestorGuid);
            foreach (var entrant in queue)
            {
                foreach (var player in entrant.Team.Participants)
                {
                    if (requestorGuid == player.PlayerGuid)
                    {
                        dequeueableFound = true;
                    }
                    if (dequeueableFound)
                    {
                        string requestorName = "(offline player)";
                        if (requestor != null)
                        {
                            requestorName = requestor.Name;
                        }
                        foreach(var participant in entrant.Team.Participants)
                        {
                            if (participant.Player != null)
                            {
                                var msg = $"[PvP Queue] {requestorName} has removed your team from the queue.";
                                participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
                            }
                        }
                        queue.Remove(entrant);
                        break;
                    }
                }
                if (dequeueableFound) break;
            }
            return dequeueableFound;
        }

        public bool Enqueue(Team team)
        {
            RemoveOfflineParticipants();
            var allPlayersInQueue = GetAllParticipantGuids();
            PKArenaParticipant playerInQueue = null;
            foreach(var participant in team.Participants)
            {
                if (allPlayersInQueue.Contains(participant.PlayerGuid))
                {
                    playerInQueue = participant;
                    break;
                }
            }
            if (playerInQueue == null)
            {
                queue.Add(new Entrant() { Team = team });
                foreach (var participant in team.Participants)
                {
                    if (participant.Player != null)
                    {
                        participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have been queued for a fight. You will get a confirmation pop-up when your match is ready.", ChatMessageType.Broadcast));                        
                    }
                }
                if (DoTeamMatchmaking)
                {
                    HandleTeamMatchmaking();
                }
                else
                {
                    CheckForValidMatchup();
                }
            }
            else
            {
                foreach(var participant in team.Participants)
                {
                    if (participant.Player != null)
                    {
                        participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Queue failed. {playerInQueue.Player.Name} is already in queue.", ChatMessageType.Broadcast));
                    }
                }
            }
            return playerInQueue == null;
        }

        public List<ObjectGuid> GetAllParticipantGuids()
        {
            List<ObjectGuid> participants = new List<ObjectGuid>();
            (from t in queue select t.Team.Participants)
                .ToList() // list of all participant groups
                .ForEach(t => t.ForEach(p => participants.Add(p.PlayerGuid))); // for each participant group, add their player's guids
            return participants;
        }

        private void RemoveOfflineParticipants()
        {
            List<ObjectGuid> offlinePlayerGuids = new List<ObjectGuid>();
            queue.ForEach(x => offlinePlayerGuids.AddRange(x.Team.GetOfflineMembers()));

            foreach(var pGuid in offlinePlayerGuids)
            {
                Dequeue(pGuid);
            }
        }

        protected virtual void OnQueuePop(QueuePopEventArgs e)
        {
            QueuePop?.Invoke(this, e);
        }

        private void CheckForValidMatchup()
        {
            Team teamOne = null;
            Team teamTwo = null;

            foreach(var entrant in from t in queue orderby t.TimeEnqueued select t)
            {
                foreach(var opponent in from t in queue where !t.Team.Equals(entrant.Team) orderby t.TimeEnqueued select t)
                {
                    if (!DisableMatchingSameIp || entrant.Team.Participants.Count > 1 || entrant.Team.GetMatchingIpCount(opponent.Team) == 0)
                    {
                        teamOne = entrant.Team;
                        teamTwo = opponent.Team;
                        queue.Remove(entrant);
                        queue.Remove(opponent);
                        break;
                    }
                }
                if (teamOne != null && teamTwo != null)
                    break;
            }            

            if (teamOne != null && teamTwo != null)
            {
                var e = new QueuePopEventArgs()
                {
                    Matchup = new Match() { TeamOne = teamOne, TeamTwo = teamTwo },
                    Timestamp = DateTime.Now,
                };
                OnQueuePop(e);
            }
        }

        private void HandleTeamMatchmaking()
        {
            List<Team> availableTeams = new List<Team>();
            availableTeams.AddRange(queue.Select(x => x.Team));

            Console.WriteLine("--HandleTeamMatchmaking().Start--");
            //availableTeams.ForEach(x => Console.Write($"{string.Join('|', x.Participants.Select(p => p.Player.Name))},"));
            //Console.WriteLine("");

            Console.WriteLine("--Queue info (beginning)--");
            queue.ForEach(x => Console.Write($"{string.Join('|', x.Team.Participants.Select(p => p.Player?.Name))},"));
            Console.WriteLine("");

            int totalInQueue = 0;
            availableTeams.ForEach(x => totalInQueue += x.Participants.Count);

            int maxVsSize = totalInQueue / 2; // rounds down

            while (maxVsSize >= 3)
            {
                Team teamOne = new Team() { DoFellowship = true };
                Team teamTwo = new Team() { DoFellowship = true };
                List<Team> removeFromMatchmaking = new List<Team>();

                while (teamOne.Participants.Count < maxVsSize)
                {
                    var team = from a in availableTeams
                               where a.Participants.Count <= (maxVsSize - teamOne.Participants.Count)
                               && a.Participants.Where(x => !teamOne.Participants.Select(x => x.PlayerGuid).Contains(x.PlayerGuid)).Count() == a.Participants.Count
                               select a;
                    team = team.OrderByDescending(x => x.Participants.Count).ThenBy(_ => Guid.NewGuid());                    
                    if (team != null && team.Count() > 0)
                    {
                        var selectedTeam = team.Take(1).ToList()[0];
                        teamOne.Participants.AddRange(selectedTeam.Participants);
                        removeFromMatchmaking.Add(selectedTeam);
                    }
                    else
                    {
                        break;
                    }
                    Console.WriteLine($"teamOne: Count:{teamOne.Participants.Count}; Participants:{string.Join('|', teamOne.Participants.Select(p => p.Player?.Name))}");
                }
                if (teamOne.Participants.Count == maxVsSize)
                {
                    while (teamTwo.Participants.Count < maxVsSize)
                    {
                        var team = from a in availableTeams
                                   where a.Participants.Count <= (maxVsSize - teamTwo.Participants.Count)
                                   && a.Participants.Where(x => !teamOne.Participants.Select(x => x.PlayerGuid).Contains(x.PlayerGuid)).Count() == a.Participants.Count
                                   && a.Participants.Where(x => !teamTwo.Participants.Select(x => x.PlayerGuid).Contains(x.PlayerGuid)).Count() == a.Participants.Count
                                   select a;
                        team = team.OrderByDescending(x => x.Participants.Count).ThenBy(_ => Guid.NewGuid());
                        if (team != null && team.Count() > 0)
                        {
                            var selectedTeam = team.Take(1).ToList()[0];
                            teamTwo.Participants.AddRange(selectedTeam.Participants);
                            removeFromMatchmaking.Add(selectedTeam);
                        }
                        else
                        {
                            break;
                        }
                        Console.WriteLine($"teamTwo: Count:{teamTwo.Participants.Count}; Participants:{string.Join('|', teamTwo.Participants.Select(p => p.Player?.Name))}");
                    }
                    if (teamTwo.Participants.Count == maxVsSize) // we found an even match!
                    {
                        Console.WriteLine($"We found a match!");
                        Console.WriteLine($"{string.Join(',', teamOne.Participants.Select(x => x.Player?.Name))} vs {string.Join(',', teamTwo.Participants.Select(x => x.Player?.Name))}");
                        Console.WriteLine($"{queue.Count} teams in mm queue, {removeFromMatchmaking.Count} set for removal");
                        removeFromMatchmaking.ForEach(x => queue.RemoveAll(e => e.Team == x));
                        Console.WriteLine($"{queue.Count} teams in mm queue after removal");
                        var e = new QueuePopEventArgs()
                        {
                            Matchup = new Match() { TeamOne = teamOne, TeamTwo = teamTwo },
                            Timestamp = DateTime.Now,
                        };
                        OnQueuePop(e);
                        return;
                    }
                }
                var largestTeam = (from a in availableTeams orderby a.Participants.Count descending select a).FirstOrDefault();
                availableTeams.Remove(largestTeam);
                totalInQueue -= largestTeam.Participants.Count;
                maxVsSize = totalInQueue / 2;
                //Console.WriteLine("--HandleTeamMatchmaking().EndOfLoop--");
                //availableTeams.ForEach(x => Console.Write($"{string.Join('|', x.Participants.Select(p => p.Player.Name))},"));
                //Console.WriteLine("");
            }
            Console.WriteLine("--Queue info (end)--");
            queue.ForEach(x => Console.Write($"{string.Join('|', x.Team.Participants.Select(p => p.Player?.Name))},"));
            Console.WriteLine("");
            //Console.WriteLine("--availableTeams info--");
            //availableTeams.ForEach(x => Console.Write($"{string.Join('|', x.Participants.Select(p => p.Player.Name))},"));
            //Console.WriteLine("");
            Console.WriteLine("--HandleTeamMatchmaking().End--");
        }

        public class Entrant
        {
            public Team Team { get; set; }
            public DateTime TimeEnqueued { get; private set; } = DateTime.Now;
        }

        public event EventHandler QueuePop;

        public class QueuePopEventArgs : EventArgs
        {
            public Match Matchup { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
