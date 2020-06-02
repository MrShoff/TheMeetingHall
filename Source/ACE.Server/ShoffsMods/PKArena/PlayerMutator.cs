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

namespace ACE.Server.ShoffsMods.PKArena
{
    public static class PlayerMutator
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum CharacterType
        {
            Mule,
            MageDuelist,
            Nightmare
        }

        public static void Mutate(Player target, Player creator, CharacterType type)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            _ = creator ?? throw new ArgumentNullException(nameof(creator));

            SetNewCharacterLocation(target);
            target.Character.CharacterOptions1 = creator.Character.CharacterOptions1;
            target.Character.CharacterOptions2 = creator.Character.CharacterOptions2;

            // make sure they aren't viable non-mutated characters
            target.SetProperty(PropertyFloat.GlobalXpMod, 0.0f);

            // temporarily grant skill credits for template creation. these are removed at the end
            target.SetProperty(PropertyInt.TotalSkillCredits, 1000);
            target.SetProperty(PropertyInt.AvailableSkillCredits, 1000);

            // handle type specific mutations
            switch (type)
            {
                case CharacterType.MageDuelist:
                    SetupDuelistTemplate(target);
                    AddDuelistItems(target);
                    break;
                case CharacterType.Nightmare:
                    SetupNightmareTemplate(target);
                    AddStreaksAndExtraLifeSpells(target);
                    AddNightmareItems(target);
                    break;
                case CharacterType.Mule:
                    SetupMuleTemplate(target);
                    target.SetProperty(PropertyDataId.SoundTable, 0); // turn off that damn mooing                                                                      
                    target.SetProperty(PropertyFloat.DefaultScale, 0.8f); // make them a little smaller, to prevent model size issues
                    break;
            }

            if (type != CharacterType.Mule)
            {
                // make them viable pks
                target.SetProperty(PropertyInt.PlayerKillerStatus, (int)PlayerKillerStatus.PK);
                target.SetProperty(PropertyBool.SpellComponentsRequired, false);
                AddStandardSpells(target);
                SetupDefaultSpellBars(target);
                MirrorCreatorLooks(target, creator);

                // max out skills & attributes
                foreach (var skill in target.Skills.Where(x => x.Value.AdvancementClass >= SkillAdvancementClass.Trained))
                {
                    skill.Value.ExperienceSpent = skill.Value.ExperienceLeft;
                    skill.Value.Ranks = (ushort)Player.CalcSkillRank(skill.Value.AdvancementClass, skill.Value.ExperienceSpent);
                }
                foreach (var vital in target.Vitals)
                {
                    vital.Value.ExperienceSpent = vital.Value.ExperienceLeft;
                    vital.Value.Ranks = (ushort)Player.CalcVitalRank(vital.Value.ExperienceSpent);
                }

                // ban from /cg and add /o
                target.ChannelsActive &= ~Channel.AllBroadcast;
                target.ChannelsActive |= Channel.Olthoi;
                target.ChannelsAllowed &= ~Channel.AllBroadcast;
                target.ChannelsAllowed |= Channel.Olthoi;
            }

