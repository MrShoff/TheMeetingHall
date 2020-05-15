using System;
using System.Collections.Generic;
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

        public void Enqueue(Team team)
        {
            queue.Add(new Entrant() { Team = team });
            if (!matchWatcherIsRunning)
                Task.Factory.StartNew(WatchForMatchup);
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
                foreach(var entrant in queue)
                {
                    foreach(var opponent in queue.FindAll(x => x != entrant))
                    {

                    }
                }
                Thread.Sleep(15000);
            }
            matchWatcherIsRunning = false;


            var e = new QueuePopEventArgs()
            {
                Matchup = new Match() { TeamOne = teamOne, TeamTwo = teamTwo },
                Timestamp = DateTime.Now,
            };
            OnQueuePop(e);
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
