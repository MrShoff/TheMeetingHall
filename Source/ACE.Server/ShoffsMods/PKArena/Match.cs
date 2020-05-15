using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.Server.ShoffsMods.PKArena
{
    public class Match
    {
        public Team TeamOne { get; set; }
        public Team TeamTwo { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Team Winner { get; set; }
        public State CurrentState { get; set; } = State.InQueue;

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
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
