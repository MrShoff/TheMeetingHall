using ACE.Common;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;
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
            _ = session.Player.IsInDailyDungeon;
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
