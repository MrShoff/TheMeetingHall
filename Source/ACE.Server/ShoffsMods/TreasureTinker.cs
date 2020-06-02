using ACE.Common;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;

namespace ACE.Server.ShoffsMods
{
    public class TreasureTinker
    {
        // This mod is used to create items that are already pre-tinked
        // and disallow salvage collection
        // To use in your game, just use these functions in the LootGenerationFactory.CreateRandomLootObjects function

        /* Uncommented icon underlay in RecipeManager.AddImbuedEffect
         *
         * 
         * */

        /// <summary>
        /// The maximum possible number of tinks to be applied. Default is 10
        /// </summary>
        public int MaxTinks { get; set; } = 10;

        /// <summary>
        /// Percent chance to apply tinks to an item. Default is 100% (1.0f)
        /// </summary>
        public float ChanceToTink { get; private set; }
        /// <summary>
        /// Chance to apply an imbue to armor. Default is 1% (0.01f)
        /// </summary>
        public float ChanceImbueArmor { get; private set; }
        /// <summary>
        /// Chance to apply an imbue to jewelry. Default is 1% (0.01f)
        /// </summary>
        public float ChanceImbueJewelry { get; private set; }
        /// <summary>
        /// Chance to apply an imbue to weapon. Default is 10% (0.1f)
        /// </summary>
        public float ChanceImbueWeapon { get; private set; }

        private Fraction tinkChance;
        private Fraction imbueArmorChance;
        private Fraction imbueJewelryChance;
        private Fraction imbueWeaponChance;

        public TreasureTinker()
        {
            ChanceToTink = 1.0f;
            ChanceImbueArmor = 0.01f;
            ChanceImbueJewelry = 0.01f;
            ChanceImbueWeapon = 0.1f;
        }

        public void SetChanceToTink(float value)
        {
            ChanceToTink = value;
            tinkChance = new Fraction(value);
        }

        public void SetChanceImbueArmor(float value)
        {
            ChanceImbueArmor = value;
            imbueArmorChance = new Fraction(value);
        }

        public void SetChanceImbueJewelry(float value)
        {
            ChanceImbueJewelry = value;
            imbueJewelryChance = new Fraction(value);
        }

        public void SetChanceImbueWeapon(float value)
        {
            ChanceImbueWeapon = value;
            imbueWeaponChance = new Fraction(value);
        }


        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Generates and applies bags of salvage up to 10 times, based on workmanship.
        /// Supported types: Caster, Missile Weapon, Melee Weapon, Armor with AL.
        /// </summary>
        /// <param name="wo">Loot to be tinkered.</param>
        /// <param name="player">Player who "did the tinkering". Can be null.</param>
        /// <returns>Tinkered loot.</returns>
        public void ApplyTinks(WorldObject wo, Player player = null)
        {
            _ = wo ?? throw new ArgumentNullException(nameof(wo));

            if (ThreadSafeRandom.Next(1, tinkChance.Denominator) > tinkChance.Numerator) return;

            switch (wo.ItemType)
            {
                case ItemType.Armor:
                case ItemType.Clothing when wo.ArmorLevel > 0:
                    ApplyArmorTinks(wo, player);
                    break;
                case ItemType.Caster:
                    ApplyCasterTinks(wo, player);
                    break;
                case ItemType.MissileWeapon:
                    ApplyMissileWeaponTinks(wo, player);
                    break;
                case ItemType.MeleeWeapon:
                    ApplyMeleeWeaponTinks(wo, player);
                    break;
                case ItemType.Jewelry:
                    ApplyJewelryTinks(wo, player);
                    break;
                default:
                    return;
            }
            if (player != null)
            {
                if (wo.GetImbuedEffects() != ImbuedEffectType.Undef)
                    wo.SetProperty(PropertyString.ImbuerName, player.Name);
                if (wo.NumTimesTinkered > 0)
                    wo.SetProperty(PropertyString.TinkerName, player.Name);
            }
        }

        public static void ApplyInscription(WorldObject item, Player p, string inscriptionText)
        {
            if (p == null) return;

            item.Inscription += inscriptionText;
            item.ScribeName = p.Name;
            item.ScribeAccount = p.Account.AccountName;
            item.ScribeIID = p.Guid.Full;
        }

