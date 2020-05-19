using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Command.Handlers;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.ShoffsMods.PKArena;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.Server.WorldObjects
{
    public class PKArenaParticipant
    {
        public PlayerKillerStatus PriorPlayerKillerStatus { get; set; }
        public PKLevel PriorPKLevel { get; set; }
        public Position PriorLocation { get; set; }
        public uint PKArenaRating1 { get; set; } = 1400;
        public uint PKArenaRating3 { get; set; } = 1400;
        public bool AcceptedQueue { get; set; } = false;
        public bool AcceptedMatch { get; set; } = false;
        public bool IsKilled { get; set; } = false;
        public Player Player { get => PlayerManager.GetOnlinePlayer(PlayerGuid); }
        public OfflinePlayer OfflinePlayer { get => PlayerManager.GetOfflinePlayer(PlayerGuid); }

        public DateTime LastConfirmationSent { get; set; }


        private ObjectGuid PlayerGuid;

        public PKArenaParticipant(ObjectGuid playerGuid)
        {
            PlayerGuid = playerGuid;
        }

        public void SetPklStatus()
        {

            PriorPlayerKillerStatus = Player.PlayerKillerStatus;
            PriorPKLevel = Player.PkLevel;

            Player.PlayerKillerStatus = PlayerKillerStatus.PKLite;
            Player.PkLevel = PKLevel.NPK;

            Player.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(Player, PropertyInt.PlayerKillerStatus, (int)Player.PlayerKillerStatus));
            CommandHandlerHelper.WriteOutputInfo(Player.Session, $"Your current PK state is now set to: {Player.PlayerKillerStatus.ToString()}", ChatMessageType.Broadcast);
        }

        public void HandleDeath(DamageHistoryInfo lastDamager, DamageHistoryInfo topDamager)
        {
            IsKilled = true;

            MatchManager.HandleParticipantKilled(this);
            ReturnPlayer();
        }

        public void HandleWin()
        {            
            Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Congratulations! You won in the rated PvP arena!", ChatMessageType.Broadcast));

            ReturnPlayer();
            OnMatchConcluded();
        }

        public void HandleDefeat()
        {
            Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been defated in the rated PvP arena.", ChatMessageType.Broadcast));

            OnMatchConcluded();
        }

        private void ReturnPlayer()
        {
            Player.Session.Network.EnqueueSend(new GameMessageSystemChat("You are being transported back to your previous location.", ChatMessageType.Broadcast));

            // wait for the death animation to finish
            var dieChain = new ActionChain();
            var animLength = DatManager.PortalDat.ReadFromDat<MotionTable>(Player.MotionTableId).GetAnimationLength(MotionCommand.Dead);
            dieChain.AddDelaySeconds(animLength + 1.0f);

            dieChain.AddAction(Player, () =>
            {
                ThreadSafeTeleportOnDeath(); // enter portal space

                Player.PlayerKillerStatus = PriorPlayerKillerStatus;
                Player.PkLevel = PriorPKLevel;
                Player.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(Player, PropertyInt.PlayerKillerStatus, (int)Player.PlayerKillerStatus));
                CommandHandlerHelper.WriteOutputInfo(Player.Session, $"Your current PK state is now reset to: {Player.PlayerKillerStatus.ToString()}", ChatMessageType.Broadcast);

                Player.IsBusy = false;
            });

            dieChain.EnqueueChain();
        }

        public void OnMatchConcluded()
        {     
        }


        /// <summary>
        /// Called when the player enters portal space after dying
        /// </summary>
        private void ThreadSafeTeleportOnDeath()
        {
            // teleport to sanctuary or best location
            var newPosition = PriorLocation ?? Player.Location;

            WorldManager.ThreadSafeTeleport(Player, newPosition, new ActionEventDelegate(() =>
            {
                // Stand back up
                Player.SetCombatMode(CombatMode.NonCombat);

                var teleportChain = new ActionChain();
                teleportChain.AddDelaySeconds(3.0f);
                teleportChain.AddAction(Player, () =>
                {
                    // currently happens while in portal space
                    var newHealth = (uint)Math.Round(Player.Health.MaxValue * 0.75f);
                    var newStamina = (uint)Math.Round(Player.Stamina.MaxValue * 0.75f);
                    var newMana = (uint)Math.Round(Player.Mana.MaxValue * 0.75f);

                    var msgHealthUpdate = new GameMessagePrivateUpdateAttribute2ndLevel(Player, Vital.Health, newHealth);
                    var msgStaminaUpdate = new GameMessagePrivateUpdateAttribute2ndLevel(Player, Vital.Stamina, newStamina);
                    var msgManaUpdate = new GameMessagePrivateUpdateAttribute2ndLevel(Player, Vital.Mana, newMana);

                    Player.UpdateVital(Player.Health, newHealth);
                    Player.UpdateVital(Player.Stamina, newStamina);
                    Player.UpdateVital(Player.Mana, newMana);

                    Player.Session.Network.EnqueueSend(msgHealthUpdate, msgStaminaUpdate, msgManaUpdate);

                    // reset damage history for this player
                    Player.DamageHistory.Reset();

                    Player.OnHealthUpdate();
                });

                teleportChain.EnqueueChain();
            }));
        }
    }
}
