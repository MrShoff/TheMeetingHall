using ACE.Entity;
using ACE.Entity.Enum;
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
        private bool DisableMatchingSameIp = PropertyManager.GetBool("disable_matching_same_ip").Item;

        // entrant holder
        private List<Entrant> queue = new List<Entrant>();

        // globals for finding next match
        private bool matchWatcherIsRunning = false;
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MinRematchTime = TimeSpan.FromMinutes(10);
        public List<Match> recentMatchups = new List<Match>();        

        public bool Dequeue(ObjectGuid requestorGuid)
        {
            log.Info($"[Dequeue.Start] queue.Count:{queue.Count}");
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
                                log.Info(msg);
                            }
                        }
                        queue.Remove(entrant);
                        break;
                    }
                }
                if (dequeueableFound) break;
            }
            log.Info($"[Dequeue.End] dequeueableFound:{dequeueableFound}");
            log.Info($"[Dequeue.End] queue.Count:{queue.Count}");
            return dequeueableFound;
        }

        public bool Enqueue(Team team)
        {
            log.Info($"[Enqueue.Start] queue.Count:{queue.Count}");
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
                        participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have been queued for a rated pk fight. You will get a confirmation pop-up when your match is ready.", ChatMessageType.Broadcast));
                        
                    }
                }                
                if (!matchWatcherIsRunning)
                    Task.Factory.StartNew(WatchForMatchup);
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

            log.Info($"[Enqueue.End] queue.Count:{queue.Count}");
            return playerInQueue == null;
        }

        private List<ObjectGuid> GetAllParticipantGuids()
        {
            RemoveOfflineParticipants();
            List<ObjectGuid> participants = new List<ObjectGuid>();
            (from t in queue select t.Team.Participants)
                .ToList() // list of all participant groups
                .ForEach(t => t.ForEach(p => participants.Add(p.PlayerGuid))); // for each participanshutdt group, add their player's guids
            return participants;
        }

        private void RemoveOfflineParticipants()
        {
            List<ObjectGuid> offlinePlayerGuids = new List<ObjectGuid>();
            queue.ForEach(x => offlinePlayerGuids.AddRange(x.Team.GetOfflineMembers()));

            foreach(var pGuid in offlinePlayerGuids)
            {
                log.Info("Offline player in queue detected. Attempting to remove.");
                Dequeue(pGuid);
            }
        }

        protected virtual void OnQueuePop(QueuePopEventArgs e)
        {
            QueuePop?.Invoke(this, e);
        }

        private void WatchForMatchup()
        {
            log.Info($"WatchForMatchup started");
            matchWatcherIsRunning = true;
            Team teamOne = null;
            Team teamTwo = null;

            uint teamsWithDistinctIps = GetDistinctIpCount();

            while(teamsWithDistinctIps > 1)
            {
                foreach(var entrant in from t in queue orderby t.TimeEnqueued select t)
                {
                    foreach(var opponent in from t in queue where !t.Team.Equals(entrant.Team) orderby t.TimeEnqueued select t)
                    {
                        if (!DisableMatchingSameIp || entrant.Team.GetMatchingIpCount(opponent.Team) == 0)
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
                    break;
                Thread.Sleep(15000);
                teamsWithDistinctIps = GetDistinctIpCount();
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
            matchWatcherIsRunning = false;
            log.Info($"WatchForMatchup ended");
        }

        private uint GetDistinctIpCount()
        {
            uint count = 0;
            if (DisableMatchingSameIp)
            {
                foreach (var team in from t in queue select t.Team)
                {
                    bool noMatches = true;
                    foreach (var team2 in from t in queue where !t.Team.Equals(team) select t.Team)
                    {
                        if (team.GetMatchingIpCount(team2) >= 1)
                        {
                            noMatches = false;
                            break;
                        }
                    }
                    if (noMatches)
                        count++;
                }
            }
            else
            {
                count = (uint)queue.Count;
            }
            return count;
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
