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
        public PlayerBotActionBase? followaction;
        public PlayerBotActionBase? travelaction;


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
                var textmenuroot = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "menuroot", English = player.PlayerName });
                localizationService.m_texts.Add(textmenuroot.persistentID, textmenuroot.GetText(localizationService.CurrentLanguage));

                var textallowedpickups = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "pickupperm", English = player.PlayerName + " toggle pickup permission" });
                localizationService.m_texts.Add(textallowedpickups.persistentID, textallowedpickups.GetText(localizationService.CurrentLanguage));

                var textallowedshare = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "shareperm", English = player.PlayerName + " toggle share permission" });
                localizationService.m_texts.Add(textallowedshare.persistentID, textallowedshare.GetText(localizationService.CurrentLanguage));

                var textstopcommand = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "stopcommand", English = player.PlayerName + " stop what you are doing" });
                localizationService.m_texts.Add(textstopcommand.persistentID, textstopcommand.GetText(localizationService.CurrentLanguage));

                var textattack = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "attack", English = player.PlayerName + " attack my target" });
                localizationService.m_texts.Add(textattack.persistentID, textattack.GetText(localizationService.CurrentLanguage));

                var textpickup = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "pickup", English = player.PlayerName + " pickup resource under my aim" });
                localizationService.m_texts.Add(textpickup.persistentID, textpickup.GetText(localizationService.CurrentLanguage));

                var textsupply = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "supply", English = player.PlayerName + " supply resource (aimed or me)" });
                localizationService.m_texts.Add(textsupply.persistentID, textsupply.GetText(localizationService.CurrentLanguage));

                var textsentry = TextDataBlock.AddBlock(new() { persistentID = 0, internalEnabled = true, SkipLocalization = true, name = player.PlayerName + "sentry", English = player.PlayerName + " toggle sentry mode" });
                localizationService.m_texts.Add(textsentry.persistentID, textsentry.GetText(localizationService.CurrentLanguage));

                var menu = new CommunicationNode(textmenuroot.persistentID, CommunicationNode.ScriptType.None);
                menu.IsLastNode = false;
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

            if (!ZombifiedInitiative.BotTable.ContainsKey(player.PlayerName))
                ZombifiedInitiative.BotTable.Add(player.PlayerName, bot);

            started = true;
        }

        public void OnDestroy()
        {
            if (myself != null)
                ZombifiedInitiative.BotTable.Remove(myself.PlayerName);

            if (mymenu != null)
                mymenu.IsLastNode = true;
        }

        void Update()
        {
            var player = myself;
            var bot = myAI;
            var botMenu = mymenu;
            if (!started || player == null || bot == null || botMenu == null) return;

            var communicationMenu = ZombifiedInitiative._menu;
            if (!menusetup && ZombifiedInitiative.rootmenusetup && communicationMenu != null)
            {
                int menunumber = 0;
                bool flag = false;
                var childNodes = communicationMenu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes;
                // get index of zombified
                for (int num = 0; num < childNodes.Count; num++)
                    if (TextDataBlock.GetBlock(childNodes[num].TextId).English == "Zombified Initiative")
                        menunumber = num;

                // not readding bot if its already somehow in
                var zombifiedMenu = childNodes[menunumber];
                for (int num = 0; num < zombifiedMenu.m_ChildNodes.Count; num++)
                    if (TextDataBlock.GetBlock(zombifiedMenu.m_ChildNodes[num].TextId).English == player.PlayerName)
                    {
                        flag = true;
                        zombifiedMenu.m_ChildNodes[num].IsLastNode = false;
                    }

                if (!flag)
                {
                    zombifiedMenu.m_ChildNodes.Add(botMenu);
                    for (int num = 0; num < zombifiedMenu.m_ChildNodes.Count; num++)
                        if (TextDataBlock.GetBlock(zombifiedMenu.m_ChildNodes[num].TextId).English == player.PlayerName)
                            zombifiedMenu.m_ChildNodes[num].IsLastNode = false;
                }
                menusetup = true;
            }

            if (!SNet.IsMaster) return;
            if (bot.Actions.Count == 0) return;
            actionsToRemove.Clear();
            foreach (var action in bot.Actions)
            {
                // sentry?
                if (action.GetIl2CppType().Name == "PlayerBotActionFollow") followaction = action;
                if (action.GetIl2CppType().Name == "PlayerBotActionTravel") travelaction = action;

                if (!allowedmove && action.GetIl2CppType().Name == "PlayerBotActionFollow") action.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.Queued;
                if (!allowedmove && action.GetIl2CppType().Name == "PlayerBotActionTravel") action.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.Queued;

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
