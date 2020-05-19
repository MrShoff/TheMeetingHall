using ACE.Common;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.ShoffsMods.PKArena;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;
using ACE.Server.WorldObjects.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ACE.Server.Command.Handlers
{
    public static class PlayerCommands_ShoffsMods
    {

        /// <summary>
        /// Creates a mule on your account with the specificed name. Useage: /mule [character name] 
        /// </summary>
        [CommandHandler("testfunc", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Creates a mule on your account with the specificed name",
            "/mule [character name]")]
        public static void HandleTestFunc(Session session, params string[] parameters)
        {
            //_ = session.Player.IsInDailyDungeon;
        }



        /// <summary>
        /// Using this to trigger enlightenment until the NPC is introduced
        /// </summary>
        [CommandHandler("dequeue", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Dequeues you from the duel queue")]
        public static void HandlePlayerDequeue(Session session, params string[] parameters)
        {
            MatchManager.DequeueMe(session.Player.Guid);
        }

        /// <summary>
        /// Using this to trigger enlightenment until the NPC is introduced
        /// </summary>
        [CommandHandler("queue", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1,
            "Queues you for a rated PK fight!",
            "[me or us]")]
        public static void HandlePlayerQueue(Session session, params string[] parameters)
        {
            if (parameters.Length == 1)
            {
                if (parameters[0].Equals("me", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.Player != null)
                    {
                        Team team = new Team();
                        PKArenaParticipant me = new PKArenaParticipant(session.Player.Guid);
                        team.Participants.Add(me);
                        team.Rating = me.PKArenaRating1;
                        MatchManager.EnqueueTeam(team);
                    }
                }
                if (parameters[0].Equals("us", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.Player != null)
                    {
                        var fellowshipMembers = session.Player.GetFellowshipTargets();
                        if (fellowshipMembers.Count != 3)
                        {
                            session.Network.EnqueueSend(new GameMessageSystemChat($"The team queue is only available for fellowships of 3. Your fellowship has {fellowshipMembers.Count} member{(fellowshipMembers.Count > 1 ? "s" : "")}.", ChatMessageType.Broadcast));
                            return;
                        }
                        else
                        {
                            session.Network.EnqueueSend(new GameMessageSystemChat($"This feature is not fully implemented yet. Try back later.", ChatMessageType.Broadcast));
                            return;
                        }
                        Team team = new Team();

                        PKArenaParticipant me = new PKArenaParticipant(session.Player.Guid);
                        me.AcceptedQueue = true;
                        team.Participants.Add(me);

                        foreach(var player in fellowshipMembers)
                        {
                            var msg = $"{session.Player.Name} has initiated a PK arena queue for your fellowship.\nDo you wish to accept?";
                            player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(player.Guid, () => HandleQueueFellowship(team)), msg);
                            PKArenaParticipant fellowMember = new PKArenaParticipant(player.Guid);
                            team.Participants.Add(fellowMember);
                        }
                        team.Participants.ForEach(x => team.Rating += x.PKArenaRating3);
                        team.Rating /= 3;
                        teamsWaitingForResponse.Add(Tuple.Create(team, DateTime.Now));
                    }
                }
            }
        }

        private static List<Tuple<Team, DateTime>> teamsWaitingForResponse = new List<Tuple<Team, DateTime>>();

        private static void HandleQueueFellowship(Team team)
        {

            MatchManager.EnqueueTeam(team);
        }

        /// <summary>
        /// Using this to trigger enlightenment until the NPC is introduced
        /// </summary>
        [CommandHandler("enlighten", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Begins the enlightenment process!",
            "/enlighten")]
        public static void HandlePlayerEnlightenment(Session session, params string[] parameters)
        {
            Enlightenment.HandleEnlightenment(session.Player);
        }

        /// <summary>
        /// Creates a mule on your account with the specificed name. Useage: /mule [character name] 
        /// </summary>
        [CommandHandler("mule", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Creates a mule on your account with the specificed name",
            "/mule [character name]")]
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

            var name = string.Join(' ', parameters);
            if (parameters.Length > 0)
            {
                name = name.TrimStart('+').TrimStart(' ').TrimEnd(' ');
                name = Regex.Replace(name, "[^a-zA-Z' ]", "");
            }
            else
            {
                name = $"{session.Player.Name}'s Mule";
            }

            name = name.Substring(0, name.Length > 32 ? 32 : name.Length);

            mule.Name = name;
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

                mule.Location = session.Player.Location;

                mule.Character.CharacterOptions1 = session.Player.Character.CharacterOptions1;
                mule.Character.CharacterOptions2 = session.Player.Character.CharacterOptions2;

                // make sure they aren't viable non-mule characters
                mule.SetProperty(PropertyInt.Level, 5);
                mule.SetProperty(PropertyInt.TotalSkillCredits, 0);
                mule.SetProperty(PropertyInt.AvailableSkillCredits, 0);
                mule.SetProperty(PropertyFloat.GlobalXpMod, 0.0f);

                // give mule higher strength
                mule.Attributes.TryGetValue(PropertyAttribute.Strength, out CreatureAttribute strength);
                strength.StartingValue = (uint)(isRareCreation ? 270 : 250);
                mule.Attributes.Remove(PropertyAttribute.Strength);
                mule.Attributes.TryAdd(PropertyAttribute.Strength, strength);

                // make them a little smaller, to prevent model size issues
                mule.SetProperty(PropertyFloat.DefaultScale, 0.8f);

                var possessedBiotas = new Collection<(Biota biota, ReaderWriterLockSlim rwLock)>();

                DatabaseManager.Shard.AddCharacterInParallel(mule.Biota, mule.BiotaDatabaseLock, possessedBiotas, mule.Character, mule.CharacterDatabaseLock, null);

                PlayerManager.AddOfflinePlayer(mule);
                session.Characters.Add(mule.Character);

                session.LogOffPlayer();
            });

        }
    }
}
