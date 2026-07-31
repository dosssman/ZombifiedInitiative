using GameData;
using Localization;
using Player;
using SNetwork;
using UnityEngine;

namespace Zombified_Initiative
{
    public class ZombieComp : MonoBehaviour
    {
        bool menusetup = false;
        private CommunicationNode? mymenu;

        public bool allowedpickups = true;
        public bool allowedshare = true;
        public bool allowedmove = true;
        public bool started = false;
        private readonly List<PlayerBotActionBase> actionsToRemove = new();
        private PlayerAgent? myself;
        private PlayerAIBot? myAI;

        public PlayerBotActionBase? pickupaction;
        public PlayerBotActionBase? shareaction;


        public void Initialize()
        {
            var player = gameObject.GetComponent<PlayerAgent>();
            if (player == null) return;
            myself = player;

            if (!player.Owner.IsBot)
            {
                Destroy(this);
                return;
            }

            var bot = gameObject.GetComponent<PlayerAIBot>();
            if (bot == null)
            {
                ZombifiedInitiative.L.LogError($"Could not find PlayerAIBot for {player.PlayerName}.");
                return;
            }
            myAI = bot;

            var localizationService = Text.TextLocalizationService.TryCast<GameDataTextLocalizationService>();
            if (localizationService == null)
            {
                ZombifiedInitiative.L.LogError("Could not access the game-data localization service.");
                return;
            }

            ZombifiedInitiative.L.LogInfo($"initializing zombified comp on {player.PlayerName} slot {player.PlayerSlotIndex}..");
            try
            {
                var textmenuroot = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "menuroot", player.PlayerName);
                var textallowedpickups = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "pickupperm", player.PlayerName + " toggle pickup permission");
                var textallowedshare = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "shareperm", player.PlayerName + " toggle share permission");
                var textstopcommand = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "stopcommand", player.PlayerName + " stop what you are doing");
                var textattack = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "attack", player.PlayerName + " attack my target");
                var textpickup = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "pickup", player.PlayerName + " pickup resource under my aim");
                var textsupply = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "supply", player.PlayerName + " supply resource (aimed or me)");
                var textsentry = ZombieController.GetOrCreateTextBlock(
                    localizationService, player.PlayerName + "sentry", player.PlayerName + " toggle sentry mode");

                var menu = new CommunicationNode(textmenuroot.persistentID, CommunicationNode.ScriptType.None)
                {
                    IsLastNode = false
                };
                menu.m_ChildNodes.Add(new CommunicationNode(textallowedpickups.persistentID, CommunicationNode.ScriptType.None));
                menu.m_ChildNodes.Add(new CommunicationNode(textallowedshare.persistentID, CommunicationNode.ScriptType.None));
                menu.m_ChildNodes.Add(new CommunicationNode(textstopcommand.persistentID, CommunicationNode.ScriptType.None));
                menu.m_ChildNodes.Add(new CommunicationNode(textattack.persistentID, CommunicationNode.ScriptType.None));
                menu.m_ChildNodes.Add(new CommunicationNode(textpickup.persistentID, CommunicationNode.ScriptType.None));
                menu.m_ChildNodes.Add(new CommunicationNode(textsupply.persistentID, CommunicationNode.ScriptType.None));
                menu.m_ChildNodes.Add(new CommunicationNode(textsentry.persistentID, CommunicationNode.ScriptType.None));

                for (var index = 0; index < menu.m_ChildNodes.Count; index++)
                    menu.m_ChildNodes[index].DialogID = 314;

                mymenu = menu;
            }
            catch (Exception exception)
            {
                ZombifiedInitiative.L.LogError($"Failed to create the communication menu for {player.PlayerName}: {exception}");
                return;
            }

            ZombifiedInitiative.BotTable[player.PlayerName] = bot;

            started = true;
        }

        public void OnDestroy()
        {
            var replacedByAnotherBot = false;
            if (myself != null && myAI != null &&
                ZombifiedInitiative.BotTable.TryGetValue(myself.PlayerName, out var registeredBot))
            {
                replacedByAnotherBot = registeredBot != myAI;
                if (!replacedByAnotherBot)
                    ZombifiedInitiative.BotTable.Remove(myself.PlayerName);
            }

            if (!replacedByAnotherBot && mymenu != null)
                mymenu.IsLastNode = true;
        }

        void Update()
        {
            var player = myself;
            var bot = myAI;
            var botMenu = mymenu;
            if (!started || player == null || bot == null || botMenu == null) return;

            var zombifiedMenu = ZombieController.ZombifiedMenu;
            if (!menusetup && ZombifiedInitiative.rootmenusetup && zombifiedMenu != null)
            {
                for (int num = 0; num < zombifiedMenu.m_ChildNodes.Count; num++)
                {
                    var existingMenu = zombifiedMenu.m_ChildNodes[num];
                    if (existingMenu.TextId == botMenu.TextId)
                    {
                        existingMenu.IsLastNode = false;
                        mymenu = existingMenu;
                        menusetup = true;
                        break;
                    }
                }

                if (!menusetup)
                {
                    zombifiedMenu.m_ChildNodes.Add(botMenu);
                    botMenu.IsLastNode = false;
                    menusetup = true;
                }
            }

            if (!SNet.IsMaster) return;
            if (bot.Actions.Count == 0) return;
            actionsToRemove.Clear();
            foreach (var action in bot.Actions)
            {
                // pickups?
                if (!allowedpickups && action.GetIl2CppType().Name == "PlayerBotActionCollectItem")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
                    var itemIsDesinfectionPack = descriptor.TargetItem.PublicName == "Disinfection Pack";
                    var itemIsMediPack = descriptor.TargetItem.PublicName == "MediPack";
                    var itemIsAmmoPack = descriptor.TargetItem.PublicName == "Ammo Pack";
                    var itemIsToolRefillPack = descriptor.TargetItem.PublicName == "Tool Refill Pack";

                    var itemIsPack = itemIsToolRefillPack || itemIsAmmoPack || itemIsMediPack || itemIsDesinfectionPack;
                    if (descriptor.Haste < ZombifiedInitiative._manualActionsHaste && itemIsPack)
                    {
                        pickupaction = action;
                        actionsToRemove.Add(action);
                    }
                } // pickups

                // sharing?
                if (!allowedshare && action.GetIl2CppType().Name == "PlayerBotActionShareResourcePack")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
                    if (descriptor.Haste < ZombifiedInitiative._manualActionsHaste)
                    {
                        shareaction = action;
                        actionsToRemove.Add(action);
                    }
                } // share
            } // foreach action

            if (actionsToRemove.Count == 0) return;
            foreach (var action in actionsToRemove)
            {
                bot.Actions.Remove(action);
                ZombifiedInitiative.L.LogInfo($"{player.PlayerName} action {action.GetIl2CppType().Name} was cancelled");
            }
            actionsToRemove.Clear();
        } // update

        public void PreventManualActions()
        {
            var player = myself;
            var bot = myAI;
            if (!started || player == null || bot == null || bot.Actions.Count == 0) return;

            if (pickupaction != null) pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
            if (shareaction != null) shareaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
            pickupaction = null;
            shareaction = null;

            var actionsToRemove = new List<PlayerBotActionBase>();
            var haste = ZombifiedInitiative._manualActionsHaste - 0.01f;

            foreach (var action in bot.Actions)
            {
                if (action.GetIl2CppType().Name == "PlayerBotActionAttack")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionAttack.Descriptor>();
                    if (descriptor.Haste > haste)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }
                }
                if (action.GetIl2CppType().Name == "PlayerBotActionCollectItem")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionCollectItem.Descriptor>();
                    if (descriptor.Haste > haste)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }
                }

                if (action.GetIl2CppType().Name == "PlayerBotActionShareResourcePack")
                {
                    var descriptor = action.DescBase.Cast<PlayerBotActionShareResourcePack.Descriptor>();
                    if (descriptor.Haste > haste)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }
                }
            }

            foreach (var action in actionsToRemove)
            {
                bot.Actions.Remove(action); // Queued stop
                // this.myAI.StopAction(action.DescBase); // Instant stop
                ZombifiedInitiative.L.LogInfo($"{player.PlayerName}'s manual actions were cancelled");
            }
            actionsToRemove.Clear();
        } // preventmanual

        public void ExecuteBotAction(PlayerBotActionBase.Descriptor descriptor, string message)
        {
            var bot = myAI;
            if (bot == null) return;

            bot.StartAction(descriptor);
            ZombifiedInitiative.L.LogInfo(message);
        }
    }
}
