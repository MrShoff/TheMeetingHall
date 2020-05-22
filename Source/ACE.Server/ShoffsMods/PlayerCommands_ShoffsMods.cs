using ACE.Common;
using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
            //MatchManager.SerializeLocations();
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
                            session.Network.EnqueueSend(new GameMessageSystemChat($"The locations for team duels aren't set up yet, so we're using 1v1 locations- it may be a bit cramped.", ChatMessageType.Broadcast));
                            var team = new Team();
                            fellowshipMembers.ForEach(x => team.Participants.Add(new PKArenaParticipant(x.Guid)));
                            SendMatchConfirmation(team, session.Player);
                            return;
                        }
                    }
                }
            }
        }

        private static void SendMatchConfirmation(Team team, Player requestingPlayer)
        {
            HandleConfirmation(team, false, requestingPlayer);
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

        private static void HandleConfirmation(Team team, bool playerConfirmed = false, Player playerThatConfirmed = null)
        {
            if (!playerConfirmed)
            {
                var msg = $"{playerThatConfirmed.Name} is queueing for team PvP!\nAre you in?";
                foreach (var participant in team.Participants)
                {
                    if (participant.Player != null)
                    {
                        participant.Player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(participant.PlayerGuid, () => HandleConfirmation(team, true, participant.Player)), msg);
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
                    MatchManager.EnqueueTeam(team);
                }
            }
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
        /// Creates a mule on your account with the specificed name. Useage: /create_mule [character name] 
        /// </summary>
        [CommandHandler("create_mule", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
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

            var name = GetCharacterName(session, parameters);
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

                SetNewCharacterLocation(mule, session.Player);

                mule.Character.CharacterOptions1 = session.Player.Character.CharacterOptions1;
                mule.Character.CharacterOptions2 = session.Player.Character.CharacterOptions2;

                // make sure they aren't viable non-mule characters
                mule.SetProperty(PropertyInt.Level, 5);
                mule.SetProperty(PropertyInt.TotalSkillCredits, 0);
                mule.SetProperty(PropertyInt.AvailableSkillCredits, 0);
                mule.SetProperty(PropertyFloat.GlobalXpMod, 0.0f);

                // make them viable mules
                mule.SetProperty(PropertyString.Title, "Mule");
                mule.Strength.StartingValue = (uint)(isRareCreation ? 270 : 250);

                // make them a little smaller, to prevent model size issues
                mule.SetProperty(PropertyFloat.DefaultScale, 0.8f);

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

        /// <summary>
        /// Creates a mule on your account with the specificed name. Useage: /mule [character name] 
        /// </summary>
        [CommandHandler("create_duelist", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Creates a duelist on your account with the specificed name. Only available to enlightened characters.",
            "[character name]")]
        public static void HandleCreateDuelist(Session session, params string[] parameters)
        {
            var guid = GuidManager.NewPlayerGuid();

            var weenie = DatabaseManager.World.GetCachedWeenie(session.Player.WeenieClassId);
            var duelist = new Player(weenie, guid, session.AccountId);

            // set the character name
            var name = GetCharacterName(session, parameters);
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

                SetupCharacterLooks(duelist, session.Player);
                SetNewCharacterLocation(duelist, session.Player);

                duelist.Character.CharacterOptions1 = session.Player.Character.CharacterOptions1;
                duelist.Character.CharacterOptions2 = session.Player.Character.CharacterOptions2;

                // make sure they aren't viable non-duelist characters
                duelist.SetProperty(PropertyFloat.GlobalXpMod, 0.0f);

                // make them viable duelists
                duelist.SetProperty(PropertyInt.Level, 300);
                duelist.SetProperty(PropertyInt.TotalSkillCredits, 100);
                duelist.SetProperty(PropertyInt.AvailableSkillCredits, 100);
                duelist.SetProperty(PropertyInt.TotalExperience, 1234567890);
                duelist.SetProperty(PropertyBool.SpellComponentsRequired, false);

                // add spells
                duelist.AddKnownSpell(4426); // Lightning Arc 8
                duelist.AddKnownSpell(4451); // Lightning Bolt 8
                duelist.AddKnownSpell(4452); // Lightning Streak 8
                duelist.AddKnownSpell(4483); // Lightning Vuln 8
                duelist.AddKnownSpell(4321); // Revit Self 8
                duelist.AddKnownSpell(4311); // Heal Self 8
                duelist.AddKnownSpell(2343); // Stam to Health 7
                duelist.AddKnownSpell(2345); // Stam to Mana 7
                duelist.AddKnownSpell(3818); // Tugak

                // set mage attributes & skills
                duelist = SetAttributes(duelist, 10, 100, 10, 10, 100, 100);
                duelist.TrainSkill(Skill.ManaConversion);
                duelist.TrainSkill(Skill.Run);
                duelist.TrainSkill(Skill.WarMagic);
                duelist.TrainSkill(Skill.LifeMagic);
                duelist.SpecializeSkill(Skill.WarMagic);
                duelist.SpecializeSkill(Skill.LifeMagic);
                foreach (var skill in duelist.Skills.Where(x => x.Value.AdvancementClass >= SkillAdvancementClass.Trained))
                {
                    skill.Value.ExperienceSpent = skill.Value.ExperienceLeft;
                    skill.Value.Ranks = (ushort)Player.CalcSkillRank(skill.Value.AdvancementClass, skill.Value.ExperienceSpent);
                }
                duelist.SetProperty(PropertyInt.AvailableSkillCredits, 0);
                foreach (var vital in duelist.Vitals)
                {
                    vital.Value.ExperienceSpent = vital.Value.ExperienceLeft;
                    vital.Value.Ranks = (ushort)Player.CalcVitalRank(vital.Value.ExperienceSpent);
                }
                duelist.Health.Current = duelist.Health.MaxValue;
                duelist.Stamina.Current = duelist.Stamina.MaxValue;
                duelist.Mana.Current = duelist.Mana.MaxValue;

                // give weeping
                var weeping = (Caster)WorldObjectFactory.CreateNewWorldObject(24207);
                weeping.WieldDifficulty = 0;
                weeping.EncumbranceVal = 0;
                // cantrips
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.CantripWarMagicAptitude4, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.CantripLifeMagicAptitude4, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.AsheronsLesserBenediction, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.GolemHunterHealthHigh, weeping.BiotaDatabaseLock, out _);
                // buff spells
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.QuicknessOther6, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.SprintOther6, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.EnduranceOther8, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.FocusOther8, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.WillpowerOther8, weeping.BiotaDatabaseLock, out _);
                weeping.Biota.GetOrAddKnownSpell((int)SpellId.LightningProtectionOther8, weeping.BiotaDatabaseLock, out _);
                weeping.ManaRate = 0;
                weeping.ItemMaxMana = int.MaxValue;
                weeping.ItemCurMana = int.MaxValue;
                weeping.ItemSpellcraft = 0;
                weeping.Name = "Duelist's Weeping Wand";
                duelist.TryAddToInventory(weeping);

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

        private static void SetupCharacterLooks(Player duelist, Player creator)
        {
            var heritageGroup = DatManager.PortalDat.CharGen.HeritageGroups[(uint?)creator.HeritageGroup ?? 1];

            duelist.SetProperty(PropertyInt.HeritageGroup, creator.GetProperty(PropertyInt.HeritageGroup) ?? 1);
            duelist.SetProperty(PropertyString.HeritageGroup, creator.GetProperty(PropertyString.HeritageGroup) ?? "Duelist");
            duelist.SetProperty(PropertyInt.Gender, creator.GetProperty(PropertyInt.Gender) ?? 0);
            duelist.SetProperty(PropertyString.Sex, creator.GetProperty(PropertyString.Sex) ?? "Non-Binary");

            //player.SetProperty(PropertyDataId.Icon, cgh.IconImage); // I don't believe this is used anywhere in the client, but it might be used by a future custom launcher

            // pull character data from the dat file
            var sex = heritageGroup.Genders[creator.Gender ?? 1];

            duelist.SetProperty(PropertyDataId.MotionTable, sex.MotionTable);
            duelist.SetProperty(PropertyDataId.SoundTable, sex.SoundTable);
            duelist.SetProperty(PropertyDataId.PhysicsEffectTable, sex.PhysicsTable);
            duelist.SetProperty(PropertyDataId.Setup, sex.SetupID);
            duelist.SetProperty(PropertyDataId.PaletteBase, sex.BasePalette);
            duelist.SetProperty(PropertyDataId.CombatTable, sex.CombatTable);

            // Check the character scale
            if (sex.Scale != 100u)
                duelist.SetProperty(PropertyFloat.DefaultScale, (sex.Scale / 100f)); // Scale is stored as a percentage

            // Get the hair first, because we need to know if you're bald, and that's the name of that tune!
            var hairstyle = sex.HairStyleList[Convert.ToInt32(creator.HairStyle)];

            // Olthoi and Gear Knights have a "Body Style" instead of a hair style. These styles have multiple model/texture changes, instead of a single head/hairstyle.
            // Storing this value allows us to send the proper appearance ObjDesc
            if (hairstyle.ObjDesc.AnimPartChanges.Count > 1)
                duelist.SetProperty(PropertyInt.Hairstyle, (int)creator.HairStyle);

            // Certain races (Undead, Tumeroks, Others?) have multiple body styles available. This is controlled via the "hair style".
            if (hairstyle.AlternateSetup > 0)
                duelist.SetProperty(PropertyDataId.Setup, hairstyle.AlternateSetup);            

            duelist.SetProperty(PropertyDataId.EyesTexture, creator.GetProperty(PropertyDataId.EyesTexture) ?? 0);
            duelist.SetProperty(PropertyDataId.DefaultEyesTexture, creator.GetProperty(PropertyDataId.DefaultEyesTexture) ?? 0);
            duelist.SetProperty(PropertyDataId.NoseTexture, creator.GetProperty(PropertyDataId.NoseTexture) ?? 0);
            duelist.SetProperty(PropertyDataId.DefaultNoseTexture, creator.GetProperty(PropertyDataId.DefaultNoseTexture) ?? 0);
            duelist.SetProperty(PropertyDataId.MouthTexture, creator.GetProperty(PropertyDataId.MouthTexture) ?? 0);
            duelist.SetProperty(PropertyDataId.DefaultMouthTexture, creator.GetProperty(PropertyDataId.DefaultMouthTexture) ?? 0);
            duelist.Character.HairTexture = creator.Character.HairTexture;
            duelist.Character.DefaultHairTexture = creator.Character.DefaultHairTexture;

            duelist.SetProperty(PropertyDataId.HeadObject, creator.GetProperty(PropertyDataId.HeadObject) ?? 0);

            // Skin is stored as PaletteSet (list of Palettes), so we need to read in the set to get the specific palette
            var skinPalSet = DatManager.PortalDat.ReadFromDat<PaletteSet>(sex.SkinPalSet);
            duelist.SetProperty(PropertyDataId.SkinPalette, creator.GetProperty(PropertyDataId.SkinPalette) ?? 0);
            duelist.SetProperty(PropertyFloat.Shade, creator.GetProperty(PropertyFloat.Shade) ?? 0);

            // Hair is stored as PaletteSet (list of Palettes), so we need to read in the set to get the specific palette
            duelist.SetProperty(PropertyDataId.HairPalette, creator.GetProperty(PropertyDataId.HairPalette) ?? 0);

            // Eye Color
            duelist.SetProperty(PropertyDataId.EyesPalette, creator.GetProperty(PropertyDataId.EyesPalette) ?? 0);

            duelist.SetProperty(PropertyString.Template, creator.GetProperty(PropertyString.Template));
        }

        private static void SetNewCharacterLocation(Player duelist, Player creator)
        {
            // Dtermine the starting location
            var instantiation = new Position(0xA9B40019, 84, 7.1f, 94, 0, 0, -0.0784591f, 0.996917f); // ultimate fallback.
            var spellFreeRide = DatabaseManager.World.GetCachedSpell(3815); // Free Ride to Holtburg
            if (spellFreeRide != null && spellFreeRide.Name != "")
                instantiation = new Position(spellFreeRide.PositionObjCellId.Value, spellFreeRide.PositionOriginX.Value, spellFreeRide.PositionOriginY.Value, spellFreeRide.PositionOriginZ.Value, spellFreeRide.PositionAnglesX.Value, spellFreeRide.PositionAnglesY.Value, spellFreeRide.PositionAnglesZ.Value, spellFreeRide.PositionAnglesW.Value);

            duelist.Instantiation = new Position(instantiation);

            duelist.Sanctuary = new Position(instantiation);

            duelist.Location = new Position(instantiation);            

            duelist.SetProperty(PropertyInt.PlayerKillerStatus, (int)PlayerKillerStatus.PK);
        }

        private static string GetCharacterName(Session session, params string[] parameters)
        {
            var name = string.Join(' ', parameters);
            if (parameters.Length > 0)
            {
                name = name.TrimStart('+').TrimStart(' ').TrimEnd(' ');
                name = Regex.Replace(name, "[^a-zA-Z' ]", "");
            }
            else
            {
                name = $"{session.Player.Name}'s Duelist";
            }

            name = name.Substring(0, name.Length > 32 ? 32 : name.Length);

            if (PropertyManager.GetBool("taboo_table").Item && DatManager.PortalDat.TabooTable.ContainsBadWord(name.ToLowerInvariant()))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"{name} is not available to use for the duelist character, try another name.", ChatMessageType.Broadcast);
                return string.Empty;
            }

            if (PropertyManager.GetBool("creature_name_check").Item && DatabaseManager.World.IsCreatureNameInWorldDatabase(name))
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"{name} is not available to use for the duelist character, try another name.", ChatMessageType.Broadcast);
                return string.Empty;
            }

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            name = textInfo.ToLower(name);
            name = textInfo.ToTitleCase(name);

            return name;
        }

        /// <summary>
        /// Kills vitae for duelists
        /// </summary>
        [CommandHandler("killvp", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Kills vitae for duelists")]
        public static void HandleKillVp(Session session, params string[] parameters)
        {
            if (session.Player.Level == 300) // only available to duelists
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

        private static Player SetAttributes(Player p, uint str, uint end, uint coord, uint quick, uint focus, uint self)
        {
            p.Strength.StartingValue = str;
            p.Endurance.StartingValue = end;
            p.Coordination.StartingValue = coord;
            p.Quickness.StartingValue = quick;
            p.Focus.StartingValue = focus;
            p.Self.StartingValue = self;

            p.Strength.ExperienceSpent = p.Strength.ExperienceLeft;
            p.Endurance.ExperienceSpent = p.Endurance.ExperienceLeft;
            p.Coordination.ExperienceSpent = p.Coordination.ExperienceLeft;
            p.Quickness.ExperienceSpent = p.Quickness.ExperienceLeft;
            p.Focus.ExperienceSpent = p.Focus.ExperienceLeft;
            p.Self.ExperienceSpent = p.Self.ExperienceLeft;

            //p.Strength.Ranks = (ushort)Player.CalcAttributeRank(p.Strength.ExperienceSpent);
            p.Endurance.Ranks = (ushort)Player.CalcAttributeRank(p.Endurance.ExperienceSpent);
            //p.Coordination.Ranks = (ushort)Player.CalcAttributeRank(p.Coordination.ExperienceSpent);
            p.Quickness.Ranks = (ushort)Player.CalcAttributeRank(p.Quickness.ExperienceSpent);
            p.Focus.Ranks = (ushort)Player.CalcAttributeRank(p.Focus.ExperienceSpent);
            p.Self.Ranks = (ushort)Player.CalcAttributeRank(p.Self.ExperienceSpent);

            return p;
        }
    }
}