            target.SetCharacterOption(CharacterOption.AllowOthersToSeeYourChessRank, true);
            target.SetCharacterOption(CharacterOption.AllowOthersToSeeYourNumberOfDeaths, true);
            target.SetProperty(PropertyInt.AvailableSkillCredits, 0);
            target.Health.Current = target.Health.MaxValue;
            target.Stamina.Current = target.Stamina.MaxValue;
            target.Mana.Current = target.Mana.MaxValue;
        }

        #region Mule
        private static void SetupMuleTemplate(Player target)
        {
            target.SetProperty(PropertyString.Template, "Mule");
            target.SetProperty(PropertyInt.Level, 1);

            // set mage attributes & skills
            SetAttributes(target, 250, 10, 10, 100, 10, 10);
        }
        #endregion

        #region Duelist
        private static void SetupDuelistTemplate(Player target)
        {
            target.SetProperty(PropertyString.Template, "Duelist");
            target.SetProperty(PropertyInt.Level, 300);

            // set mage attributes & skills
            SetAttributes(target, 10, 100, 10, 10, 100, 100);
            target.TrainSkill(Skill.ManaConversion);
            target.TrainSkill(Skill.Run);
            target.TrainSkill(Skill.Jump);

            target.TrainSkill(Skill.WarMagic);
            target.TrainSkill(Skill.LifeMagic);

            target.SpecializeSkill(Skill.WarMagic);
            target.SpecializeSkill(Skill.LifeMagic);
        }
        private static void AddDuelistItems(Player target)
        {
            List<WorldObject> items = new List<WorldObject>();
            // create world objects
            for (int i = 0; i < 7; i++)
            {
                var rarePack = WorldObjectFactory.CreateNewWorldObject(30936);
                items.Add(rarePack);
            }

            var weeping = (Caster)WorldObjectFactory.CreateNewWorldObject(24207);
            ItemMutator.ClearSpells(weeping);
            ItemMutator.ApplyStandardBuffSpells(weeping);
            items.Add(weeping);

            var armorLayeringToolTop = WorldObjectFactory.CreateNewWorldObject(42724);
            var armorLayeringToolBottom = WorldObjectFactory.CreateNewWorldObject(42726);
            var armorTailoringKit = WorldObjectFactory.CreateNewWorldObject(41956);
            var weaponTailoringKit = WorldObjectFactory.CreateNewWorldObject(51445);

            var sparringPants = (Clothing)WorldObjectFactory.CreateNewWorldObject(25983);
            ItemMutator.ApplyStandardBuffSpells(sparringPants);
            ItemMutator.ApplyArmorBuffs(sparringPants);
            var sparringShirt = (Clothing)WorldObjectFactory.CreateNewWorldObject(25984);
            ItemMutator.ApplyArmorBuffs(sparringShirt);

            items.Add(armorLayeringToolTop);
            items.Add(armorLayeringToolBottom);
            items.Add(armorTailoringKit);
            items.Add(weaponTailoringKit);
            items.Add(sparringPants);
            items.Add(sparringShirt);

            foreach (var item in items)
            {
                ItemMutator.ApplyStandardSpecialItemProperties(item, CharacterType.MageDuelist);
                target.TryAddToInventory(item);
            }
        }
        #endregion

        #region Nightmare
        private static void SetupNightmareTemplate(Player target)
        {
            target.SetProperty(PropertyString.Template, "One of His Chosen");
            target.SetProperty(PropertyInt.Level, 500);

            // set attributes & skills
            SetAttributes(target, 100, 100, 100, 10, 100, 100);
            target.TrainSkill(Skill.ManaConversion);
            target.TrainSkill(Skill.Run);
            target.TrainSkill(Skill.Jump);
            target.TrainSkill(Skill.Healing);
            target.TrainSkill(Skill.MagicDefense);
            target.TrainSkill(Skill.Shield);

            target.TrainSkill(Skill.MissileWeapons);
            target.TrainSkill(Skill.TwoHandedCombat);
            target.TrainSkill(Skill.HeavyWeapons);
            target.TrainSkill(Skill.DualWield);
            target.TrainSkill(Skill.DirtyFighting);
            target.TrainSkill(Skill.SneakAttack);
            target.TrainSkill(Skill.Deception);
            target.TrainSkill(Skill.AssessPerson);
            target.TrainSkill(Skill.Recklessness);
            target.TrainSkill(Skill.WarMagic);
            target.TrainSkill(Skill.LifeMagic);
            target.TrainSkill(Skill.ArcaneLore);
            target.TrainSkill(Skill.Fletching);

            target.SpecializeSkill(Skill.MissileWeapons);
            target.SpecializeSkill(Skill.TwoHandedCombat);
            target.SpecializeSkill(Skill.HeavyWeapons);
            target.SpecializeSkill(Skill.DualWield);
            target.SpecializeSkill(Skill.DirtyFighting);
            target.SpecializeSkill(Skill.SneakAttack);
            target.SpecializeSkill(Skill.Deception);
            target.SpecializeSkill(Skill.AssessPerson);
            target.SpecializeSkill(Skill.Recklessness);
            target.SpecializeSkill(Skill.WarMagic);
            target.SpecializeSkill(Skill.LifeMagic);
            target.SpecializeSkill(Skill.ArcaneLore);
            target.SpecializeSkill(Skill.Fletching);
        }

        private static void AddNightmareItems(Player target)
        {
            AddDuelistItems(target);

            List<WorldObject> items = new List<WorldObject>();
            // ================
            //     Usables
            // ================
            var tumerokSaltedMeats = WorldObjectFactory.CreateNewWorldObject(27669);

            var plentifulKit = WorldObjectFactory.CreateNewWorldObject(22449);

            var deadlyPrismaticArrowheads = WorldObjectFactory.CreateNewWorldObject(44072);
            var arrowshafts = WorldObjectFactory.CreateNewWorldObject(9377);
            var quarrelshafts = WorldObjectFactory.CreateNewWorldObject(9378);

            var leftHandTether = WorldObjectFactory.CreateNewWorldObject(45683);
            var leftHandTetherRemover = WorldObjectFactory.CreateNewWorldObject(45684);

            items.Add(tumerokSaltedMeats);
            items.Add(plentifulKit);
            items.Add(deadlyPrismaticArrowheads);
            items.Add(arrowshafts);
            items.Add(quarrelshafts);
            items.Add(leftHandTether);
            items.Add(leftHandTetherRemover);

            // ================
            //    Wieldables
            // ================
            var aegisOfTheGoldenFlame = WorldObjectFactory.CreateNewWorldObject(43141);
            var faranRobeWithHood = WorldObjectFactory.CreateNewWorldObject(5851);
            faranRobeWithHood.ValidLocations |= EquipMask.HandWear;
            faranRobeWithHood.ClothingPriority |= CoverageMask.Hands;
            ItemMutator.ApplyArmorBuffs(faranRobeWithHood);
            ItemMutator.ApplyEpicProts(faranRobeWithHood);

            items.Add(aegisOfTheGoldenFlame);
            items.Add(faranRobeWithHood);

            // add some random weapons
            for(int i = 0; i < 30; i++)
            {
                switch(ThreadSafeRandom.Next(1,6))
                {
                    case 1:
                        var xbow = CreateLootGenMissileWeapon(CombatStyle.Crossbow, DamageType.Fire); // T7: 165 & 19
                        ItemMutator.ApplyStandardWeaponProperties(xbow, ImbuedEffectType.CripplingBlow);
                        items.Add(xbow);
                        break;
                    case 2:
                        var bow = CreateLootGenMissileWeapon(CombatStyle.Bow, DamageType.Fire); // T7: 165 & 19
                        ItemMutator.ApplyStandardWeaponProperties(bow, ImbuedEffectType.ArmorRending);
                        items.Add(bow);
                        break;
                    case 3:
                        var twoHand = CreateLootGenHeavyOrTwohandWeapon(CombatStyle.TwoHanded, Skill.TwoHandedCombat, DamageType.Fire); // T7: 45 at 35%
                        ItemMutator.ApplyStandardWeaponProperties(twoHand, ImbuedEffectType.ArmorRending);
                        items.Add(twoHand);                        
                        break;
                    case 4:
                    case 5:
                    case 6:
                        var ua = CreateLootGenHeavyOrTwohandWeapon(CombatStyle.Unarmed, Skill.HeavyWeapons, DamageType.Fire); // T7: 56 at 44%
                        ItemMutator.ApplyStandardWeaponProperties(ua, ImbuedEffectType.ArmorRending);
                        items.Add(ua);
                        break;
                }
            }

            foreach (var item in items)
            {
                ItemMutator.ApplyStandardSpecialItemProperties(item, CharacterType.Nightmare);
                target.TryAddToInventory(item);

                // organize weapons into packs
                var sidePacks = target.Inventory.Values.Where(x => x.WeenieType == WeenieType.Container).ToList();
                switch (item)
                {
                    case var i when i is MeleeWeapon m:
                        switch (m.WeaponSkill)
                        {
                            case Skill.TwoHandedCombat:
                                if (sidePacks[0] is Container c0)
                                {
                                    target.TryRemoveFromInventory(item.Guid, true);
                                    c0.TryAddToInventory(item);
                                }
                                break;
                            case Skill.HeavyWeapons:
                                if (sidePacks[1] is Container c1)
                                {
                                    target.TryRemoveFromInventory(item.Guid, true);
                                    c1.TryAddToInventory(item);
                                }
                                break;
                        }
                        break;
                    case var i when i is MissileLauncher m:
                        if (sidePacks[2] is Container c2)
                        {
                            target.TryRemoveFromInventory(item.Guid, true);
                            c2.TryAddToInventory(item);
                        }
                        break;
                }
            }
        }
        #endregion

        #region All
        private static MeleeWeapon CreateLootGenHeavyOrTwohandWeapon(CombatStyle combatStyle, Skill meleeSkill, DamageType dmgType, bool isCleaving = false)
        {
            string nameSearch = string.Empty;
            switch(dmgType)
            {
                case DamageType.Fire:
                    nameSearch = "Flaming";
                    break;
            }

            var weapon = (MeleeWeapon)LootGenerationFactory.CreateMeleeWeapon(DatabaseManager.World.GetCachedDeathTreasure(230), true);
            if (weapon == null) return CreateLootGenHeavyOrTwohandWeapon(combatStyle, meleeSkill, dmgType, isCleaving);
            while (!weapon.DefaultCombatStyle.Value.HasFlag(combatStyle) || weapon.IsCleaving != isCleaving || !weapon.Name.Contains(nameSearch))
            {
                weapon.Destroy();
                weapon = (MeleeWeapon)LootGenerationFactory.CreateMeleeWeapon(DatabaseManager.World.GetCachedDeathTreasure(230), true);
                if (weapon == null) return CreateLootGenHeavyOrTwohandWeapon(combatStyle, meleeSkill, dmgType, isCleaving);
            }

            // convert to heavy weapons or two-hand
            if (weapon.WeaponSkill.HasFlag(Skill.LightWeapons))
            {
                weapon.WeaponSkill &= ~Skill.LightWeapons;
            }
            if (weapon.WeaponSkill.HasFlag(Skill.FinesseWeapons))
            {
                weapon.WeaponSkill &= ~Skill.FinesseWeapons;
            }
            if (!weapon.WeaponSkill.HasFlag(Skill.TwoHandedCombat))
            {
                weapon.WeaponSkill |= meleeSkill;
            }

            return weapon;
        }

        private static MissileLauncher CreateLootGenMissileWeapon(CombatStyle combatStyle, DamageType dmgType)
        {
            var weapon = (MissileLauncher)LootGenerationFactory.CreateMissileWeapon(DatabaseManager.World.GetCachedDeathTreasure(230), true);
            if (weapon == null) return CreateLootGenMissileWeapon(combatStyle, dmgType);
            while (!weapon.DefaultCombatStyle.Value.HasFlag(combatStyle))
            {
                weapon.Destroy();
                weapon = (MissileLauncher)LootGenerationFactory.CreateMissileWeapon(DatabaseManager.World.GetCachedDeathTreasure(230), true);
                if (weapon == null) return CreateLootGenMissileWeapon(combatStyle, dmgType);
            }
            weapon.W_DamageType = dmgType;
            return weapon;
        }

        private static void SetNewCharacterLocation(Player character)
        {
            // Dtermine the starting location
            var instantiation = new Position(0xA9B40019, 84, 7.1f, 94, 0, 0, -0.0784591f, 0.996917f); // ultimate fallback.
            var spellFreeRide = DatabaseManager.World.GetCachedSpell(3815); // Free Ride to Holtburg
            if (spellFreeRide != null && spellFreeRide.Name != "")
                instantiation = new Position(spellFreeRide.PositionObjCellId.Value, spellFreeRide.PositionOriginX.Value, spellFreeRide.PositionOriginY.Value, spellFreeRide.PositionOriginZ.Value, spellFreeRide.PositionAnglesX.Value, spellFreeRide.PositionAnglesY.Value, spellFreeRide.PositionAnglesZ.Value, spellFreeRide.PositionAnglesW.Value);

            character.Instantiation = new Position(instantiation);
            character.Sanctuary = new Position(instantiation);
            character.Location = new Position(instantiation);
        }

        private static void MirrorCreatorLooks(Player target, Player creator)
        {
            var heritageGroup = DatManager.PortalDat.CharGen.HeritageGroups[(uint?)creator.HeritageGroup ?? 1];

            target.SetProperty(PropertyInt.HeritageGroup, creator.GetProperty(PropertyInt.HeritageGroup) ?? 1);
            target.SetProperty(PropertyString.HeritageGroup, creator.GetProperty(PropertyString.HeritageGroup) ?? "Duelist");
            target.SetProperty(PropertyInt.Gender, creator.GetProperty(PropertyInt.Gender) ?? 0);
            target.SetProperty(PropertyString.Sex, creator.GetProperty(PropertyString.Sex) ?? "Non-Binary");

            //player.SetProperty(PropertyDataId.Icon, cgh.IconImage); // I don't believe this is used anywhere in the client, but it might be used by a future custom launcher

            // pull character data from the dat file
            var sex = heritageGroup.Genders[creator.Gender ?? 1];

            target.SetProperty(PropertyDataId.MotionTable, sex.MotionTable);
            target.SetProperty(PropertyDataId.SoundTable, sex.SoundTable);
            target.SetProperty(PropertyDataId.PhysicsEffectTable, sex.PhysicsTable);
            target.SetProperty(PropertyDataId.Setup, sex.SetupID);
            target.SetProperty(PropertyDataId.PaletteBase, sex.BasePalette);
            target.SetProperty(PropertyDataId.CombatTable, sex.CombatTable);

            // Check the character scale
            if (sex.Scale != 100u)
                target.SetProperty(PropertyFloat.DefaultScale, (sex.Scale / 100f)); // Scale is stored as a percentage

            // Get the hair first, because we need to know if you're bald, and that's the name of that tune!
            var hairstyle = sex.HairStyleList[Convert.ToInt32(creator.HairStyle)];

            // Olthoi and Gear Knights have a "Body Style" instead of a hair style. These styles have multiple model/texture changes, instead of a single head/hairstyle.
            // Storing this value allows us to send the proper appearance ObjDesc
            if (hairstyle.ObjDesc.AnimPartChanges.Count > 1)
                target.SetProperty(PropertyInt.Hairstyle, (int)creator.HairStyle);

            // Certain races (Undead, Tumeroks, Others?) have multiple body styles available. This is controlled via the "hair style".
            if (hairstyle.AlternateSetup > 0)
                target.SetProperty(PropertyDataId.Setup, hairstyle.AlternateSetup);

            target.SetProperty(PropertyDataId.EyesTexture, creator.GetProperty(PropertyDataId.EyesTexture) ?? 0);
            target.SetProperty(PropertyDataId.DefaultEyesTexture, creator.GetProperty(PropertyDataId.DefaultEyesTexture) ?? 0);
            target.SetProperty(PropertyDataId.NoseTexture, creator.GetProperty(PropertyDataId.NoseTexture) ?? 0);
            target.SetProperty(PropertyDataId.DefaultNoseTexture, creator.GetProperty(PropertyDataId.DefaultNoseTexture) ?? 0);
            target.SetProperty(PropertyDataId.MouthTexture, creator.GetProperty(PropertyDataId.MouthTexture) ?? 0);
            target.SetProperty(PropertyDataId.DefaultMouthTexture, creator.GetProperty(PropertyDataId.DefaultMouthTexture) ?? 0);
            target.Character.HairTexture = creator.Character.HairTexture;
            target.Character.DefaultHairTexture = creator.Character.DefaultHairTexture;

            target.SetProperty(PropertyDataId.HeadObject, creator.GetProperty(PropertyDataId.HeadObject) ?? 0);

            // Skin is stored as PaletteSet (list of Palettes), so we need to read in the set to get the specific palette
            var skinPalSet = DatManager.PortalDat.ReadFromDat<PaletteSet>(sex.SkinPalSet);
            target.SetProperty(PropertyDataId.SkinPalette, creator.GetProperty(PropertyDataId.SkinPalette) ?? 0);
            target.SetProperty(PropertyFloat.Shade, creator.GetProperty(PropertyFloat.Shade) ?? 0);

            // Hair is stored as PaletteSet (list of Palettes), so we need to read in the set to get the specific palette
            target.SetProperty(PropertyDataId.HairPalette, creator.GetProperty(PropertyDataId.HairPalette) ?? 0);

            // Eye Color
            target.SetProperty(PropertyDataId.EyesPalette, creator.GetProperty(PropertyDataId.EyesPalette) ?? 0);
        }

        private static void AddStreaksAndExtraLifeSpells(Player target)
        {
            target.AddKnownSpell((uint)SpellId.AcidStreak8);
            target.AddKnownSpell((uint)SpellId.LightningStreak8);
            target.AddKnownSpell((uint)SpellId.FlameStreak8);
            target.AddKnownSpell((uint)SpellId.FrostStreak8);
            target.AddKnownSpell((uint)SpellId.ShockwaveStreak8);
            target.AddKnownSpell((uint)SpellId.WhirlingBladeStreak8);
            target.AddKnownSpell((uint)SpellId.ForceStreak8);

            target.AddKnownSpell((uint)SpellId.DrainHealth1);
            target.AddKnownSpell((uint)SpellId.DrainHealth7);

            target.AddKnownSpell((uint)SpellId.HealOther8);
            target.AddKnownSpell((uint)SpellId.RevitalizeOther8);

            target.AddKnownSpell((uint)SpellId.StaminaToHealthSelf1);
            target.AddKnownSpell((uint)SpellId.StaminaToHealthSelf2);
            target.AddKnownSpell((uint)SpellId.StaminaToHealthSelf3);
            target.AddKnownSpell((uint)SpellId.StaminaToHealthSelf4);
        }

        private static void AddStandardSpells(Player target)
        {
            // life spells
            target.AddKnownSpell((uint)SpellId.RevitalizeSelf7);
            target.AddKnownSpell((uint)SpellId.RevitalizeSelf8);
            target.AddKnownSpell((uint)SpellId.HealSelf8);
            target.AddKnownSpell((uint)SpellId.StaminaToHealthSelf7);
            target.AddKnownSpell((uint)SpellId.StaminaToManaSelf7);
            target.AddKnownSpell((uint)SpellId.CurseRavenFury);
            target.AddKnownSpell((uint)SpellId.ImperilOther7);

            // war spells and vulns
            target.AddKnownSpell((uint)SpellId.AcidArc8);
            target.AddKnownSpell((uint)SpellId.AcidStream8);
            target.AddKnownSpell((uint)SpellId.AcidVulnerabilityOther8);

            target.AddKnownSpell((uint)SpellId.LightningArc8);
            target.AddKnownSpell((uint)SpellId.LightningBolt8);
            target.AddKnownSpell((uint)SpellId.LightningVulnerabilityOther8);

            target.AddKnownSpell((uint)SpellId.FlameArc8);
            target.AddKnownSpell((uint)SpellId.FlameBolt8);
            target.AddKnownSpell((uint)SpellId.FireVulnerabilityOther8);

            target.AddKnownSpell((uint)SpellId.FrostArc8);
            target.AddKnownSpell((uint)SpellId.FrostBolt8);
            target.AddKnownSpell((uint)SpellId.ColdVulnerabilityOther8);

            target.AddKnownSpell((uint)SpellId.ShockArc8);
            target.AddKnownSpell((uint)SpellId.ShockWave8);
            target.AddKnownSpell((uint)SpellId.BludgeonVulnerabilityOther8);

            target.AddKnownSpell((uint)SpellId.BladeArc8);
            target.AddKnownSpell((uint)SpellId.WhirlingBlade8);
            target.AddKnownSpell((uint)SpellId.BladeVulnerabilityOther8);

            target.AddKnownSpell((uint)SpellId.ForceArc8);
            target.AddKnownSpell((uint)SpellId.ForceBolt8);
            target.AddKnownSpell((uint)SpellId.PiercingVulnerabilityOther8);

            // recalls
            target.AddKnownSpell((uint)SpellId.PortalTieRecall1);
            target.AddKnownSpell((uint)SpellId.PortalTieRecall2);
            target.AddKnownSpell((uint)SpellId.PortalTie1);
            target.AddKnownSpell((uint)SpellId.PortalTie2);
            target.AddKnownSpell((uint)SpellId.PortalRecall);
            target.AddKnownSpell((uint)SpellId.LifestoneRecall1);
            target.AddKnownSpell((uint)SpellId.LifestoneTie1);
        }

        private static void SetupDefaultSpellBars(Player target)
        {
            // ==============
            //   setup tabs
            // ==============
            List<Database.Models.Shard.CharacterPropertiesSpellBar> spellBars = new List<Database.Models.Shard.CharacterPropertiesSpellBar>();
            List<SpellId> s1Spells = new List<SpellId>();
            s1Spells.Add(SpellId.RevitalizeSelf8);
            s1Spells.Add(SpellId.StaminaToHealthSelf7);
            s1Spells.Add(SpellId.StaminaToManaSelf7);
            s1Spells.Add(SpellId.ImperilOther7);
            s1Spells.Add(SpellId.LightningVulnerabilityOther8);
            s1Spells.Add(SpellId.FireVulnerabilityOther8);
            s1Spells.Add(SpellId.ColdVulnerabilityOther8);
            s1Spells.Add(SpellId.AcidVulnerabilityOther8);
            s1Spells.Add(SpellId.BludgeonVulnerabilityOther8);
            s1Spells.Add(SpellId.BladeVulnerabilityOther8);
            s1Spells.Add(SpellId.PiercingVulnerabilityOther8);
            s1Spells.Add(SpellId.LifestoneRecall1);
            s1Spells.Add(SpellId.PortalTieRecall1);
            s1Spells.Add(SpellId.PortalTieRecall2);
            s1Spells.Add(SpellId.PortalRecall);
            s1Spells.Add(SpellId.LifestoneTie1);
            s1Spells.Add(SpellId.PortalTie1);
            s1Spells.Add(SpellId.PortalTie2);
            for (uint i = 0; i < s1Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 1,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s1Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s2Spells = new List<SpellId>();
            s2Spells.Add(SpellId.RevitalizeSelf8);
            s2Spells.Add(SpellId.StaminaToHealthSelf7);
            s2Spells.Add(SpellId.LightningArc8);
            s2Spells.Add(SpellId.LightningBolt8);
            if (target.VerifySpell((uint)SpellId.LightningStreak8))
                s2Spells.Add(SpellId.LightningStreak8);
            else
                s2Spells.Add(SpellId.LifestoneTie1);
            s2Spells.Add(SpellId.HealSelf8);
            s2Spells.Add(SpellId.StaminaToManaSelf7);
            s2Spells.Add(SpellId.LightningVulnerabilityOther8);
            s2Spells.Add(SpellId.CurseRavenFury);
            s2Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s2Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s2Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 2,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s2Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s3Spells = new List<SpellId>();
            s3Spells.Add(SpellId.RevitalizeSelf8);
            s3Spells.Add(SpellId.StaminaToHealthSelf7);
            s3Spells.Add(SpellId.FlameArc8);
            s3Spells.Add(SpellId.FlameBolt8);
            if (target.VerifySpell((uint)SpellId.FlameStreak8))
                s3Spells.Add(SpellId.FlameStreak8);
            else
                s3Spells.Add(SpellId.LifestoneTie1);
            s3Spells.Add(SpellId.HealSelf8);
            s3Spells.Add(SpellId.StaminaToManaSelf7);
            s3Spells.Add(SpellId.FireVulnerabilityOther8);
            s3Spells.Add(SpellId.CurseRavenFury);
            s3Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s3Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s3Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 3,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s3Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s4Spells = new List<SpellId>();
            s4Spells.Add(SpellId.RevitalizeSelf8);
            s4Spells.Add(SpellId.StaminaToHealthSelf7);
            s4Spells.Add(SpellId.FrostArc8);
            s4Spells.Add(SpellId.FrostBolt8);
            if (target.VerifySpell((uint)SpellId.FrostStreak8))
                s4Spells.Add(SpellId.FrostStreak8);
            else
                s4Spells.Add(SpellId.LifestoneTie1);
            s4Spells.Add(SpellId.HealSelf8);
            s4Spells.Add(SpellId.StaminaToManaSelf7);
            s4Spells.Add(SpellId.ColdVulnerabilityOther8);
            s4Spells.Add(SpellId.CurseRavenFury);
            s4Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s4Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s4Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 4,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s4Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s5Spells = new List<SpellId>();
            s5Spells.Add(SpellId.RevitalizeSelf8);
            s5Spells.Add(SpellId.StaminaToHealthSelf7);
            s5Spells.Add(SpellId.ShockArc8);
            s5Spells.Add(SpellId.ShockWave8);
            if (target.VerifySpell((uint)SpellId.ShockwaveStreak8))
                s5Spells.Add(SpellId.ShockwaveStreak8);
            else
                s5Spells.Add(SpellId.LifestoneTie1);
            s5Spells.Add(SpellId.HealSelf8);
            s5Spells.Add(SpellId.StaminaToManaSelf7);
            s5Spells.Add(SpellId.BludgeonVulnerabilityOther8);
            s5Spells.Add(SpellId.CurseRavenFury);
            s5Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s5Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s5Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 5,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s5Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s6Spells = new List<SpellId>();
            s6Spells.Add(SpellId.RevitalizeSelf8);
            s6Spells.Add(SpellId.StaminaToHealthSelf7);
            s6Spells.Add(SpellId.BladeArc8);
            s6Spells.Add(SpellId.WhirlingBlade8);
            if (target.VerifySpell((uint)SpellId.WhirlingBladeStreak8))
                s6Spells.Add(SpellId.WhirlingBladeStreak8);
            else
                s6Spells.Add(SpellId.LifestoneTie1);
            s6Spells.Add(SpellId.HealSelf8);
            s6Spells.Add(SpellId.StaminaToManaSelf7);
            s6Spells.Add(SpellId.BladeVulnerabilityOther8);
            s6Spells.Add(SpellId.CurseRavenFury);
            s6Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s6Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s6Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 6,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s6Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s7Spells = new List<SpellId>();
            s7Spells.Add(SpellId.RevitalizeSelf8);
            s7Spells.Add(SpellId.StaminaToHealthSelf7);
            s7Spells.Add(SpellId.ForceArc8);
            s7Spells.Add(SpellId.ForceBolt8);
            if (target.VerifySpell((uint)SpellId.ForceStreak8))
                s7Spells.Add(SpellId.ForceStreak8);
            else
                s7Spells.Add(SpellId.LifestoneTie1);
            s7Spells.Add(SpellId.HealSelf8);
            s7Spells.Add(SpellId.StaminaToManaSelf7);
            s7Spells.Add(SpellId.PiercingVulnerabilityOther8);
            s7Spells.Add(SpellId.CurseRavenFury);
            s7Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s7Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s7Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 7,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s7Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }
            List<SpellId> s8Spells = new List<SpellId>();
            s8Spells.Add(SpellId.RevitalizeSelf8);
            s8Spells.Add(SpellId.StaminaToHealthSelf7);
            s8Spells.Add(SpellId.AcidArc8);
            s8Spells.Add(SpellId.AcidStream8);
            if (target.VerifySpell((uint)SpellId.AcidStreak8))
                s8Spells.Add(SpellId.AcidStreak8);
            else
                s8Spells.Add(SpellId.LifestoneTie1);
            s8Spells.Add(SpellId.HealSelf8);
            s8Spells.Add(SpellId.StaminaToManaSelf7);
            s8Spells.Add(SpellId.AcidVulnerabilityOther8);
            s8Spells.Add(SpellId.CurseRavenFury);
            s8Spells.Add(SpellId.ImperilOther7);
            if (target.VerifySpell((uint)SpellId.HealOther8))
                s8Spells.Add(SpellId.HealOther8);
            for (uint i = 0; i < s8Spells.Count; i++)
            {
                spellBars.Add(new Database.Models.Shard.CharacterPropertiesSpellBar()
                {
                    Character = target.Character,
                    SpellBarNumber = 8,
                    SpellBarIndex = i, // spell position on bar
                    SpellId = (uint)s8Spells[(int)i],
                    CharacterId = target.Character.Id
                });
            }

            target.Character.CharacterPropertiesSpellBar = spellBars;
        }

        private static void SetAttributes(Player p, uint str, uint end, uint coord, uint quick, uint focus, uint self)
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

            p.Strength.Ranks = (ushort)Player.CalcAttributeRank(p.Strength.ExperienceSpent);
            p.Endurance.Ranks = (ushort)Player.CalcAttributeRank(p.Endurance.ExperienceSpent);
            p.Coordination.Ranks = (ushort)Player.CalcAttributeRank(p.Coordination.ExperienceSpent);
            p.Quickness.Ranks = (ushort)Player.CalcAttributeRank(p.Quickness.ExperienceSpent);
            p.Focus.Ranks = (ushort)Player.CalcAttributeRank(p.Focus.ExperienceSpent);
            p.Self.Ranks = (ushort)Player.CalcAttributeRank(p.Self.ExperienceSpent);
        }
        #endregion
    }
}
