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
using System.Diagnostics;
using System.IO;
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

        private static Queue queue_1s = new Queue();
        private static Queue queue_3s = new Queue();
        private static Queue queue_duelist = new Queue();
        private static List<Match> matches = new List<Match>();


        static MatchManager()
        {
            queue_1s.QueuePop += Queue_QueuePop;
            queue_3s.QueuePop += Queue_QueuePop;
            queue_duelist.QueuePop += Queue_QueuePop;
        }

        public static void SerializeLocations()
        {
            var serializedLocations = Newtonsoft.Json.JsonConvert.SerializeObject(OneOnOneMatchLocations);
            File.WriteAllText("OneOnOneMatchLocations.json", serializedLocations);
        }

        public static MatchLocation GetNextAvailableLocation()
        {
            var availableLocations = OneOnOneMatchLocations.Where(x => !x.InUse).ToList();
            if (availableLocations.Count == 0) return null;
            var matchLocation = availableLocations[ThreadSafeRandom.Next(0, availableLocations.Count - 1)];
            return matchLocation;
        }

        private static List<MatchLocation> OneOnOneMatchLocations = new List<MatchLocation>()
        {
            new MatchLocation(new Position(0xB36F0008, 12f, 180f, 69.580711f, 0.0f, 0.0f, 0.0f, 0.0f)), // Yanshi Platform
            new MatchLocation(new Position(0x15540014, 62f, 84f, 122.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Mount Lethe - Volcano
            new MatchLocation(new Position(0x2D42002F, 141f, 162f, 20.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Obsidian Rim
            new MatchLocation(new Position(0xB6A3001C, 88f, 87f, 1.105250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Waterfalls Near Cragstone
            new MatchLocation(new Position(0xB2AA003E, 179f, 132f, 52.005253f, 0.0f, 0.0f, 0.0f, 0.0f)), // Obsidian Span
            new MatchLocation(new Position(0x2D310021, 108f, 13f, 185.015244f, 0.0f, 0.0f, 0.0f, 0.0f)), // Rynthid Platform
            new MatchLocation(new Position(0x316B002A, 132f, 36f, 320.294708f, 0.0f, 0.0f, 0.0f, 0.0f)), // Crystalline Platforms
            new MatchLocation(new Position(0xBAE80019, 90f, 15f, -0.894750f, 0.0f, 0.0f, 0.0f, 0.0f)), // Aerlinthe Drop
            new MatchLocation(new Position(0x73080006, 18f, 133f, 8.393781f, 0.0f, 0.0f, 0.0f, 0.0f)), // Ulgrim's Island
            new MatchLocation(new Position(0x1035003D, 189f, 109f, 72.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Ayan LS
            new MatchLocation(new Position(0x09050001, 14f, 15f, 87.197716f, 0.0f, 0.0f, 0.0f, 0.0f)), // Caul Drop
            new MatchLocation(new Position(0x7D670011, 48f, 0f, 10.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Yaraq Auroch Cows
            new MatchLocation(new Position(0x02E3018B, 96f, -55f, 12.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Yaraq PK Arena
            new MatchLocation(new Position(0x050F0040, 180f, 181f, 416.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Caul Rim
            new MatchLocation(new Position(0x1203002C, 135f, 84f, 31.604441f, 0.0f, 0.0f, 0.0f, 0.0f)), // Lugian Island
            new MatchLocation(new Position(0xC3AA002F, 124f, 167f, 114.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Subway Outside
            new MatchLocation(new Position(0x01C90235, 80f, -39f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Subway (Inside)
            new MatchLocation(new Position(0x01C90114, 90f, -185f, -71.994751f, 0.0f, 0.0f, 0.0f, 0.0f)), // Subway (Bottom)
            new MatchLocation(new Position(0xCE950038, 150f, 174f, 20.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Eastham
            new MatchLocation(new Position(0xF5190018, 71f, 178f, 100.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Sanctuary
            new MatchLocation(new Position(0xED0D0018, 60f, 189f, -0.894750f, 0.0f, 0.0f, 0.0f, 0.0f)), // Alt Sanctuary
            new MatchLocation(new Position(0x0066011B, 44f, -25f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // PKArena East
            new MatchLocation(new Position(0x00660108, 13f, -25f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // PKArena West
            new MatchLocation(new Position(0x215D001B, 74f, 70f, 26.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Vale of Giant Flowers
            new MatchLocation(new Position(0x5E430104, 20f, -21f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Throne of the Tusker King
            new MatchLocation(new Position(0x01A20437, 69f, -77f, -11.994750f, 0.0f, 0.0f, 0.0f, 0.0f)), // Swamp Temple
            new MatchLocation(new Position(0x40310017, 60f, 158f, 148.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Small Black HIll
            new MatchLocation(new Position(0xEB4E002F, 143f, 151f, 6.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Hebian-To Outdoors
            new MatchLocation(new Position(0xBDD20034, 156f, 84f, 198.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Bandit Castle
            new MatchLocation(new Position(0x26810004, 9f, 86f, 220.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Fort Teth
            new MatchLocation(new Position(0x28940023, 114f, 51f, 30.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Dark Spiral
            new MatchLocation(new Position(0x1B900036, 167f, 132f, 30.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Outside Teth BSD
            new MatchLocation(new Position(0x1338003F, 191f, 160f, 98.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Outside Ayan BSD
            new MatchLocation(new Position(0xD054003C, 178f, 83f, 238.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Shoushi Renegade Stronghold
            new MatchLocation(new Position(0x31D60134, 72f, 145f, 80.105278f, 0.0f, 0.0f, 0.0f, 0.0f)), // Sanamar Royal Hall
            new MatchLocation(new Position(0x38F20025, 106f, 107f, 100.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Shadow Pass
            new MatchLocation(new Position(0xD43D0008, 4f, 190f, 520.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Southern Osteth Peak
            new MatchLocation(new Position(0xF92F0032, 156f, 36f, 50.005253f, 0.0f, 0.0f, 0.0f, 0.0f)), // Freebooter Exploration Stone
            new MatchLocation(new Position(0x13960031, 161f, 21f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // Most Northern Direlands Point
            new MatchLocation(new Position(0x7D82003D, 173f, 111f, 154.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Outside Al-Jalima
            new MatchLocation(new Position(0x231D0022, 117f, 43f, 56.005253f, 0.0f, 0.0f, 0.0f, 0.0f)), // Planetarium
            new MatchLocation(new Position(0x40E7000A, 29f, 35f, 200.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Corcima Castle
            new MatchLocation(new Position(0xB997001B, 89f, 63f, 22.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Chapel Blackspire
            new MatchLocation(new Position(0x20E70038, 155f, 178f, 58.005253f, 0.0f, 0.0f, 0.0f, 0.0f)), // Cataracts of Sabella
            new MatchLocation(new Position(0x4AE2003E, 175f, 141f, 172.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Hoshino Fortress
            new MatchLocation(new Position(0x4BE20003, 13f, 72f, 211.304153f, 0.0f, 0.0f, 0.0f, 0.0f)), // Hoshino Tower
            new MatchLocation(new Position(0x9F29000E, 42f, 133f, 290.005249f, 0.0f, 0.0f, 0.0f, 0.0f)), // Frost Haven
            new MatchLocation(new Position(0xC8E90023, 118f, 54f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f)), // 150 LS
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
                    if (player.Player != null)
                    {
                        player.Player.IsBusy = false;
                        player.Player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(player.Player.Guid, () => HandleConfirmation(matchup, true, player.Player)), msg);
                    }
                }
            }
            else
            {
                foreach (var player in matchup.GetAllParticipants())
                {
                    if (player.PlayerGuid == playerThatConfirmed.Guid)
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
            var nextMatch = matchesInQueue[0];
            var matchLocation = GetNextAvailableLocation();
            matchLocation.InUse = true;
            nextMatch.FightLocation = matchLocation;
            nextMatch.CurrentState = State.InProgress;
            foreach(var p in nextMatch.GetAllParticipants())
            {
                if (p.Player != null)
                {
                    p.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Here we go! You are being transported to the fight location.\nDon't forget to turn off vTank!", ChatMessageType.Broadcast));
                    p.PriorLocation = p.Player.Location;
                    p.Player.Attackable = false;

                    DispellNegativeEnchantments(p.Player);
                    MaxVitals(p.Player);

                    WorldManager.ThreadSafeTeleport(p.Player, new Position(matchLocation.TeamTwoPos));
                }
            }
            foreach (var p in nextMatch.TeamOne.Participants)
            {
                if (p.Player != null)
                {
                    WorldManager.ThreadSafeTeleport(p.Player, new Position(matchLocation.TeamOnePos));
                }                
            }
            foreach (var p in nextMatch.TeamTwo.Participants)
            {
                if (p.Player != null)
                {
                    WorldManager.ThreadSafeTeleport(p.Player, new Position(matchLocation.TeamTwoPos));
                }
            }
            Task.Factory.StartNew(() => WatchForQuitters(nextMatch));
        }

        private static void MaxVitals(Player player)
        {
            var msgHealthUpdate = new GameMessagePrivateUpdateAttribute2ndLevel(player, Vital.Health, player.Health.MaxValue);
            var msgStaminaUpdate = new GameMessagePrivateUpdateAttribute2ndLevel(player, Vital.Stamina, player.Stamina.MaxValue);
            var msgManaUpdate = new GameMessagePrivateUpdateAttribute2ndLevel(player, Vital.Mana, player.Mana.MaxValue);

            player.UpdateVital(player.Health, player.Health.MaxValue);
            player.UpdateVital(player.Stamina, player.Stamina.MaxValue);
            player.UpdateVital(player.Mana, player.Mana.MaxValue);
            
            player.Session.Network.EnqueueSend(msgHealthUpdate, msgStaminaUpdate, msgManaUpdate);
            
            player.OnHealthUpdate();
        }

        private static void DispellNegativeEnchantments(Player player)
        {
            List<Spell> dispellSpells = new List<Spell>()
                    {
                        new Spell(SpellId.DispelLifeBadOther8),
                        new Spell(SpellId.DispelCreatureBadOther8),
                        new Spell(SpellId.DispelItemGoodOther8),
                    };

            foreach (var spell in dispellSpells)
            {
                player.TryCastSpell(spell, player, null, false);
            }
        }

        private static void WatchForQuitters(Match matchup)
        {
            foreach(var p in matchup.GetAllParticipants())
            {
                var player = p.Player;
                if (player != null)
                {
                    p.SetNpkStatus();
                    player.SetCombatMode(CombatMode.NonCombat);
                    player.IsBusy = false;
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Fight starts in {15} seconds", ChatMessageType.Broadcast));
                }
            }
            matchup.SpawnBarriers();
            // wait for players to portal in
            Thread.Sleep(15000);

            // do countdown
            int timeToStart = 3;
            while (timeToStart > 0)
            {
                matchup.GetAllParticipants().ForEach(x => x.Player?.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Fight starts in {timeToStart}...", ChatMessageType.Broadcast)));
                Thread.Sleep(1000);
                timeToStart--;
            }
            matchup.StartTime = DateTime.Now;
            Stopwatch matchTimer = Stopwatch.StartNew();
            matchup.GetAllParticipants().ForEach(p => p.SetPklStatus());
            matchup.GetAllParticipants().ForEach(x => x.Player?.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] GO!", ChatMessageType.Broadcast)));

            List<KeyValuePair<ObjectGuid, int>> playersWarned = new List<KeyValuePair<ObjectGuid, int>>();
            while(matchup.CurrentState == State.InProgress)
            {                
                foreach (var participant in matchup.GetAllParticipants())
                {
                    if (participant.Player != null)
                    {
                        int playerWarnings = playersWarned.Where(x => x.Key == participant.PlayerGuid).FirstOrDefault().Value;
                        bool maxWarningLimitReached = playerWarnings == 3;

                        Position playerLocation = participant.Player.Location;
                        float distanceToMidpoint = playerLocation.DistanceTo(matchup.FightLocation.MidPoint);
                        if (distanceToMidpoint > matchup.FightLocation.Radius)
                        {
                            if (maxWarningLimitReached)
                            {
                                participant.HandleDeath(null, null);
                                playersWarned.RemoveAll(x => x.Key == participant.PlayerGuid);
                                break;
                            }
                            participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {(playerWarnings == 2 ? "LAST WARNING! " : "")}You are too far from the fight location. Turn back immediately or get disqualified!", ChatMessageType.Broadcast));

                            if (playerWarnings == 0)
                                playersWarned.Add(new KeyValuePair<ObjectGuid, int>(participant.PlayerGuid, 1));
                            else
                            {
                                playersWarned.RemoveAll(x => x.Key == participant.PlayerGuid);
                                playersWarned.Add(new KeyValuePair<ObjectGuid, int>(participant.PlayerGuid, playerWarnings+1));
                            }
                        }
                        else
                        {
                            if (playerWarnings > 0)
                            {
                                playersWarned.RemoveAll(x => x.Key == participant.PlayerGuid);
                            }
                        }

                        if (matchTimer.ElapsedMilliseconds > matchup.TimeLimit.TotalMilliseconds) // 20 minute time limit
                        {
                            participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Ending match due to time length exceeded.", ChatMessageType.Broadcast));
                            participant.HandleDraw();
                        }
                    }
                    
                }
                if (matchTimer.ElapsedMilliseconds > matchup.TimeLimit.TotalMilliseconds) // 20 minute time limit
                {
                    matchup.HandleMatchCompleted(null);
                    break;
                }
                Thread.Sleep(2500);
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

        public static bool AreThesePlayersInTheSameInProgressMatch(Player pSource, Player pTarget)
        {
            bool returnValue = false;
            if (pSource != null && pTarget != null)
            {
                var sourceParticipant = TryGetInProgressParticipant(pSource);
                if (sourceParticipant != null)
                {
                    var targetParticipant = TryGetInProgressParticipant(pTarget);
                    if (targetParticipant != null)
                    {
                        var sourceMatch = TryGetInProgressMatchByParticipant(sourceParticipant);
                        var targetMatch = TryGetInProgressMatchByParticipant(targetParticipant);
                        if (sourceMatch.Equals(targetMatch)) // all participants should be able to damage each other, even friendlies
                        {
                            returnValue = true;
                        }
                    }
                }
            }
            return returnValue;
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
                participantsMatch.HandleMatchCompleted(teamOneWon);
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
                if (ineligibleParticipants.Select(x => x.PlayerGuid).ToList().Contains(participant.PlayerGuid))
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

            if (team.Participants.Count == 3)
            {
                if (queue_3s.Enqueue(team)) log.Info($"Team was added to 3s queue");
            }
            if (team.Participants.Count == 1)
            {
                if (team.Participants[0].Player.Level == 300)
                {
                    if (queue_duelist.Enqueue(team)) log.Info($"Team was added to duelist queue");
                }
                else
                {
                    if (queue_1s.Enqueue(team)) log.Info($"Team was added to 1s queue");
                }
            }
        }

        public static void DequeueMe(ObjectGuid requestorGuid)
        {
            if (queue_1s.Dequeue(requestorGuid)) log.Info($"{PlayerManager.FindByGuid(requestorGuid).Name} was removed from 1s queue");
            if (queue_3s.Dequeue(requestorGuid)) log.Info($"{PlayerManager.FindByGuid(requestorGuid).Name}'s team was removed from 3s queue");
            if (queue_duelist.Dequeue(requestorGuid)) log.Info($"{PlayerManager.FindByGuid(requestorGuid).Name} was removed from duelist queue");
        }
    }
}
