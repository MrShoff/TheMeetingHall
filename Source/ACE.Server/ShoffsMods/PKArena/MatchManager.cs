using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Managers;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ACE.Server.ShoffsMods.PKArena.Match;
using static ACE.Server.ShoffsMods.PKArena.Queue;

namespace ACE.Server.ShoffsMods.PKArena
{
    public static class MatchManager
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Queue queue = new Queue();
        private static List<Match> matches = new List<Match>();


        static MatchManager()
        {
            queue.QueuePop += Queue_QueuePop;
        }

        private static List<MatchLocation> OneOnOneMatchTeleportLocations = new List<MatchLocation>()
        {
            //new MatchLocation() { TeamOnePos = new Position(0xB36F0018, 69.208527f, 180.122711f, 69.580460f, 0.0f, 0.0f, -0.700329f, -0.713820f)
            //                    , TeamTwoPos = new Position(0xB36F0018, 50.718620f, 180.045853f, 69.580460f, 0.0f, 0.00f, -0.715707f, 0.698400f)
            //                    , ValidBlockCellIDs = new List<uint> {  0xB36F0018, 0xB37F0011/*N*/, 0xB36F0017/*S*/, 0xB36F0010/*W*/, 0xB36F0020/*E*/ } },
            //new MatchLocation() { TeamOnePos = new Position(0xB36F0008, 11.825514f, 170.851669f, 69.580711f, 0f, 0f, 0.013912f, -0.999903f)
            //                    , TeamTwoPos = new Position(0xB36F0008, 12.055335f, 188.877823f, 69.580711f, 0f, 0f, -0.999966f, -0.008269f)
            //                    , ValidBlockCellIDs = new List<uint> {  0xB36F0018, 0xB37F0011/*N*/, 0xB36F0017/*S*/, 0xB36F0010/*W*/, 0xB36F0020/*E*/ } },
            new MatchLocation(new Position(0xB36F0018, 60.0f, 180.0f, 70.0f, 0.0f, 0.0f, 0.0f, 0.0f)),
            new MatchLocation(new Position(0xB36F0008, 12.0f, 180.0f, 70.0f, 0.0f, 0.0f, 0.0f, 0.0f)),
        };

        private static void Queue_QueuePop(object sender, EventArgs e)
        {
            var args = (QueuePopEventArgs)e;
            SendMatchConfirmation(args.Matchup);
            SendTheNextMatchIn();
        }

        private static void SendMatchConfirmation(Match matchup)
        {
            matchup.CurrentState = State.InQueue;
            matches.Add(matchup);

            HandleConfirmation(matchup);
            Task.Factory.StartNew(() => WatchForNonResponders(matchup));
        }

        private static void WatchForNonResponders(Match matchup)
        {
            Thread.Sleep(5000);
            List<PKArenaParticipant> participantsThatDidntAccept = matchup.GetAllParticipants().Where(x => !x.AcceptedMatch).ToList();
            foreach (var participant in participantsThatDidntAccept)
            {
                participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have 10 seconds to accept.", ChatMessageType.Broadcast));
            }
            Thread.Sleep(5000);
            uint secondsRemaining = 5;
            participantsThatDidntAccept = matchup.GetAllParticipants().Where(x => !x.AcceptedMatch).ToList();
            while (secondsRemaining > 0 && participantsThatDidntAccept.Count > 0)
            {
                foreach (var participant in participantsThatDidntAccept)
                {
                    participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have {secondsRemaining} seconds to accept.", ChatMessageType.Broadcast));
                }
                Thread.Sleep(1000);
                secondsRemaining--;
                participantsThatDidntAccept = matchup.GetAllParticipants().Where(x => !x.AcceptedMatch).ToList();
            }
            if (participantsThatDidntAccept.Count > 0)
            {
                // someone didn't accept/respond - cancel the match and tell all the players
                matchup.CurrentState = State.Canceled;
                var namesThatDidntAccept = string.Join(", ", from p in participantsThatDidntAccept select p.Player.Name);
                foreach (var participant in matchup.GetAllParticipants())
                {
                    participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {namesThatDidntAccept} did not accept. Match canceled.", ChatMessageType.Broadcast));
                }
            }
        }

