using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
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
        // entrant holder
        private List<Entrant> queue = new List<Entrant>();

        // globals for finding next match
        private bool matchWatcherIsRunning = false;
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MinRematchTime = TimeSpan.FromMinutes(10);
        public List<Match> recentMatchups = new List<Match>();

        public void Dequeue(ObjectGuid requestorGuid)
        {
            bool dequeueableFound = false;
            var requestor = PlayerManager.FindByGuid(requestorGuid);
            foreach (var entrant in queue)
            {
                foreach (var player in entrant.Team.Participants)
                {
                    if (requestorGuid == player.Player.Guid)
                    {
                        dequeueableFound = true;
                    }
                    if (dequeueableFound)
                    {
                        string requestorName = "(unknown)";
                        if (requestor != null)
                        {
                            requestorName = requestor.Name;
                        }
                        entrant.Team.Participants.ForEach(x => x.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {requestorName} has removed your team from the queue.", ChatMessageType.Broadcast)));
                        queue.Remove(entrant);
                        break;
                    }
                }
                if (dequeueableFound)
                    break;
            }
            if (!dequeueableFound)
            {
                if (requestor != null)
                {
                    ((Player)requestor).Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You are not in the queue.", ChatMessageType.Broadcast));
                }
            }
        }

        public void Enqueue(Team team)
        {
            var allPlayersInQueue = GetAllParticipantsInQueue();
            PKArenaParticipant playerInQueue = null;
            foreach(var participant in team.Participants)
            {
                if (allPlayersInQueue.Contains(participant))
                {
                    playerInQueue = participant;
                    break;
                }
            }
            if (playerInQueue == null)
            {
                queue.Add(new Entrant() { Team = team });
                team.Participants.ForEach(x => x.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have been queued for a rated pk fight. You will get a confirmation pop-up when your match is ready.", ChatMessageType.Broadcast)));
                if (!matchWatcherIsRunning)
                    Task.Factory.StartNew(WatchForMatchup);
            }
            else
            {
                foreach(var participant in team.Participants)
                {
                    team.Participants.ForEach(x => x.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Queue failed. {playerInQueue.Player.Name} is already in queue.", ChatMessageType.Broadcast)));
                }
            }
        }

        private List<PKArenaParticipant> GetAllParticipantsInQueue()
        {
            List<PKArenaParticipant> participants = new List<PKArenaParticipant>();
            queue.ForEach(x => participants.AddRange(x.Team.Participants));
            return participants;
        }

        protected virtual void OnQueuePop(QueuePopEventArgs e)
        {
            QueuePop?.Invoke(this, e);
        }

        private void WatchForMatchup()
        {
            Team teamOne = null;
            Team teamTwo = null;

            while(queue.Count > 1)
            {
                foreach(var entrant in from t in queue orderby t.TimeEnqueued select t)
                {
                    foreach(var opponent in from t in queue where !t.Team.Equals(entrant.Team) orderby t.TimeEnqueued select t)
                    {
                        teamOne = entrant.Team;
                        teamTwo = opponent.Team;
                        queue.Remove(entrant);
                        queue.Remove(opponent);
                        break;
                    }
                    if (teamOne != null && teamTwo != null)
                        break;
                }
                if (teamOne != null && teamTwo != null)
                    break;
                Thread.Sleep(15000);
            }
            matchWatcherIsRunning = false;

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
