using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
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

        private static Dictionary<int, Queue> room_queue = new Dictionary<int, Queue>();
        private static List<Match> matches = new List<Match>();


        static MatchManager()
        {
            room_queue.Add(0, new Queue()); // duelists
            room_queue.Add(1, new Queue()); // 1s
            room_queue.Add(2, new Queue()); // 2s
            room_queue.Add(3, new Queue()); // 3s
            room_queue.ToList().ForEach(x => x.Value.QueuePop += Queue_QueuePop);
        }

        public static void SerializeLocations()
        {
            var serializedLocations = Newtonsoft.Json.JsonConvert.SerializeObject(OneOnOneMatchLocations);
            File.WriteAllText("OneOnOneMatchLocations.json", serializedLocations);
        }

        public static MatchLocation GetNextAvailableLocation(MatchLocation.LocationType type)
        {
            var availableLocations = OneOnOneMatchLocations.Where(x => !x.InUse).ToList();
            if (type == MatchLocation.LocationType.Team)
                availableLocations.RemoveAll(x => x.Type == MatchLocation.LocationType.OneOnOne);
            if (availableLocations.Count == 0) return null;
            var matchLocation = availableLocations[ThreadSafeRandom.Next(0, availableLocations.Count - 1)];
            return matchLocation;
        }

        private static List<MatchLocation> OneOnOneMatchLocations = new List<MatchLocation>()
        {
            new MatchLocation(new Position(0xB36F0008, 12f, 180f, 69.580711f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Yanshi Platform"), // Yanshi Platform
            new MatchLocation(new Position(0x15540014, 62f, 84f, 122.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Mount Lethe - Volcano"), // Mount Lethe - Volcano
            new MatchLocation(new Position(0x2D42002F, 141f, 162f, 20.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Obsidian Rim"), // Obsidian Rim
            new MatchLocation(new Position(0xB6A3001C, 88f, 87f, 1.105250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Waterfalls Near Cragstone"), // Waterfalls Near Cragstone
            new MatchLocation(new Position(0xB2AA003E, 179f, 132f, 52.005253f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Obsidian Span"), // Obsidian Span
            new MatchLocation(new Position(0x2D310021, 108f, 13f, 185.015244f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Rynthid Platform"), // Rynthid Platform
            new MatchLocation(new Position(0x316B002A, 132f, 36f, 320.294708f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Crystalline Platforms"), // Crystalline Platforms
            new MatchLocation(new Position(0xBAE80019, 90f, 15f, -0.894750f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Aerlinthe Drop"), // Aerlinthe Drop
            new MatchLocation(new Position(0x73080006, 18f, 133f, 8.393781f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Ulgrim's Island"), // Ulgrim's Island
            new MatchLocation(new Position(0x1035003D, 189f, 109f, 72.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Ayan LS"), // Ayan LS
            new MatchLocation(new Position(0x09050001, 14f, 15f, 87.197716f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Caul Drop"), // Caul Drop
            new MatchLocation(new Position(0x7D670011, 48f, 0f, 10.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Yaraq Auroch Cows"), // Yaraq Auroch Cows
            new MatchLocation(new Position(0x02E3018B, 96f, -55f, 12.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Yaraq PK Arena"), // Yaraq PK Arena
            new MatchLocation(new Position(0x050F0040, 180f, 181f, 416.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Caul Rim"), // Caul Rim
            new MatchLocation(new Position(0x1203002C, 135f, 84f, 31.604441f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Lugian Island"), // Lugian Island
            new MatchLocation(new Position(0xC3AA002F, 124f, 167f, 114.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Subway Outside"), // Subway Outside
            new MatchLocation(new Position(0x01C90235, 80f, -39f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Subway (Inside)"), // Subway (Inside)
            new MatchLocation(new Position(0x01C90114, 90f, -185f, -71.994751f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Subway (Bottom)"), // Subway (Bottom)
            new MatchLocation(new Position(0xCE950038, 150f, 174f, 20.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Eastham"), // Eastham
            new MatchLocation(new Position(0xF5190018, 71f, 178f, 100.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Sanctuary"), // Sanctuary
            new MatchLocation(new Position(0xED0D0018, 60f, 189f, -0.894750f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Alt Sanctuary"), // Alt Sanctuary
            new MatchLocation(new Position(0x0066011B, 44f, -25f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "PKArena East"), // PKArena East
            new MatchLocation(new Position(0x00660108, 13f, -25f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "PKArena West"), // PKArena West
            new MatchLocation(new Position(0x215D001B, 74f, 70f, 26.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Vale of Giant Flowers"), // Vale of Giant Flowers
            new MatchLocation(new Position(0x5E430104, 20f, -21f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Throne of the Tusker King"), // Throne of the Tusker King
            new MatchLocation(new Position(0x01A20437, 69f, -77f, -11.994750f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Swamp Temple"), // Swamp Temple
            new MatchLocation(new Position(0x40310017, 60f, 158f, 148.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Small Black HIll"), // Small Black HIll
            new MatchLocation(new Position(0xEB4E002F, 143f, 151f, 6.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Hebian-To Outdoors"), // Hebian-To Outdoors
            new MatchLocation(new Position(0xBDD20034, 156f, 84f, 198.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Bandit Castle"), // Bandit Castle
            new MatchLocation(new Position(0x26810004, 9f, 86f, 220.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Fort Teth"), // Fort Teth
            new MatchLocation(new Position(0x28940023, 114f, 51f, 30.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Dark Spiral"), // Dark Spiral
            new MatchLocation(new Position(0x1B900036, 167f, 132f, 30.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Outside Teth BSD"), // Outside Teth BSD
            new MatchLocation(new Position(0x1338003F, 191f, 160f, 98.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Outside Ayan BSD"), // Outside Ayan BSD
            new MatchLocation(new Position(0xD054003C, 178f, 83f, 238.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Shoushi Renegade Stronghold"), // Shoushi Renegade Stronghold
            new MatchLocation(new Position(0x31D60134, 72f, 145f, 80.105278f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Sanamar Royal Hall"), // Sanamar Royal Hall
            new MatchLocation(new Position(0x38F20025, 106f, 107f, 100.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Shadow Pass"), // Shadow Pass
            new MatchLocation(new Position(0xD43D0008, 4f, 190f, 520.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Southern Osteth Peak"), // Southern Osteth Peak
            new MatchLocation(new Position(0xF92F0032, 156f, 36f, 50.005253f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Freebooter Exploration Stone"), // Freebooter Exploration Stone
            new MatchLocation(new Position(0x13960031, 161f, 21f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Most Northern Direlands Point"), // Most Northern Direlands Point
            new MatchLocation(new Position(0x7D82003D, 173f, 111f, 154.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Outside Al-Jalima"), // Outside Al-Jalima
            new MatchLocation(new Position(0x231D0022, 117f, 43f, 56.005253f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Planetarium"), // Planetarium
            new MatchLocation(new Position(0x40E7000A, 29f, 35f, 200.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Corcima Castle"), // Corcima Castle
            new MatchLocation(new Position(0xB997001B, 89f, 63f, 22.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Chapel Blackspire"), // Chapel Blackspire
            new MatchLocation(new Position(0x20E70038, 155f, 178f, 58.005253f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Cataracts of Sabella"), // Cataracts of Sabella
            new MatchLocation(new Position(0x4AE2003E, 175f, 141f, 172.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Hoshino Fortress"), // Hoshino Fortress
            new MatchLocation(new Position(0x4BE20003, 13f, 72f, 211.304153f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Hoshino Tower"), // Hoshino Tower
            new MatchLocation(new Position(0x9F29000E, 42f, 133f, 290.005249f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Frost Haven"), // Frost Haven
            new MatchLocation(new Position(0xC8E90023, 118f, 54f, 0.005250f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "150 LS"), // 150 LS
            new MatchLocation(new Position(0x039D02A4, 100f, -50f, 48.005001f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "110 Eaters"), // 110 Eaters
            new MatchLocation(new Position(0xEC0E0104, 155f, 83f, 9.824999f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Chapel"), // Chapel
            new MatchLocation(new Position(0x5964010A, 22f, -90f, 0.005000f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Gauntlet (that Legacy beat)"), // Gauntlet (that Legacy beat)
            new MatchLocation(new Position(0xB54A0014, 61f, 80f, 201.199463f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Virindi Rise Treetop"), // Virindi Rise Treetop
            new MatchLocation(new Position(0x013A0358, 105f, -40f, 24.004999f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.Team, "Disco Back Room"), // Disco Back Room
            new MatchLocation(new Position(0x013A01D0, 2f, -50f, 6.005000f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Disco Main Hall"), // Disco Main Hall
            new MatchLocation(new Position(0xBC9F0040, 183f, 178f, 32.005001f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Cragstone Waterfront"), // Cragstone Waterfront
            new MatchLocation(new Position(0x013A03BA, 90f, -70f, 30.004999f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Disco 110"), // Disco 110
            new MatchLocation(new Position(0x5C4B0106, 30f, -70f, -41.994999f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Unknown Dungeon"), // Unknown Dungeon
            new MatchLocation(new Position(0x01A80150, 35f, -65f, 0.005000f, 0.0f, 0.0f, 0.0f, 0.0f), MatchLocation.LocationType.OneOnOne, "Abandoned Arena"), // Abandoned Arena
        };

        private static void Queue_QueuePop(object sender, EventArgs e)
        {
            var args = (QueuePopEventArgs)e;
            SendMatchConfirmation(args.Matchup);
        }

        private static void SendMatchConfirmation(Match matchup)
        {
            matchup.CurrentState = State.InvitePending;
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
                var pConfirmed = matchup.GetAllParticipants().Where(x => x.PlayerGuid == playerThatConfirmed.Guid);
                foreach(var p in pConfirmed)
                {
                    p.AcceptedMatch = true;
                    log.Info($"{p.Player.Name} confirmed");
                }
                if (matchup.AllPlayersConfirmed())
                {
                    log.Info("All players confirmed");
                    SendTheNextMatchIn();
                }                
            }
        }

        public static void Spectate(Player p, string playerName)
        {
            if (p == null) return;
            if (matches.Where(x => x.CurrentState == State.InProgress && x.IsPlayerSpectator(p.Guid)).Count() != 0)
            {
                p.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You are already spectating a fight. Try /spectate [player name] next time.", ChatMessageType.Broadcast));
                return;
            }
            if (string.IsNullOrEmpty(playerName))
            {
                bool matchFound = false;
                foreach (var match in matches)
                {
                    if (match.CurrentState == State.InProgress)
                    {
                        match.AddSpectator(p);
                        matchFound = true;
                        break;
                    }
                }
                if (!matchFound)
                    p.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] No match found.", ChatMessageType.Broadcast));
            }
            else
            {
                var playerToSpectate = PlayerManager.GetOnlinePlayer(playerName);
                if (playerToSpectate != null)
                {
                    var match = GetPlayerMatch(playerToSpectate.Guid);
                    if (match != null)
                    {
                        match.AddSpectator(p);
                    }
                    else
                    {
                        p.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {playerToSpectate.Name} is not currently in a match.", ChatMessageType.Broadcast));
                    }
                }
                else
                {
                    var target = PlayerManager.GetOfflinePlayer(playerName);
                    if (target != null)
                    {
                        p.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {target.Name} is not currently in a match.", ChatMessageType.Broadcast));
                    }
                    else
                    {
                        p.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {target.Name} not found.", ChatMessageType.Broadcast));
                    }
                }
            }

        }

        private static void SendTheNextMatchIn()
        {
            RemoveOfflinePlayersFromInvitePending();
            var matchesInQueue = matches.Where(x => x.CurrentState == State.InvitePending && x.AllPlayersConfirmed()).ToList();
            if (matchesInQueue.Count == 0) return;
            var nextMatch = matchesInQueue[0];
            var matchLocation = GetNextAvailableLocation(nextMatch.GetAllParticipants().Count > 2 ? MatchLocation.LocationType.Team : MatchLocation.LocationType.OneOnOne);
            matchLocation.InUse = true;
            nextMatch.FightLocation = matchLocation;
            nextMatch.CurrentState = State.InProgress;
            foreach(var p in nextMatch.GetAllParticipants())
            {
                if (p.Player != null)
                {
                    p.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Here we go! You are being transported to {matchLocation.Description}.\nDon't forget to turn off vTank!", ChatMessageType.Broadcast));
                    p.PriorLocation = p.Player.Location;
                    p.Player.Attackable = false;

                    DispellNegativeEnchantments(p.Player);
                    MaxVitals(p.Player);
                }
            }
            // check if they need to be fellowed
            Player teamOneLeader = nextMatch.TeamOne.Participants[0].Player;
            Player teamTwoLeader = nextMatch.TeamTwo.Participants[0].Player;
            if (nextMatch.TeamOne.DoFellowship)
            {
                teamOneLeader.FellowshipQuit(false);
                teamOneLeader.FellowshipCreate("Team One", true);
                teamOneLeader.Session.Network.EnqueueSend(new GameEventFellowshipFullUpdate(teamOneLeader.Session));
                teamOneLeader.Session.Network.EnqueueSend(new GameEventFellowshipFellowUpdateDone(teamOneLeader.Session));
            }
            if (nextMatch.TeamTwo.DoFellowship)
            {
                teamTwoLeader.FellowshipQuit(false);
                teamTwoLeader.FellowshipCreate("Team Two", true);
                teamTwoLeader.Session.Network.EnqueueSend(new GameEventFellowshipFullUpdate(teamTwoLeader.Session));
                teamTwoLeader.Session.Network.EnqueueSend(new GameEventFellowshipFellowUpdateDone(teamTwoLeader.Session));
            }
            for (int i = 0; i < nextMatch.TeamOne.Participants.Count; i++)
            {
                var p = nextMatch.TeamOne.Participants[i];

                // teleport them to team one position
                if (p.Player != null)
                {
                    Position teleportLocation = new Position(matchLocation.TeamOnePos);
                    teleportLocation.PositionX += i * 1.0f;
                    WorldManager.ThreadSafeTeleport(p.Player, teleportLocation, new ActionEventDelegate(() =>
                    {
                        var teleportChain = new ActionChain();
                        teleportChain.AddDelaySeconds(3.0f);
                        teleportChain.AddAction(p.Player, () =>
                        {
                            // do fellowship quit & recruit
                            if (nextMatch.TeamOne.DoFellowship && p.Player != teamOneLeader)
                            {
                                p.Player.FellowshipQuit(false);
                                teamOneLeader.Fellowship.AddConfirmedMember(teamOneLeader, p.Player, true);
                            }
                        });

                        teleportChain.EnqueueChain();
                    }
                    ));
                }                
            }
            for (int i = 0; i < nextMatch.TeamTwo.Participants.Count; i++)
            {
                var p = nextMatch.TeamTwo.Participants[i];

                // teleport them to team one position
                if (p.Player != null)
                {
                    Position teleportLocation = new Position(matchLocation.TeamTwoPos);
                    teleportLocation.PositionX += i * 1.0f;
                    WorldManager.ThreadSafeTeleport(p.Player, teleportLocation, new ActionEventDelegate(() =>
                    {
                        var teleportChain = new ActionChain();
                        teleportChain.AddDelaySeconds(3.0f);
                        teleportChain.AddAction(p.Player, () =>
                        {
                            // do fellowship quit & recruit
                            if (nextMatch.TeamTwo.DoFellowship && p.Player != teamTwoLeader)
                            {
                                p.Player.FellowshipQuit(false);
                                teamTwoLeader.Fellowship.AddConfirmedMember(teamTwoLeader, p.Player, true);
                            }
                        });

                        teleportChain.EnqueueChain();
                    }
                    ));
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
                        new Spell(SpellId.DispelItemBadOther8),
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

                    player.SetCharacterOption(CharacterOption.AllowOthersToSeeYourChessRank, true);
                    player.SetCharacterOption(CharacterOption.AllowOthersToSeeYourNumberOfDeaths, true);
                }
            }
            matchup.SpawnBarriers();
            // wait for players to portal in
            Thread.Sleep(12000);

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
            matchup.GetAllParticipants().ForEach(x => x.SetPklStatus());
            matchup.GetAllParticipants().ForEach(x => x.Player?.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] GO!", ChatMessageType.Broadcast)));

            var barrierRadius = matchup.GetBarrierRadius();
            List<KeyValuePair<ObjectGuid, int>> playersWarned = new List<KeyValuePair<ObjectGuid, int>>();
            while(matchup.CurrentState == State.InProgress)
            {                
                foreach (var participant in matchup.GetAllParticipants())
                {
                    if (participant.Player != null)
                    {
                        int playerWarnings = playersWarned.Where(x => x.Key == participant.PlayerGuid).FirstOrDefault().Value;
                        bool maxWarningLimitReached = playerWarnings == 2;

                        Position playerLocation = participant.Player.Location;
                        float distanceToMidpoint = playerLocation.DistanceTo(matchup.FightLocation.MidPoint);
                        if (distanceToMidpoint > barrierRadius)
                        {
                            if (maxWarningLimitReached)
                            {
                                participant.HandleDeath(null, null);
                                playersWarned.RemoveAll(x => x.Key == participant.PlayerGuid);
                                break;
                            }
                            participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {(playerWarnings == 1 ? "LAST WARNING! " : "")}You are too far from the fight location. Turn back immediately or get disqualified!", ChatMessageType.Broadcast));

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

        private static void RemoveOfflinePlayersFromInvitePending()
        {
            var matchesInQueue = matches.Where(x => x.CurrentState == State.InvitePending).ToList();
            List<Match> matchesToRemove = new List<Match>();
            foreach(var match in matchesInQueue)
            {
                bool foundOfflineParticipant = false;
                foreach(var p in match.GetAllParticipants())
                {
                    if (p.Player == null)
                    {
                        foundOfflineParticipant = true;
                        break;
                    }
                }
                if (foundOfflineParticipant)
                    matchesToRemove.Add(match);
            }
            matchesToRemove.ForEach(x => matches.Remove(x));
        }

        public static PKArenaParticipant TryGetInProgressParticipant(Player p)
        {
            var inProgressMatches = from m in matches where m.CurrentState == State.InProgress select m;
            foreach(var match in inProgressMatches)
            {
                foreach(var participant in match.GetAllParticipants())
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
                Task.Factory.StartNew(() => participantsMatch.HandleMatchCompleted(teamOneWon));
                SendTheNextMatchIn();
            }
        }

        public static void EnqueueTeam(Team team, int roomNum, bool doTeamMatchmaking)
        {
            PKArenaParticipant busyPlayer = null;
            foreach (var p in team.Participants)
            {
                if (GetPlayerMatch(p.PlayerGuid) != null)
                {
                    busyPlayer = p;
                    break;
                }
                if (GetPlayerQueue(p.PlayerGuid) != null)
                {
                    busyPlayer = p;
                    break;
                }
            }

            if (busyPlayer != null)
            {
                var bPlayer = PlayerManager.FindByGuid(busyPlayer.PlayerGuid);
                foreach (var participant in team.Participants)
                {
                    if (participant.Player != null)
                    {
                        participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] Queue failed. {bPlayer.Name} is already in a queue or match.", ChatMessageType.Broadcast));
                    }
                }
            }
            else
            {
                Enqueue(team, roomNum);
            }       
        }
        
        private static void Enqueue(Team team, int roomNum)
        {
            if (room_queue.TryGetValue(roomNum, out var q))
            {
                q.Enqueue(team);
            }
            else
            {
                var roomQueue = new Queue();
                if (roomNum == -1000000)
                    roomQueue.DoTeamMatchmaking = true;
                roomQueue.QueuePop += Queue_QueuePop;
                roomQueue.Enqueue(team);

                room_queue.Add(roomNum, roomQueue);
            }
        }

        private static Match GetPlayerMatch(ObjectGuid pGuid)
        {
            foreach (var m in matches.Where(x => x.CurrentState == State.InProgress || x.CurrentState == State.InvitePending))
            {
                if (m.GetAllParticipants().Select(x => x.PlayerGuid).Contains(pGuid))
                {
                    return m;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the Queue in which a player is enqueued.
        /// </summary>
        /// <param name="pGuid">Player.Guid</param>
        /// <returns></returns>
        private static Queue GetPlayerQueue(ObjectGuid pGuid)
        {
            foreach (var q in room_queue.Values)
            {
                if (q.GetAllParticipantGuids().Contains(pGuid))
                {
                    return q;
                }
            }
            return null;
        }

        public static void DequeueMe(ObjectGuid requestorGuid)
        {
            var playerQueue = GetPlayerQueue(requestorGuid);
            if (playerQueue != null)
            {
                playerQueue.Dequeue(requestorGuid);
            }
            else
            {
                var player = PlayerManager.GetOnlinePlayer(requestorGuid);
                if (player != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You are not currently in a queue.", ChatMessageType.Broadcast));
                }
            }
        }
    }
}
