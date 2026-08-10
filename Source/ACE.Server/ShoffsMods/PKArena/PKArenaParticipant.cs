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
using System.Threading;

namespace ACE.Server.WorldObjects
{
    public class PKArenaParticipant
    {
        public Position PriorLocation { get; set; }
        public bool AcceptedQueue { get; set; } = false;
        public bool AcceptedMatchInvite { get; set; } = false;
        public bool IsKilled { get; set; } = false;
        public Player? Player { get => PlayerManager.GetOnlinePlayer(PlayerGuid); }
        public OfflinePlayer OfflinePlayer { get => PlayerManager.GetOfflinePlayer(PlayerGuid); }
        public bool PlayerIsReturned { get; private set; } = false;
        public ObjectGuid PlayerGuid { get; private set; }


        private int PriorRating;

        public PKArenaParticipant(ObjectGuid playerGuid)
        {
            PlayerGuid = playerGuid;
            PriorRating = Player.ChessRank ?? 1400;
        }
        
        public void SetNpkStatus()
        {
            if (Player != null)
            {
                Player.PlayerKillerStatus = PlayerKillerStatus.NPK;
                Player.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(Player, PropertyInt.PlayerKillerStatus, (int)Player.PlayerKillerStatus));
            }
        }

        public void SetPklStatus()
        {
            if (Player != null)
            {
                Player.PlayerKillerStatus = PlayerKillerStatus.PKLite;
                Player.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(Player, PropertyInt.PlayerKillerStatus, (int)Player.PlayerKillerStatus));
            }
        }

        public void HandleDeath(DamageHistoryInfo lastDamager, DamageHistoryInfo topDamager)
        {
            IsKilled = true;

            // return player to previous location
            if (!PlayerIsReturned)
            {
                ReturnPlayer();
            }

            MatchManager.HandleParticipantKilled(this);
        }

        public void HandleDraw()
        {
            if (Player != null)
            {
                Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your PvP match ended in a draw.", ChatMessageType.Broadcast));
                Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your rating is unaffected.", ChatMessageType.Broadcast));
            }
            if (!PlayerIsReturned)
            {
                ReturnPlayer();
            }
        }

        public void HandleWin()
        {
            // broadcast ratings change
            if (Player != null)
            {
                Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Congratulations! You won in the PvP queue!", ChatMessageType.Broadcast));
                if (PriorRating != (Player.ChessRank ?? 1400))
                {
                    Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your new rating is {Player.ChessRank} (+{Player.ChessRank - PriorRating}).", ChatMessageType.Broadcast));
                }
            }

            // return player to previous location
            if (!PlayerIsReturned)
            {
                ReturnPlayer();
            }
        }

        public void HandleDefeat()
        {
            // broadcast ratings change
            if (Player != null)
            {
                Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have been defated in the PvP queue.", ChatMessageType.Broadcast));
                if (PriorRating != (Player.ChessRank ?? 1400))
                {
                    Player.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your new rating is {Player.ChessRank} ({Player.ChessRank - PriorRating}).", ChatMessageType.Broadcast));
                }
            }

            // return player to previous location
            if (!PlayerIsReturned)
            {
                ReturnPlayer();
            }
        }

        public void DispellNegativeEnchantments()
        {
            if (Player == null) return;

            List<Spell> dispellSpells = new List<Spell>()
                    {
                        new Spell(SpellId.DispelAllBadOther8)
                    };

            foreach (var spell in dispellSpells)
            {
                Player.TryCastSpell(spell, Player, null, tryResist: false);
            }
        }

        private void ReturnPlayer()
        {
            IPlayer curPlayer;
            if (Player == null)
            {
                curPlayer = OfflinePlayer;
            }
            else
            {
                curPlayer = Player;
            }

            // reset to normal player
            if (curPlayer.Level > 275)
            {
                curPlayer.SetProperty(PropertyInt.PlayerKillerStatus, (int)PlayerKillerStatus.PK);
            }
            else
            {
                int pkLevel = curPlayer.GetProperty(PropertyInt.PkLevelModifier) ?? 0;
                curPlayer.SetProperty(PropertyInt.PlayerKillerStatus, (int)((PKLevel)pkLevel == PKLevel.PK ? PlayerKillerStatus.PK : PlayerKillerStatus.NPK));
            }
            curPlayer.SetProperty(PropertyBool.Attackable, true);            

            // move them back to where they were
            if (Player != null)
            {
                Player.Session.Network.EnqueueSend(new GameMessageSystemChat("You are being transported back to your previous location.", ChatMessageType.Broadcast));

                var dieChain = new ActionChain();

                // wait for the death animation to finish
                var animLength = DatManager.PortalDat.ReadFromDat<MotionTable>(Player.MotionTableId).GetAnimationLength(MotionCommand.Dead);
                dieChain.AddDelaySeconds(animLength + 1.0f);

                dieChain.AddAction(Player, () =>
                {
                    ThreadSafeTeleportOnDeath(); // enter portal space
                    DispellNegativeEnchantments();

                    if (Player != null)
                    {
                        Player.EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(Player, PropertyInt.PlayerKillerStatus, (int)Player.PlayerKillerStatus));

                        Player.IsBusy = false;
                    }
                });

                dieChain.EnqueueChain();
            }
            else
            {
                if (PriorLocation != null)
                {
                    OfflinePlayer.Biota.SetPosition(PositionType.Location, new Position(PriorLocation), OfflinePlayer.BiotaDatabaseLock);
                }
            }
            PlayerIsReturned = true;
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