        private void ApplyJewelryTinks(WorldObject jewelry, Player player = null)
        {
            try
            {
                // get num of tinks to apply based on workmanshop
                int? tinksToApply = GetNumTinksToApply(jewelry);
                int tinksLeft = tinksToApply ?? 0;

                // chance to get vitality imbue
                if (ThreadSafeRandom.Next(1, imbueJewelryChance.Denominator) <= imbueJewelryChance.Numerator && tinksLeft > 0)
                {
                    WorldObject hematiteSalvage = GeneratePhantomSalvage(MaterialType.Hematite);
                    RecipeManager.HandleRecipe(player, hematiteSalvage, jewelry, RecipeManager.GetRecipe(player, hematiteSalvage, jewelry), 1.0f);
                    ApplyInscription(jewelry, player, $"IMBUED with {hematiteSalvage.Name}\n(this doesn't seem to work atm)");
                    tinksLeft--;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ApplyArmorTinks(WorldObject armor, Player player = null)
        {
            try
            {
                if (!armor.IsEnchantable) return; 

                // get num of tinks to apply based on workmanshop
                int? tinksToApply = GetNumTinksToApply(armor);
                int tinksLeft = tinksToApply ?? 0;

                // chance to get magic d, melee d, or missile d imbue   
                if (ThreadSafeRandom.Next(1, imbueArmorChance.Denominator) <= imbueArmorChance.Numerator && tinksLeft > 0)
                {
                    switch (ThreadSafeRandom.Next(1, 3))
                    {
                        case 1: // magic d imbue
                            WorldObject zirconSalvage = GeneratePhantomSalvage(MaterialType.Zircon);
                            RecipeManager.Tinkering_ModifyItem(player, zirconSalvage, armor);
                            ApplyInscription(armor, player, $"IMBUED with {zirconSalvage.Name}\n");
                            break;
                        case 2: // melee d imbue
                            WorldObject peridotSalvage = GeneratePhantomSalvage(MaterialType.Peridot);
                            RecipeManager.Tinkering_ModifyItem(player, peridotSalvage, armor);
                            ApplyInscription(armor, player, $"IMBUED with {peridotSalvage.Name}\n");
                            break;
                        case 3: // missile d imbue
                            WorldObject yellowTopazSalvage = GeneratePhantomSalvage(MaterialType.YellowTopaz);
                            RecipeManager.Tinkering_ModifyItem(player, yellowTopazSalvage, armor);
                            ApplyInscription(armor, player, $"IMBUED with {yellowTopazSalvage.Name}\n");
                            break;
                    }
                }
                tinksLeft--; // if not imbued, leave one tink spot for an imbue

                if (tinksLeft > 0)
                {
                    // add steel with remaining tinks
                    WorldObject steelSalvage = GeneratePhantomSalvage(MaterialType.Steel);
                    ApplyInscription(armor, player, $"Tinked {tinksLeft}x with {steelSalvage.Name}");
                    while (tinksLeft > 0)
                    {
                        RecipeManager.Tinkering_ModifyItem(player, steelSalvage, armor);
                        tinksLeft--;
                    }
                }
                
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ApplyCasterTinks(WorldObject caster, Player player = null)
        {
            try
            {
                // get num of tinks to apply based on workmanshop
                int? tinksToApply = GetNumTinksToApply(caster);
                int tinksLeft = tinksToApply ?? 0;

                // 10% chance to imbue
                if (ThreadSafeRandom.Next(1, imbueWeaponChance.Denominator) <= imbueWeaponChance.Numerator && tinksLeft > 0)
                {
                    if (caster.W_DamageType == DamageType.Nether)
                    {
                        switch (ThreadSafeRandom.Next(1, 2))
                        {
                            case 1: // CB imbue
                                WorldObject fireOpalSalvage = GeneratePhantomSalvage(MaterialType.FireOpal);
                                RecipeManager.Tinkering_ModifyItem(player, fireOpalSalvage, caster);
                                break;
                            case 2: // CS imbue
                                WorldObject blackOpalSalvage = GeneratePhantomSalvage(MaterialType.BlackOpal);
                                RecipeManager.Tinkering_ModifyItem(player, blackOpalSalvage, caster);
                                break;
                        }
                    }
                    else
                    {
                        switch (ThreadSafeRandom.Next(1, 3))
                        {
                            case 1: // rend imbue
                                var rendSalvageMaterial = GetMaterialForRend(caster);
                                WorldObject rendSalvage = GeneratePhantomSalvage(rendSalvageMaterial);
                                RecipeManager.Tinkering_ModifyItem(player, rendSalvage, caster);
                                break;
                            case 2: // CB imbue
                                WorldObject fireOpalSalvage = GeneratePhantomSalvage(MaterialType.FireOpal);
                                RecipeManager.Tinkering_ModifyItem(player, fireOpalSalvage, caster);
                                break;
                            case 3: // CS imbue
                                WorldObject blackOpalSalvage = GeneratePhantomSalvage(MaterialType.BlackOpal);
                                RecipeManager.Tinkering_ModifyItem(player, blackOpalSalvage, caster);
                                break;
                        }
                    }
                    tinksLeft--;
                }

                // add brass or green garnet with the remaining tinks
                if (tinksLeft > 0)
                {
                    WorldObject remainingTinksSalvage = null;
                    switch (ThreadSafeRandom.Next(1, 2))
                    {
                        case 1:
                            remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.Brass);
                            break;
                        case 2:
                            remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.GreenGarnet);
                            break;
                    }
                    ApplyInscription(caster, player, $"Tinked {tinksLeft}x with {remainingTinksSalvage.Name}");
                    while (tinksLeft > 0)
                    {
                        RecipeManager.Tinkering_ModifyItem(player, remainingTinksSalvage, caster);
                        tinksLeft--;
                    }                    
                }
                
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ApplyMissileWeaponTinks(WorldObject missileWeapon, Player player = null)
        {
            try
            {
                // get num of tinks to apply based on workmanshop
                int? tinksToApply = GetNumTinksToApply(missileWeapon);
                int tinksLeft = tinksToApply ?? 0;

                // 10% chance to imbue
                if (ThreadSafeRandom.Next(1, imbueWeaponChance.Denominator) <= imbueWeaponChance.Numerator && tinksLeft > 0)
                {
                    switch (ThreadSafeRandom.Next(1, 4))
                    {
                        case 1: // rend imbue
                            var rendSalvageMaterial = GetMaterialForRend(missileWeapon);
                            WorldObject rendSalvage = GeneratePhantomSalvage(rendSalvageMaterial);
                            RecipeManager.Tinkering_ModifyItem(player, rendSalvage, missileWeapon);
                            break;
                        case 2: // AR imbue
                            WorldObject sunstoneSalvage = GeneratePhantomSalvage(MaterialType.Sunstone);
                            RecipeManager.Tinkering_ModifyItem(player, sunstoneSalvage, missileWeapon);
                            break;
                        case 3: // CB imbue
                            WorldObject fireOpalSalvage = GeneratePhantomSalvage(MaterialType.FireOpal);
                            RecipeManager.Tinkering_ModifyItem(player, fireOpalSalvage, missileWeapon);
                            break;
                        case 4: // CS imbue
                            WorldObject blackOpalSalvage = GeneratePhantomSalvage(MaterialType.BlackOpal);
                            RecipeManager.Tinkering_ModifyItem(player, blackOpalSalvage, missileWeapon);
                            break;
                    }
                    tinksLeft--;
                }

                // add brass or mahogany with the remaining tinks
                WorldObject remainingTinksSalvage = null;
                switch (ThreadSafeRandom.Next(1, 2))
                {
                    case 1:
                        remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.Mahogany);
                        break;
                    case 2:
                        remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.Brass);
                        break;
                }
                ApplyInscription(missileWeapon, player, $"Tinked {tinksLeft}x with {remainingTinksSalvage.Name}");
                while (tinksLeft > 0)
                {
                    RecipeManager.Tinkering_ModifyItem(player, remainingTinksSalvage, missileWeapon);
                    tinksLeft--;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ApplyMeleeWeaponTinks(WorldObject meleeWeapon, Player player = null)
        {
            try
            {
                // get num of tinks to apply based on workmanshop
                int? tinksToApply = GetNumTinksToApply(meleeWeapon);
                int tinksLeft = tinksToApply ?? 0;

                // 10% chance to imbue
                if (ThreadSafeRandom.Next(1, imbueWeaponChance.Denominator) <= imbueWeaponChance.Numerator && tinksLeft > 0)
                {
                    switch (ThreadSafeRandom.Next(1, 4))
                    {
                        case 1: // rend imbue
                            var rendSalvageMaterial = GetMaterialForRend(meleeWeapon);
                            WorldObject rendSalvage = GeneratePhantomSalvage(rendSalvageMaterial);
                            RecipeManager.Tinkering_ModifyItem(player, rendSalvage, meleeWeapon);
                            break;
                        case 2: // AR imbue
                            WorldObject sunstoneSalvage = GeneratePhantomSalvage(MaterialType.Sunstone);
                            RecipeManager.Tinkering_ModifyItem(player, sunstoneSalvage, meleeWeapon);
                            break;
                        case 3: // CB imbue
                            WorldObject fireOpalSalvage = GeneratePhantomSalvage(MaterialType.FireOpal);
                            RecipeManager.Tinkering_ModifyItem(player, fireOpalSalvage, meleeWeapon);
                            break;
                        case 4: // CS imbue
                            WorldObject blackOpalSalvage = GeneratePhantomSalvage(MaterialType.BlackOpal);
                            RecipeManager.Tinkering_ModifyItem(player, blackOpalSalvage, meleeWeapon);
                            break;
                    }
                    tinksLeft--;
                }

                // add brass, velvet, or granite&iron with the remaining tinks
                // TODO: set the ideal granite:iron ratio
                WorldObject remainingTinksSalvage = null;
                WorldObject remainingTinksSalvage2 = null;
                switch (ThreadSafeRandom.Next(1, 3))
                {
                    case 1:
                        remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.Brass);
                        break;
                    case 2:
                        remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.Velvet);
                        break;
                    case 3:
                        remainingTinksSalvage = GeneratePhantomSalvage(MaterialType.Granite);
                        remainingTinksSalvage2 = GeneratePhantomSalvage(MaterialType.Iron);
                        break;
                }
                if (remainingTinksSalvage2 != null)
                {
                    ApplyInscription(meleeWeapon, player, $"Tinked {Math.Round(tinksLeft / 2.0, MidpointRounding.AwayFromZero)}x with {remainingTinksSalvage.Name}");
                    ApplyInscription(meleeWeapon, player, $" and {tinksLeft/2}x with {remainingTinksSalvage2.Name}");
                }
                else
                {
                    ApplyInscription(meleeWeapon, player, $"Tinked {tinksLeft}x with {remainingTinksSalvage.Name}");
                }
                while (tinksLeft > 0)
                {
                    if (tinksLeft % 2 == 0 && remainingTinksSalvage2 != null)
                    {
                        RecipeManager.Tinkering_ModifyItem(player, remainingTinksSalvage2, meleeWeapon);
                    }
                    else
                    {
                        RecipeManager.Tinkering_ModifyItem(player, remainingTinksSalvage, meleeWeapon);
                    }
                    tinksLeft--;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private static MaterialType GetMaterialForRend(WorldObject wo)
        {
            MaterialType[] allRendSalvage = new MaterialType[] { MaterialType.Emerald, MaterialType.WhiteSapphire, MaterialType.Aquamarine, MaterialType.Jet, MaterialType.RedGarnet, MaterialType.BlackGarnet, MaterialType.ImperialTopaz };
            switch (wo.W_DamageType)
            {
                case DamageType.Acid:
                    return MaterialType.Emerald;
                case DamageType.Bludgeon:
                    return MaterialType.WhiteSapphire;
                case DamageType.Cold:
                    return MaterialType.Aquamarine;
                case DamageType.Electric:
                    return MaterialType.Jet;
                case DamageType.Fire:
                    return MaterialType.RedGarnet;
                case DamageType.Pierce:
                    return MaterialType.BlackGarnet;
                case DamageType.Slash:
                    return MaterialType.ImperialTopaz;
                default:
                    return allRendSalvage[ThreadSafeRandom.Next(0, 6)];
            }
        }

        public static WorldObject GeneratePhantomSalvage(MaterialType materialType)
        {
            var wcid = (uint)Player.MaterialSalvage[(int)materialType];
            return WorldObjectFactory.CreateNewWorldObject(wcid);

        }

        private int? GetNumTinksToApply(WorldObject wo)
        {
            int tinksToApply;
            if (wo.ItemWorkmanship != null)
            {
                tinksToApply = 15 - wo.ItemWorkmanship.Value;
                tinksToApply = tinksToApply > MaxTinks ? MaxTinks : tinksToApply;
                tinksToApply -= wo.NumTimesTinkered;
            }
            else
            {
                return null;
            }

            return tinksToApply;
        }

        private class Fraction
        {
            public int Numerator { get; private set; }
            public int Denominator { get; private set; }

            public Fraction(int numerator, int denominator)
            {
                Numerator = numerator;
                Denominator = denominator;
            }

            public Fraction(float value)
            {
                Denominator = 1;
                if (value % 1.0f == 0)
                {
                    Numerator = (int)value;
                }
                else
                {
                    Numerator = (int)MathF.Round(value * 1000000.0f, 0);
                    Denominator = (int)MathF.Round(Denominator * 1000000.0f, 0);
                }
                Reduce();
                if (Denominator < 0)
                {
                    Numerator *= -1;
                    Denominator *= -1;
                }
            }

            private static int GetGCD(int a, int b)
            {
                while (a != 0 && b != 0)
                {
                    if (a > b)
                        a %= b;
                    else
                        b %= a;
                }

                return a == 0 ? b : a;
            }

            private void Reduce()
            {
                var gcd = GetGCD(Numerator, Denominator);
                Numerator = Numerator / gcd;
                Denominator = Denominator / gcd;
            }

            private static int GetLCM(int a, int b)
            {
                var largerValue = a > b ? a : b;
                var smallerValue = a < b ? a : b;
                for (int i = largerValue; i <= a * b; i += largerValue)
                {
                    if (i % smallerValue == 0)
                        return i;
                }
                return 1;
            }
        }
    }
}
