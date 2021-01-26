using ACE.Common;
using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
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

using static ACE.Server.ShoffsMods.PKArena.PlayerMutator;

namespace ACE.Server.Entity
{
    public class ItemMutator
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public static WeenieError VerifyUseRequirements(Player player,  WorldObject target)
        {
            log.Info($"1");
            if (player.FindObject(target.Guid.Full, Player.SearchLocations.MyInventory) == null)
                return WeenieError.YouDoNotPassCraftingRequirements;

            // verify not retained item
            if (target.Retained)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("You must use Sandstone Salvage to remove the retained property before mutating.", ChatMessageType.Craft));
                return WeenieError.YouDoNotPassCraftingRequirements;
            }

            // verify its a weapon or armor
            if (!target.ValidLocations.HasValue || target.ValidLocations.Value == EquipMask.None)
            {
                return WeenieError.YouDoNotPassCraftingRequirements;
            }

            return WeenieError.None;
        }

        public static void DoMutation(Player p, WorldObject target)
        {
            if (VerifyUseRequirements(p, target) != WeenieError.None) return;

            var charType = p.Level == 300 ? CharacterType.MageDuelist : CharacterType.Nightmare;

            ClearSpells(target);
            ApplyStandardSpecialItemProperties(target, charType);

            if (target is MeleeWeapon || target is MissileLauncher)
            {
                ApplyStandardWeaponProperties(target, 0);
                p.Session.Network.EnqueueSend(new GameMessageSystemChat("You mutate your weapon.", ChatMessageType.Broadcast)); 
            }
            else if (target is Caster)
            {
                p.Session.Network.EnqueueSend(new GameMessageSystemChat("You mutate your weapon.", ChatMessageType.Broadcast));
            }
            else
            {
                ApplyArmorBuffs(target);
                if (charType == CharacterType.Nightmare)
                {
                    ApplyEpicProts(target);
                }
                p.Session.Network.EnqueueSend(new GameMessageSystemChat("You mutate your armor.", ChatMessageType.Broadcast));
            }

            p.TryConsumeFromInventoryWithNetworking(target, 1);
            p.TryCreateInInventoryWithNetworking(target);
        }

        public static void ApplyStandardWeaponProperties(WorldObject item, ImbuedEffectType imbuedEffectType)
        {
            int damamgeOffMax = 3;

            if (item is MeleeWeapon)
            {
                int granite = 0;
                int iron = 0;
                switch (item.W_WeaponType)
                {
                    case WeaponType.Axe: // T7: 71 at 90%
                        item.Damage = 71;
                        item.DamageVariance = 0.9;
                        iron = 1;
                        granite = 8;
                        break;
                    case WeaponType.Dagger: // T7: 68 at 47% (sword and dagger are exactly the same)
                    case WeaponType.Sword:
                        item.Damage = item.W_AttackType.HasFlag(AttackType.MultiStrike) ? 36 : 68;
                        item.DamageVariance = item.W_AttackType.HasFlag(AttackType.MultiStrike) ? 0.4 : 0.47;
                        iron = 4;
                        granite = 5;
                        break;
                    case WeaponType.Mace: // T7: 66 at 30%
                        item.Damage = 66;
                        item.DamageVariance = 0.3;
                        iron = 6;
                        granite = 3;
                        break;
                    case WeaponType.Spear: // T7: 69 at 59%
                        item.Damage = 69;
                        item.DamageVariance = 0.59;
                        iron = 3;
                        granite = 6;
                        break;
                    case WeaponType.Staff: // T7: 66 at 38%
                        item.Damage = 66;
                        item.DamageVariance = 0.38;
                        iron = 5;
                        granite = 4;
                        break;
                    case WeaponType.TwoHanded: // T7: 45 at 35%
                        item.Damage = 45;
                        item.DamageVariance = 0.35;
                        iron = 6;
                        granite = 3;
                        break;
                    case WeaponType.Unarmed: // T7 Max: 56 at 44%
                        item.Damage = 56;
                        item.DamageVariance = 0.44;
                        iron = 5;
                        granite = 4;
                        break;
                }
                for(int i = 0; i < granite; i++)
                {
                    var salvage = TreasureTinker.GeneratePhantomSalvage(MaterialType.Granite);
                    RecipeManager.Tinkering_ModifyItem(null, salvage, item);
                }
                for (int i = 0; i < (iron - damamgeOffMax); i++)
                {
                    var salvage = TreasureTinker.GeneratePhantomSalvage(MaterialType.Iron);
                    RecipeManager.Tinkering_ModifyItem(null, salvage, item);
                }

                item.WeaponOffense = 1.1;
            }
            
            if (item is MissileLauncher)
            {
                switch (item.W_WeaponType)
                {
                    case WeaponType.Crossbow: // T7 Max: 165, +19
                        item.DamageMod = 3.01; // Simulate 10x on 161% +18
                        item.ElementalDamageBonus = 19 - damamgeOffMax;
                        break;
                    case WeaponType.Bow: // T7 Max: 140, +19
                        item.DamageMod = 2.76; 
                        item.ElementalDamageBonus = 19 - damamgeOffMax;
                        break;
                    case WeaponType.Thrown: // T7 Max: 160, +19
                        item.DamageMod = 2.96; // Simulate 10x on 156% +18
                        item.ElementalDamageBonus = 19 - damamgeOffMax;
                        break;
                }
                RecipeManager.AddImbuedEffect(null, item, ImbuedEffectType.IgnoreSomeMagicProjectileDamage);
                item.SetProperty(PropertyFloat.AbsorbMagicDamage, 0.25f);
                item.Name = $"Dark {item.Name}";
            }

            if (imbuedEffectType > 0)
            {
                RecipeManager.AddImbuedEffect(null, item, imbuedEffectType);
            }

            
            item.WeaponDefense = 1.01;

            item.NumTimesTinkered = 10;
            item.SetProperty(PropertyString.ImbuerName, "The Resurrection");
            item.SetProperty(PropertyString.TinkerName, "The Resurrection");

            // add spells
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPBLOODTHIRST3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.BloodDrinkerOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.SwiftKillerOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.DefenderOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.HeartSeekerOther8, item.BiotaDatabaseLock, out _);
        }

        public static void ApplyStandardSpecialItemProperties(WorldObject item, CharacterType characterType)
        {
            if (item.ClothingPriority.HasValue)
            {
                if (item.ClothingPriority.Value.HasFlag(CoverageMask.OuterwearAbdomen) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.OuterwearUpperLegs) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.OuterwearLowerLegs) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.OuterwearChest) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.OuterwearUpperArms) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.OuterwearLowerArms) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.Head) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.Hands) ||
                    item.ClothingPriority.Value.HasFlag(CoverageMask.Feet))
                {
                    item.ArmorLevel = 450;
                    item.ArmorModVsAcid = 450;
                    item.ArmorModVsBludgeon = 450;
                    item.ArmorModVsCold = 450;
                    item.ArmorModVsAcid = 450;
                    item.ArmorModVsElectric = 450;
                    item.ArmorModVsFire = 450;
                    item.ArmorModVsPierce = 450;
                    item.ArmorModVsSlash = 450;

                    item.NumTimesTinkered = 10;
                    item.SetProperty(PropertyString.ImbuerName, "The Resurrection");
                    item.SetProperty(PropertyString.TinkerName, "The Resurrection");
                }
            }

            

            if (!item.ValidLocations.HasValue || item.ValidLocations == EquipMask.None)
            {
                // items that you don't equip
                item.Attuned = AttunedStatus.Attuned;
                item.UnlimitedUse = true;
            }
            else
            {
                // items that you do equip
                item.ManaRate = 0;
                item.ItemMaxMana = int.MaxValue;
                item.ItemCurMana = int.MaxValue;
                item.ItemSpellcraft = 0;
                item.Attuned = AttunedStatus.Normal;
                item.WieldRequirements = WieldRequirement.Level;
                item.WieldDifficulty = characterType == CharacterType.Nightmare ? 500 : 300;
            }

            item.Name = (characterType == CharacterType.MageDuelist ? "Duelist's " : "Mutant's ") + item.Name;
            item.Bonded = BondedStatus.Bonded;
            item.StackSize = 1;
            item.EncumbranceVal = 0;
            item.StackUnitEncumbrance = 0;
            item.Value = 0;
            item.Workmanship = null;
            item.IsSellable = false;
        }

        public static void ClearSpells(WorldObject item)
        {
            item.Biota.ClearSpells(item.BiotaDatabaseLock);
        }

        public static void ApplyStandardBuffSpells(WorldObject item)
        {
            // ==================
            //    Cantrips
            // ==================
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPSTRENGTH3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPENDURANCE3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPCOORDINATION3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPQUICKNESS3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPFOCUS3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPWILLPOWER3, item.BiotaDatabaseLock, out _);

            // ==================
            //    Health
            // ==================
            item.Biota.GetOrAddKnownSpell((int)SpellId.AsheronsLesserBenediction, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.GolemHunterHealthHigh, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.WarriorsVitality, item.BiotaDatabaseLock, out _);

            // ==================
            //    Buffs
            // ==================
            // attributes
            item.Biota.GetOrAddKnownSpell((int)SpellId.StrengthOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.EnduranceOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CoordinationOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.QuicknessOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.FocusOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.WillpowerOther8, item.BiotaDatabaseLock, out _);

            // masteries
            item.Biota.GetOrAddKnownSpell((int)SpellId.WarMagicMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.LifeMagicMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.TwoHandedMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.MissileWeaponsMasteryOther8, item.BiotaDatabaseLock, out _); 
            item.Biota.GetOrAddKnownSpell((int)SpellId.HeavyWeaponsMasteryOther8, item.BiotaDatabaseLock, out _); 
            item.Biota.GetOrAddKnownSpell((int)SpellId.DualWieldMasteryOther8, item.BiotaDatabaseLock, out _);

            item.Biota.GetOrAddKnownSpell((int)SpellId.DeceptionMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.DirtyFightingMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.PersonAttunementOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.SneakAttackMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.RecklessnessMasteryOther8, item.BiotaDatabaseLock, out _);

            item.Biota.GetOrAddKnownSpell((int)SpellId.HealingMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.ManaMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.SprintOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.JumpingMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.ShieldMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.MagicResistanceOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.FletchingMasteryOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.ArcaneEnlightenmentOther8, item.BiotaDatabaseLock, out _);

            // prots
            item.Biota.GetOrAddKnownSpell((int)SpellId.ArmorOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.AcidProtectionOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.LightningProtectionOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.FireProtectionOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.ColdProtectionOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.BludgeonProtectionOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.BladeProtectionOther8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.PiercingProtectionOther8, item.BiotaDatabaseLock, out _);
        }

        public static void ApplyEpicProts(WorldObject item)
        {
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPARMOR3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPACIDWARD3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPSTORMWARD3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPFLAMEWARD3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPFROSTWARD3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPBLUDGEONINGWARD3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPSLASHINGWARD3, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.CANTRIPPIERCINGWARD3, item.BiotaDatabaseLock, out _);
        }

        public static void ApplyArmorBuffs(WorldObject item)
        {
            item.Biota.GetOrAddKnownSpell((int)SpellId.Impenetrability8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.AcidBane8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.LightningBane8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.FlameBane8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.FrostBane8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.BludgeonBane8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.BladeBane8, item.BiotaDatabaseLock, out _);
            item.Biota.GetOrAddKnownSpell((int)SpellId.PiercingBane8, item.BiotaDatabaseLock, out _);
        }

    }
}