        private static void HandleConfirmation(Match matchup, bool playerConfirmed = false, Player playerThatConfirmed = null)
        {
            if (!playerConfirmed)
            {
                var msg = $"PvP match found!\nAre you ready?";
                foreach (var player in matchup.GetAllParticipants())
                {
                    player.Player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(player.Player.Guid, () => HandleConfirmation(matchup, true, player.Player)), msg);
                }
            }
            else
            {
                foreach (var player in matchup.GetAllParticipants())
                {
                    if (player.Player.Guid == playerThatConfirmed.Guid)
                    {
                        player.AcceptedMatch = playerConfirmed;
                    }
                }
                if (matchup.AllPlayersConfirmed())
                {
                    SendTheNextMatchIn();
                }                
            }
        }

        private static void SendTheNextMatchIn()
        {
            RemoveOfflinePlayersFromQueue();
            var matchesInQueue = matches.Where(x => x.CurrentState == State.InQueue && x.AllPlayersConfirmed()).ToList();
            if (matchesInQueue.Count == 0) return;
            var availableLocations = OneOnOneMatchTeleportLocations.Where(x => !x.InUse).ToList();
            if (availableLocations.Count == 0) return;
            var matchLocation = availableLocations[ThreadSafeRandom.Next(0, availableLocations.Count - 1)];
            matchLocation.InUse = true;
            var nextMatch = matchesInQueue[0];
            nextMatch.FightLocation = matchLocation;
            foreach (var p in nextMatch.TeamOne.Participants)
            {
                p.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Your PvP queue popped! You are being transported to the fight location.\nDon't forget to turn off vTank!", ChatMessageType.Broadcast));
                p.SetPklStatus();
                p.PriorLocation = p.Player.Location;
                WorldManager.ThreadSafeTeleport(p.Player, new Position(matchLocation.TeamOnePos));
            }
            foreach (var p in nextMatch.TeamTwo.Participants)
            {
                p.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Your PvP queue popped! You are being transported to the fight location.\nDon't forget to turn off vTank!", ChatMessageType.Broadcast));
                p.SetPklStatus();
                p.PriorLocation = p.Player.Location;
                WorldManager.ThreadSafeTeleport(p.Player, new Position(matchLocation.TeamTwoPos));
            }
            nextMatch.CurrentState = State.InProgress;
            Match.MatchLocation.GetAdjacentCells(matchLocation.TeamOnePos.Cell);
            Task.Factory.StartNew(() => WatchForQuitters(nextMatch));
        }

        private static void WatchForQuitters(Match matchup)
        {
            Thread.Sleep(5000); // give them time to enter

            List<ObjectGuid> playersWarned = new List<ObjectGuid>();
            while(matchup.CurrentState == State.InProgress)
            {
                foreach (var participant in matchup.GetAllParticipants())
                {
                    if (participant.Player == null || PlayerManager.GetOfflinePlayer(participant.Player.Guid) != null)
                    {
                        participant.HandleDeath(null, null);
                    }
                    else
                    {
                        bool playerWasWarned = playersWarned.Contains(participant.Player.Guid);
                        Position playerLocation = participant.Player.Location;
                        float distanceToMidpoint = playerLocation.DistanceTo(matchup.FightLocation.MidPoint);
                        if (distanceToMidpoint > 15.0f)
                        {
                            if (playerWasWarned)
                            {
                                participant.HandleDeath(null, null);
                                playersWarned.Remove(participant.Player.Guid);
                                break;
                            }
                            participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You are too far from the fight location. Turn back immediately or get disqualified!", ChatMessageType.Broadcast));
                            playersWarned.Add(participant.Player.Guid);
                        }

                        if (playerWasWarned)
                        {
                            playersWarned.Remove(participant.Player.Guid);
                        }
                    }
                }
                Thread.Sleep(5000);
            }
        }

        private static void RemoveOfflinePlayersFromQueue()
        {
            var matchesInQueue = matches.Where(x => x.CurrentState == State.InQueue).ToList();
            List<Match> matchesToRemove = new List<Match>();
            for (int i = 0; i < matchesInQueue.Count; i++)
            {
                bool foundOne = false;
                foreach (var p in matches[i].TeamOne.Participants)
                {
                    if (p.Player != null)
                    {
                        foundOne = true;
                        break;
                    }
                }
                bool foundAnother = false;
                foreach (var p in matches[i].TeamTwo.Participants)
                {
                    if (p.Player != null)
                    {
                        foundAnother = true;
                        break;
                    }
                }
                if (!foundOne || !foundAnother)
                    matchesToRemove.Add(matchesInQueue[i]);
            }
            matchesToRemove.ForEach(x => matches.Remove(x));
        }

        public static PKArenaParticipant TryGetInProgressParticipant(Player p)
        {
            var inProgressMatches = from m in matches where m.CurrentState == State.InProgress select m;
            foreach(var match in inProgressMatches)
            {
                foreach(var participant in match.TeamOne.Participants)
                {
                    if (participant.Player == p)
                        return participant;
                }
                foreach (var participant in match.TeamTwo.Participants)
                {
                    if (participant.Player == p)
                        return participant;
                }
            }
            return null;
        }

        public static Match TryGetInProgressMatchByParticipant(PKArenaParticipant p)
        {
            var inProgressMatches = from m in matches where m.CurrentState == State.InProgress select m;
            Match participantsMatch = null;
            foreach (var match in inProgressMatches)
            {
                foreach (var participant in match.TeamOne.Participants)
                {
                    if (participant == p)
                    {
                        participantsMatch = match;
                        break;
                    }
                }
                foreach (var participant in match.TeamTwo.Participants)
                {
                    if (participant == p)
                    {
                        participantsMatch = match;
                        break;
                    }
                }
            }
            return participantsMatch;
        }

        public static void HandleParticipantKilled(PKArenaParticipant p)
        {
            var participantsMatch = TryGetInProgressMatchByParticipant(p);
            bool teamOneWon = true;
            bool teamTwoWon = true;
            foreach (var participant in participantsMatch.TeamOne.Participants)
            {
                if (!participant.IsKilled)
                {
                    teamTwoWon = false;
                }
            }
            foreach (var participant in participantsMatch.TeamTwo.Participants)
            {
                if (!participant.IsKilled)
                {
                    teamOneWon = false;
                }
            }
            if (teamOneWon || teamTwoWon)
            {
                foreach (var participant in participantsMatch.TeamOne.Participants)
                {
                    if (teamOneWon)
                        participant.HandleWin();
                    else
                        participant.HandleDefeat();
                }
                foreach (var participant in participantsMatch.TeamTwo.Participants)
                {
                    if (teamTwoWon)
                        participant.HandleWin();
                    else
                        participant.HandleDefeat();
                }
                participantsMatch.EndTime = DateTime.Now;
                participantsMatch.CurrentState = Match.State.Completed;
                participantsMatch.FightLocation.InUse = false;
                SendTheNextMatchIn();
            }
        }

        public static void EnqueueTeam(Team team)
        {
            PKArenaParticipant inMatchParticipant = null;
            List<PKArenaParticipant> ineligibleParticipants = new List<PKArenaParticipant>();
            (from m in matches where m.CurrentState == State.InProgress || m.CurrentState == State.InQueue select m.GetAllParticipants()).ToList().ForEach(x => ineligibleParticipants.AddRange(x));
            foreach(var participant in team.Participants)
            {
                if (ineligibleParticipants.Contains(participant))
                {
                    inMatchParticipant = participant;
                    break;
                }
            }
            if (inMatchParticipant != null)
            {
                foreach (var player in team.Participants)
                {
                    player.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Queue failed, {inMatchParticipant.Player.Name} is already in a match.", ChatMessageType.Broadcast));
                }
                return;
            }            
            
            queue.Enqueue(team);
        }

        public static void DequeueMe(ObjectGuid requestorGuid)
        {
            queue.Dequeue(requestorGuid);
        }
    }
}
