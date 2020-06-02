using ACE.Common;
using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Command.Handlers.Processors;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.ShoffsMods;
using ACE.Server.ShoffsMods.PKArena;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;
using ACE.Server.WorldObjects.Managers;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ACE.Server.Command.Handlers
{
    public static class PlayerCommands_ShoffsMods
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [CommandHandler("testfunc", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
        public static void HandleTestFunc(Session session, params string[] parameters)
        {
            //Task.Factory.StartNew(() => WispWriter.WriteWispString("3", session.Player.Location.InFrontOf(10), WispWriter.Color.Blue, 0.8));
            //Thread.Sleep(1000);
            //Task.Factory.StartNew(() => WispWriter.WriteWispString("2", session.Player.Location.InFrontOf(10), WispWriter.Color.Blue, 0.8));
            //Thread.Sleep(1000);
            //Task.Factory.StartNew(() => WispWriter.WriteWispString("1", session.Player.Location.InFrontOf(10), WispWriter.Color.Blue, 0.8));
            //Thread.Sleep(1000);
            Task.Factory.StartNew(() => WispWriter.WriteWispString("FIGHT", session.Player.Location.InFrontOf(10), WispWriter.Color.Red, 3));
        }

        [CommandHandler("getmyenlightenmentcerts", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0)]
        public static void HandleGetEnlightenmentCerts(Session session, params string[] parameters)
        {
            var certs = session.Player.Inventory.Values.Where(x => x.WeenieClassId == 46420 || x.WeenieClassId == 46421);
            if (certs.Any())
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Wtf dude. I'm telling Shoff.", ChatMessageType.Broadcast));
            }
            else
            {
                if (session.Player.Enlightenment > 0)
                {
                    // add reset certs
                    var skillResetCert = WorldObjectFactory.CreateNewWorldObject(46420);
                    var attributeResetCert = WorldObjectFactory.CreateNewWorldObject(46421);
                    session.Player.TryCreateInInventoryWithNetworking(skillResetCert);
                    session.Player.TryCreateInInventoryWithNetworking(attributeResetCert);

                    session.Network.EnqueueSend(new GameMessageSystemChat($"The free reset certificates have been added to your inventory. Please don't use this temporary command again.", ChatMessageType.Broadcast));
                }
                else
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"This temporary command is for Enlightened players only.", ChatMessageType.Broadcast));
                }
            }

        }

        [CommandHandler("sortinv", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
        public static void HandleSortInventory(Session session, params string[] parameters)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat($"Starting inventory sorting.", ChatMessageType.Broadcast));
            List<KeyValuePair<ObjectGuid, WorldObject>> sortedInventory = new List<KeyValuePair<ObjectGuid, WorldObject>>();
            sortedInventory.AddRange(session.Player.Inventory.Where(x => x.Value.ContainerType == ContainerType.NonContainer));
            sortedInventory.Sort((x, y) => (int)((x.Value.ValidLocations ?? EquipMask.None) & (y.Value.ValidLocations ?? EquipMask.None)));
            foreach(var item in sortedInventory)
            {
                session.Player.TryConsumeFromInventoryWithNetworking(item.Value);
                session.Player.TryCreateInInventoryWithNetworking(item.Value);
            }
            session.Network.EnqueueSend(new GameMessageSystemChat($"Inventory sorting complete.", ChatMessageType.Broadcast));
        }

        [CommandHandler("xp", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0)]
        public static void HandleCurXp(Session session, params string[] parameters)
        {
            float xpScale = 1.0f + session.Player.AugmentationBonusXp * 0.05f; // quick learner aug. this gets applied first, then multiplied by the following

            xpScale *= session.Player.EnchantmentManager.GetXPMod(); // augmented understanding trinket
            xpScale *= (float)(session.Player.GetProperty(PropertyFloat.GlobalXpMod) ?? 1.0f); // player xp modifier

            if (session.Player.IsInDailyDungeon)
            {
                xpScale *= DailyDungeonProperties.GetMyDailyDungeonXpMultiplier(session.Player);
            }

            xpScale *= (float)(PropertyManager.GetDouble("xp_modifier").Item); // server-wide xp modifier

            session.Network.EnqueueSend(new GameMessageSystemChat($"Your current xp scale is: {MathF.Round(xpScale * 100.0f)}%", ChatMessageType.Broadcast));
        }

        [CommandHandler("dd", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
        public static void HandleDailyDungeonRefresh(Session session, params string[] parameters)
        {
            DeveloperContentCommands.HandleClearCache(session, parameters);
            DeveloperCommands.HandleReloadLandblocks(session, parameters);
        }

        [CommandHandler("dq", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Dequeues you from the duel queue")]
        public static void HandlePlayerDequeueShort(Session session, params string[] parameters)
        {
            HandlePlayerDequeue(session, parameters);
        }

        [CommandHandler("dequeue", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Dequeues you from the duel queue")]
        public static void HandlePlayerDequeue(Session session, params string[] parameters)
        {
            MatchManager.DequeueMe(session.Player.Guid);
        }
        
        [CommandHandler("spectate", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Spectate a random match or specific player in the PvP queue",
            "[(optional) player name]")]
        public static void HandleSpectate(Session session, params string[] parameters)
        {
            string playerName = string.Empty;
            if (parameters.Length > 0)
            {
                playerName = string.Join(' ', parameters);
            }
            MatchManager.Spectate(session.Player, playerName);
        }

        [CommandHandler("q", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1,
                        @"
Queue system commands:
  * /q me - Queues you to the appropriate solo-queue for a 1v1 duel. You will be given assigned a Skill Rating from these fights that is displayed on your ID panel as your Chess Rank.
  * /q me team - Queues you for match-making with other players for an evenly matched team fight.
  
  * /q us - Queues your fellowship for an evenly matched team fight.
  * /q us team - Queues your fellowship for match-making with other players for an evenly matched team fight.
  * NOTE: Evenly matched refers to the number of players on a team.
  
  * /dq - Dequeues you (and your fellowship) from the PvP queue.
",
            "[me or us] [(optional) team] [(optional) <room #>]")]
        public static void HandlePlayerQueueShort(Session session, params string[] parameters)
        {
            HandlePlayerQueue(session, parameters);
        }
        
        [CommandHandler("queue", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1,
            @"
Queue system commands:
  * /q me - Queues you to the appropriate solo-queue for a 1v1 duel. You will be given assigned a Skill Rating from these fights that is displayed on your ID panel as your Chess Rank.
  * /q me team - Queues you for match-making with other players for an evenly matched team fight.
  
  * /q us - Queues your fellowship for an evenly matched team fight.
  * /q us team - Queues your fellowship for match-making with other players for an evenly matched team fight.
  * NOTE: Evenly matched refers to the number of players on a team.
  
  * /dq - Dequeues you (and your fellowship) from the PvP queue.
",
            "[me or us] [(optional) team] [(optional) <room #>]")]
        public static void HandlePlayerQueue(Session session, params string[] parameters)
        {
            if (parameters.Length >= 1)
            {
                var fellowshipMembers = session.Player.GetFellowshipTargets();
                int roomNum = fellowshipMembers.Count * -1;
                roomNum = session.Player.Level > 275 ? session.Player.Level ?? 0 * -1 : roomNum;
                bool doTeamMatchmaking = false;
                if (parameters.Length >= 2)
                {
                    if (parameters[1].Equals("team", StringComparison.InvariantCultureIgnoreCase))
                    {
                        doTeamMatchmaking = true;
                        roomNum = -1000000;
                        if (parameters.Length == 3)
                        {
                            if (int.TryParse(parameters[2], out int n))
                            {
                                session.Network.EnqueueSend(new GameMessageSystemChat($"Private rooms aren't available with team matchmaking queue.", ChatMessageType.Broadcast));
                            }
                        }
                    }
                    else
                    {
                        if (int.TryParse(parameters[1], out int n))
                        {
                            if (n >= 0)
                            {
                                roomNum = n;
                            }
                        }
                    }                    
                }
                if (parameters[0].Equals("me", StringComparison.OrdinalIgnoreCase))
                {                    
                    if (session.Player != null)
                    {
                        Team team = new Team();
                        PKArenaParticipant me = new PKArenaParticipant(session.Player.Guid);
                        team.Participants.Add(me);
                        MatchManager.EnqueueTeam(team, roomNum);
                    }
                }
                if (parameters[0].Equals("us", StringComparison.OrdinalIgnoreCase))
                {
                    if (fellowshipMembers.Count < 2)
                    {
                        session.Network.EnqueueSend(new GameMessageSystemChat($"The team queue is only available for fellowships of 2 or more. Your fellowship has {fellowshipMembers.Count} member{(fellowshipMembers.Count > 1 ? "s" : "")}.", ChatMessageType.Broadcast));
                        return;
                    }
                    else
                    {
                        if (doTeamMatchmaking && fellowshipMembers.Count > 6)
                        {
                            session.Network.EnqueueSend(new GameMessageSystemChat($"The team match-making queue is only available for fellowships of 6 or less. Your fellowship has {fellowshipMembers.Count} member{(fellowshipMembers.Count > 1 ? "s" : "")}.", ChatMessageType.Broadcast));
                            return;
                        }

                        var team = new Team();
                        fellowshipMembers.ForEach(x => team.Participants.Add(new PKArenaParticipant(x.Guid)));
                        SendMatchConfirmation(team, session.Player, roomNum);
                        return;
                    }
                }
            }
            else
            {
                session.Network.EnqueueSend(new GameMessageSystemChat($"Useage: /q [me or us] [(optional) team] [(optional) <room #>]. Example: /q me", ChatMessageType.Broadcast));
            }
        }

        private static void SendMatchConfirmation(Team team, Player requestingPlayer, int roomNum)
        {
            HandleConfirmation(team, roomNum, false, requestingPlayer);
            Task.Factory.StartNew(() => WatchForNonResponders(team));
        }

        private static void WatchForNonResponders(Team team)
        {
            Thread.Sleep(5000);
            List<PKArenaParticipant> participantsThatDidntAccept = team.Participants.Where(x => !x.AcceptedQueue).ToList();
            foreach (var participant in participantsThatDidntAccept)
            {
                participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have 10 seconds to accept.", ChatMessageType.Broadcast));
            }
            Thread.Sleep(5000);
            uint secondsRemaining = 5;
            participantsThatDidntAccept = team.Participants.Where(x => !x.AcceptedQueue).ToList();
            while (secondsRemaining > 0 && participantsThatDidntAccept.Count > 0)
            {
                foreach (var participant in participantsThatDidntAccept)
                {
                    participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] You have {secondsRemaining} seconds to accept.", ChatMessageType.Broadcast));
                }
                Thread.Sleep(1000);
                secondsRemaining--;
                participantsThatDidntAccept = team.Participants.Where(x => !x.AcceptedQueue).ToList();
            }
            if (participantsThatDidntAccept.Count > 0)
            {
                // someone didn't accept/respond
                var namesThatDidntAccept = string.Join(", ", from p in participantsThatDidntAccept select p.Player.Name);
                foreach (var participant in team.Participants)
                {
                    participant.Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"[PvP Queue] {namesThatDidntAccept} did not accept the queue invite. Queueing canceled.", ChatMessageType.Broadcast));
                }
            }
        }

        private static void HandleConfirmation(Team team, int roomNum, bool playerConfirmed = false, Player playerThatConfirmed = null)
        {
            if (!playerConfirmed)
            {
                var msg = $"{playerThatConfirmed.Name} is queueing your fellowship for team PvP!\nAre you in?";
                foreach (var participant in team.Participants)
                {
                    if (participant.Player != null)
                    {
                        participant.Player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(participant.PlayerGuid, () => HandleConfirmation(team, roomNum, true, participant.Player)), msg);
                    }
                }
            }
            else
            {                
                foreach (var participant in team.Participants)
                {
                    if (participant.PlayerGuid == playerThatConfirmed.Guid)
                    {
                        participant.AcceptedQueue = playerConfirmed;
                    }
                }
                if (team.AllPlayersAcceptedQueue())
                {
                    MatchManager.EnqueueTeam(team, roomNum);
                }
            }
        }

        /// <summary>
        /// Using this to trigger enlightenment until the NPC is introduced
        /// </summary>
        [CommandHandler("enlighten", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Begins the enlightenment process! Full details and a confirmation box will be sent upon use.",
            "/enlighten")]
        public static void HandlePlayerEnlightenment(Session session, params string[] parameters)
        {
            SendEnlightenConfirmation(session.Player);
        }

        private static void SendEnlightenConfirmation(Player requestingPlayer)
        {
            HandleEnlightenConfirmation(requestingPlayer.Guid);
        }
        
        private static void HandleEnlightenConfirmation(ObjectGuid requestorGuid, bool confirmed = false)
        {
            Player requestor = PlayerManager.GetOnlinePlayer(requestorGuid);
            if (requestor != null)
            {
                if (!confirmed)
                {
                    float xpScale = Enlightenment.CalculateXpNerf((uint)requestor.Enlightenment+1);
                    var msg = $@"
ENLIGHTENMENT:
    - Requirements:
       * Level 275
       * Have 25 unused pack spaces
       * Enlightenment is only available during the first 7 days of the month
       * *** Given these requirements, you are currently {(Enlightenment.VerifyRequirements(requestor) ? "eligible!" : "ineligieble!")} ***
    - You lose:
       * All experience, reverting to level 1.
       * The ability to use aetheria (until you attain sufficient level).
       * The ability to equip and use items which have skill and level requirements beyond those of a level 1 character. Any equipped items are moved into your pack automatically.
       * The ability to receive passup XP until level 150.
       * The ability to receive quest XP until level 150.
       * Your current location. You will be teleported to Holtburg.
    - You keep:
       * All augmentations obtained through Augmentation Gems.
       * All luminanace.
       * All skill credits and your template.
       * All quest flags.
    - You gain:
       * A new title each time you enlighten.
       * +2 to vitality.
       * +1 to all of your skills.
       * A free Skill Reset Certificate
       * A free Attribute Reset Certificate
       * A permanent XP bonus for levels 150+ (200%).
       * An experience nerf, that scales with your Enlightenment rank, for levels 1-149 (you'll receive {MathF.Round(xpScale*100.0f, MidpointRounding.AwayFromZero)}% XP).
";

                    requestor.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
                    requestor.ConfirmationManager.EnqueueSend(new Confirmation_Custom(requestorGuid, () => HandleEnlightenConfirmation(requestorGuid, true)), "Are you sure you want to do this?!\nSee your chat for details.");
                }
                else
                {
                    if (Enlightenment.VerifyRequirements(requestor))
                    {
                        if (!requestor.IsBusy)
                        {
                            var spellFreeRide = DatabaseManager.World.GetCachedSpell((uint)SpellId.PortalSendHoltburg); // Free Ride to Holtburg
                            var holtDrop = new Position(spellFreeRide.PositionObjCellId.Value, spellFreeRide.PositionOriginX.Value, spellFreeRide.PositionOriginY.Value, spellFreeRide.PositionOriginZ.Value, spellFreeRide.PositionAnglesX.Value, spellFreeRide.PositionAnglesY.Value, spellFreeRide.PositionAnglesZ.Value, spellFreeRide.PositionAnglesW.Value);

                            Enlightenment.HandleEnlightenment(requestor);

                            WorldManager.ThreadSafeTeleport(requestor, holtDrop, new ActionEventDelegate(() =>
                            {
                                // Stand back up
                                requestor.SetCombatMode(CombatMode.NonCombat);

                                var teleportChain = new ActionChain();
                                teleportChain.AddDelaySeconds(3.0f);
                                teleportChain.AddAction(requestor, () =>
                                {
                                    // reset damage history for this player
                                    requestor.DamageHistory.Reset();
                                });

                                teleportChain.EnqueueChain();
                            }));
                        }
                        else
                        {
                            requestor.Session.Network.EnqueueSend(new GameEventWeenieError(requestor.Session, WeenieError.YoureTooBusy));
                        }                        
                    }
                }
            }            
        }

        /// <summary>
        /// Creates a mule on your account with the specificed name. Useage: /create_mule [character name] 
        /// </summary>
        [CommandHandler("mule", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Creates a mule on your account with the specificed name",
            "[character name]")]
        public static void HandleMule(Session session, params string[] parameters)
        {
            uint weenieClassId;

            uint[] standardMuleWcids = new uint[] { 14, 618, 23623 };
            uint[] rareMuleWcids = new uint[] { 29504 };
            bool isRareCreation = ThreadSafeRandom.Next(1, 1000) <= 1;
            if (isRareCreation)
            {
                weenieClassId = rareMuleWcids[ThreadSafeRandom.Next(0, rareMuleWcids.Length - 1)];
            }
            else
            {
                weenieClassId = standardMuleWcids[ThreadSafeRandom.Next(0, standardMuleWcids.Length - 1)];
            }

            Weenie weenie = DatabaseManager.World.GetCachedWeenie(weenieClassId);

            var guid = GuidManager.NewPlayerGuid();

            var mule = new Player(weenie, guid, session.AccountId);

            List<string> nameParams = new List<string>();
            nameParams.AddRange(parameters);
            if (nameParams.Count == 0)
            {
                foreach (var component in session.Player.Name.Split(' '))
                {
                    nameParams.Add(component);
                }
                nameParams[nameParams.Count-1]+= "'s";
                nameParams.Add("Mule");
            }
            var name = GetCharacterName(session, nameParams);
            if (name == string.Empty) return;

            mule.Name = name;
            mule.Character.Name = name;

            mule.Biota.WeenieType = session.Player.WeenieType;

            DatabaseManager.Shard.IsCharacterNameAvailable(name, isAvailable =>
            {
                if (!isAvailable)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"{name} is not available to use for the mule character, try another name.", ChatMessageType.Broadcast);
                    return;
                }
                else
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Creating a mule for you named: {name}\n... You will be logged out.", ChatMessageType.Broadcast));
                }

                PlayerMutator.Mutate(mule, session.Player, PlayerMutator.CharacterType.Mule);

                // make them viable mules
                mule.SetProperty(PropertyString.Template, "Mule");
                mule.Strength.StartingValue = (uint)(isRareCreation ? 270 : 250);

                var possessedBiotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();

                DatabaseManager.Shard.AddCharacterInParallel(mule.Biota, mule.BiotaDatabaseLock, possessedBiotas, mule.Character, mule.CharacterDatabaseLock, saveSuccess =>
                {
                    if (!saveSuccess)
                    {
                        return;
                    }

                    PlayerManager.AddOfflinePlayer(mule);
                    session.Characters.Add(mule.Character);
                });

                session.LogOffPlayer();
            });
        }

        [CommandHandler("duelist", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Creates a duelist on your account with the specificed name.",
            "[character name]")]
        public static void HandleCreateDuelist(Session session, params string[] parameters)
        {
            var guid = GuidManager.NewPlayerGuid();

            var weenie = DatabaseManager.World.GetCachedWeenie(session.Player.WeenieClassId);
            var duelist = new Player(weenie, guid, session.AccountId);

            // set the character name
            List<string> nameParams = new List<string>();
            nameParams.AddRange(parameters);
            if (nameParams.Count == 0)
            {
                foreach (var component in session.Player.Name.Split(' '))
                {
                    nameParams.Add(component);
                }
                nameParams[nameParams.Count - 1] += "'s";
                nameParams.Add("Duelist");
            }
            var name = GetCharacterName(session, nameParams);

            if (name == string.Empty) return;
            
            duelist.Name = name;
            duelist.Character.Name = name;

            DatabaseManager.Shard.IsCharacterNameAvailable(name, isAvailable =>
            {
                if (!isAvailable)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"{name} is not available to use for the duelist character, try another name.", ChatMessageType.Broadcast);
                    return;
                }
                else
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Creating a duelist for you named: {name}\n... You will be logged out.", ChatMessageType.Broadcast));
                }

                PlayerMutator.Mutate(duelist, session.Player, PlayerMutator.CharacterType.MageDuelist);
                

                var possessions = duelist.GetAllPossessions();
                var possessedBiotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();
                foreach (var possession in possessions)
                    possessedBiotas.Add((possession.Biota, possession.BiotaDatabaseLock));

                DatabaseManager.Shard.AddCharacterInParallel(duelist.Biota, duelist.BiotaDatabaseLock, possessedBiotas, duelist.Character, duelist.CharacterDatabaseLock, saveSuccess =>
                {
                    if (!saveSuccess)
                    {
                        return;
                    }

                    PlayerManager.AddOfflinePlayer(duelist);
                    session.Characters.Add(duelist.Character);
                });

                session.LogOffPlayer();
            });
        }

        [CommandHandler("mutant", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Creates a level 500 mutant on your account with the specificed name.",
            "[character name]")]
        public static void HandleCreateNightmare(Session session, params string[] parameters)
        {
            var guid = GuidManager.NewPlayerGuid();

            var weenie = DatabaseManager.World.GetCachedWeenie(session.Player.WeenieClassId);
            var nightmare = new Player(weenie, guid, session.AccountId);

            // set the character name
            List<string> nameParams = new List<string>();
            nameParams.AddRange(parameters);
            if (nameParams.Count == 0)
            {
                foreach (var component in session.Player.Name.Split(' '))
                {
                    nameParams.Add(component);
                }
                nameParams[nameParams.Count - 1] += "'s";
                nameParams.Add("Mutant");
            }
            var name = GetCharacterName(session, nameParams);

            if (name == string.Empty) return;

            nightmare.Name = name;
            nightmare.Character.Name = name;

            DatabaseManager.Shard.IsCharacterNameAvailable(name, isAvailable =>
            {
                if (!isAvailable)
                {
                    CommandHandlerHelper.WriteOutputInfo(session, $"{name} is not available to use for the mutant character, try another name.", ChatMessageType.Broadcast);
                    return;
                }
                else
                {
                    session.Network.EnqueueSend(new GameMessageSystemChat($"Creating a mutant for you named: {name}\n... You will be logged out.", ChatMessageType.Broadcast));
                }

                PlayerMutator.Mutate(nightmare, session.Player, PlayerMutator.CharacterType.Nightmare);

                var possessions = nightmare.GetAllPossessions();
                var possessedBiotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();
                foreach (var possession in possessions)
                    possessedBiotas.Add((possession.Biota, possession.BiotaDatabaseLock));

                DatabaseManager.Shard.AddCharacterInParallel(nightmare.Biota, nightmare.BiotaDatabaseLock, possessedBiotas, nightmare.Character, nightmare.CharacterDatabaseLock, saveSuccess =>
                {
                    if (!saveSuccess)
                    {
                        return;
                    }

                    PlayerManager.AddOfflinePlayer(nightmare);
                    session.Characters.Add(nightmare.Character);
                });

                session.LogOffPlayer();
            });
        }

        private static string GetCharacterName(Session session, IEnumerable<string> parameters)
        {
            var name = string.Join(' ', parameters);
            if (parameters.Count() > 0)
            {
                name = name.TrimStart('+').TrimStart(' ').TrimEnd(' ');
                name = Regex.Replace(name, "[^a-zA-Z' ]", "");
            }

            name = name.Substring(0, name.Length > 32 ? 32 : name.Length);

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            name = textInfo.ToLower(name);
            name = textInfo.ToTitleCase(name);

            if (PropertyManager.GetBool("taboo_table").Item && DatManager.PortalDat.TabooTable.ContainsBadWord(name.ToLowerInvariant()) || name.ToLowerInvariant() == "The Resurection")
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"{name} is on the taboo names list, try another name.", ChatMessageType.Broadcast);
                return string.Empty;
            }

            if (PropertyManager.GetBool("creature_name_check").Item && DatabaseManager.World.IsCreatureNameInWorldDatabase(name))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"{name} is not available, try another name.", ChatMessageType.Broadcast);
                return string.Empty;
            }

            return name;
        }

        /// <summary>
        /// Kills vitae for duelists
        /// </summary>
        [CommandHandler("killvp", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Kills vitae for duelists")]
        public static void HandleKillVp(Session session, params string[] parameters)
        {
            if (session.Player.Level > 275) // only available to duelists & mutants
            {
                session.Player.UpdateXpVitae(long.MaxValue);
            }
        }

        public static void DoBuffs(Player buffTarget)
        {
            List<string> spellsToIgnore = new List<string>();
            spellsToIgnore.Add("Strength");
            spellsToIgnore.Add("MagicResistance");
            //spellsToIgnore.Add("AcidProtection");
            //spellsToIgnore.Add("FireProtection");
            //spellsToIgnore.Add("ColdProtection");
            //spellsToIgnore.Add("BladeProtection");
            //spellsToIgnore.Add("BludgeonProtection");
            //spellsToIgnore.Add("PiercingProtection");
            buffTarget.Session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt(buffTarget, PropertyInt.VitaeCpPool, int.MaxValue));
            buffTarget.EnchantmentManager.SendUpdateVitae();
            buffTarget.CreateSentinelBuffPlayers(new Player[] { buffTarget }, false, 8, spellsToIgnore, true);

            buffTarget.Health.Current = buffTarget.Health.MaxValue;
            buffTarget.Stamina.Current = buffTarget.Stamina.MaxValue;
            buffTarget.Mana.Current = buffTarget.Mana.MaxValue;
        }

        public static void FixDuelistSpells(Player p)
        {
            if (p != null && p.Level == 300)
            {
                p.SetCharacterOption(CharacterOption.AllowOthersToSeeYourChessRank, true);
                p.SetCharacterOption(CharacterOption.AllowOthersToSeeYourNumberOfDeaths, true);
                if (p.GetProperty(PropertyString.Template) == string.Empty)
                {
                    p.SetProperty(PropertyString.Template, "War Mage");
                }

                if (p.RemoveKnownSpell((uint)SpellId.LightningStreak8))
                {
                    var spell = new Spell((uint)SpellId.LightningStreak8, false);
                    p.Session.Network.EnqueueSend(new GameMessageSystemChat($"{spell.Name} removed from spellbook.", ChatMessageType.Broadcast));
                }

                p.LearnSpellWithNetworking((uint)SpellId.PortalTieRecall1, true, false);
                p.LearnSpellWithNetworking((uint)SpellId.PortalTieRecall2, true, false);
                p.LearnSpellWithNetworking((uint)SpellId.PortalTie1, true, false);
                p.LearnSpellWithNetworking((uint)SpellId.PortalTie2, true, false);
                p.LearnSpellWithNetworking((uint)SpellId.PortalRecall, true, false);
                p.LearnSpellWithNetworking((uint)SpellId.LifestoneRecall1, true, false);
                p.LearnSpellWithNetworking((uint)SpellId.LifestoneTie1, true, false);
            }
        }

    }
}
