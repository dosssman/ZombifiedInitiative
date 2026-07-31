using System.Collections;
using Agents;
using BepInEx.Configuration;
using Enemies;
using GameData;
using Gear;
using GTFO.API;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LevelGeneration;
using Localization;
using Player;
using SNetwork;
using UnityEngine;

namespace Zombified_Initiative
{
    public class ZombieController : MonoBehaviour
    {

        public static int _highlightedMenuButtonIndex = 0;
        public static float _manualActionsPriority = 5f;
        public static float _manualActionsHaste = 1f;
        public static bool _preventAutoPickups = true;
        public static bool _preventAutoUses = true;
        public static bool _preventManual = false;
        public static bool _debug = true;
        public static bool _menuadded = false;

        private static InputBinding _toggleDebug = null!;
        private static InputBinding _toggleAllPickups = null!;
        private static InputBinding _toggleAllUses = null!;
        private static InputBinding _selectDauda = null!;
        private static InputBinding _selectHackett = null!;
        private static InputBinding _selectBishop = null!;
        private static InputBinding _selectWoods = null!;
        private static InputBinding _attackCommand = null!;
        private static InputBinding _pickupCommand = null!;
        private static InputBinding _shareCommand = null!;

        public static void BindConfig(ConfigFile config)
        {
            _toggleDebug = BindInput(config, "ToggleDebugLogging", KeyCode.L, MouseButton.None,
                "Toggle debug logging.");
            _toggleAllPickups = BindInput(config, "ToggleAutomaticPickups", KeyCode.J, MouseButton.None,
                "Toggle automatic resource pickups for all bots.");
            _toggleAllUses = BindInput(config, "ToggleAutomaticResourceSharing", KeyCode.K, MouseButton.None,
                "Toggle automatic resource use and sharing for all bots.");
            _selectDauda = BindInput(config, "SelectDauda", KeyCode.Alpha8, MouseButton.None,
                "Hold to command Dauda.");
            _selectHackett = BindInput(config, "SelectHackett", KeyCode.Alpha9, MouseButton.None,
                "Hold to command Hackett.");
            _selectBishop = BindInput(config, "SelectBishop", KeyCode.Alpha0, MouseButton.None,
                "Hold to command Bishop.");
            _selectWoods = BindInput(config, "SelectWoods", KeyCode.F6, MouseButton.None,
                "Hold to command Woods.");
            _attackCommand = BindInput(config, "AttackAimedEnemy", KeyCode.None, MouseButton.Middle,
                "While holding a bot selector, attack the enemy under the crosshair.");
            _pickupCommand = BindInput(config, "PickUpAimedResource", KeyCode.U, MouseButton.Forward,
                "While holding a bot selector, pick up the resource under the crosshair.");
            _shareCommand = BindInput(config, "ShareResourceWithAimedPlayer", KeyCode.I, MouseButton.Back,
                "While holding a bot selector, share resources with the aimed player, or with you when no player is aimed at.");

            config.Save();
        }

        private static InputBinding BindInput(
            ConfigFile config,
            string name,
            KeyCode defaultKey,
            MouseButton defaultMouseButton,
            string description)
        {
            var key = config.Bind(
                "Keyboard",
                name,
                defaultKey,
                $"{description} Set this to None to disable the keyboard binding.");
            var mouseButton = config.Bind(
                "Mouse",
                name,
                defaultMouseButton,
                $"{description} Set this to None to disable the mouse binding.");

            return new InputBinding(key, mouseButton);
        }

        private enum MouseButton
        {
            None = -1,
            Left = 0,
            Right = 1,
            Middle = 2,
            Back = 3,
            Forward = 4,
            Extra5 = 5,
            Extra6 = 6,
        }

        private sealed class InputBinding
        {
            private readonly ConfigEntry<KeyCode> _key;
            private readonly ConfigEntry<MouseButton> _mouseButton;

            public InputBinding(ConfigEntry<KeyCode> key, ConfigEntry<MouseButton> mouseButton)
            {
                _key = key;
                _mouseButton = mouseButton;
            }

            public bool GetDown()
            {
                return (_key.Value != KeyCode.None && Input.GetKeyDown(_key.Value)) ||
                    (_mouseButton.Value != MouseButton.None && Input.GetMouseButtonDown((int)_mouseButton.Value));
            }

            public bool GetHeld()
            {
                return (_key.Value != KeyCode.None && Input.GetKey(_key.Value)) ||
                    (_mouseButton.Value != MouseButton.None && Input.GetMouseButton((int)_mouseButton.Value));
            }
        }


        public static void ReceiveZINetInfo(ulong sender, ZombifiedInitiative.ZINetInfo netInfo)
        {
            // funktio attack 0
            // funktio toggleshare 1
            // funktio togglepickup 2
            // funktio pickuppack 3
            // funktio sharepack 4
            // funktio cancel 5

            ItemInLevel? item = null;
            int itemtype = 0;
            int itemserial = 0;
            Agent? agent = null;
            PlayerAIBot? bot = null;
            ZombieComp? zbot = null;
            // if we get data from host or client, we do it here
            Debug.Log($"received data from sender " + sender + ": func:" + netInfo.FUNC + " slot:" + netInfo.SLOT + " itemtype:" + netInfo.ITEMTYPE + " itemserial:" + netInfo.ITEMSERIAL + " enemyid:" + netInfo.AGENTID); // debug poista
            if (!SNet.IsMaster) return;
            int senderindex = -1;
            for (int i = 0; i < PlayerManager.PlayerAgentsInLevel.Count; i++)
            {
                var tempplr = PlayerManager.PlayerAgentsInLevel[i];
                if (sender == tempplr.m_replicator.OwningPlayer.Lookup) senderindex = i;
            }

            if (senderindex < 0) return;
            PlayerAgent senderplr = PlayerManager.PlayerAgentsInLevel[senderindex];
            ZombifiedInitiative.L.LogInfo($"player {senderplr.PlayerName} is sender {senderplr.Sync.Replicator.OwningPlayer.Lookup} in slot {senderplr.PlayerSlotIndex}");
            // get agent by repkey
            if (netInfo.AGENTID > 0)
            {
                SNetStructs.pReplicator pRep;
                pRep.keyPlusOne = (ushort)netInfo.AGENTID;
                pAgent _agent;
                _agent.pRep = pRep;
                _agent.TryGet(out var resolvedAgent);
                agent = resolvedAgent;
            }

            itemtype = netInfo.ITEMTYPE;
            itemserial = netInfo.ITEMSERIAL;
            // get item by type and serial
            if (itemtype > 0 && itemserial > 0)
                foreach (var d in Builder.CurrentFloor.m_dimensions)
                    foreach (var t in d.Tiles)
                        foreach (var i in t.m_geoRoot.GetComponentsInChildren<ResourcePackPickup>())
                        {
                            if (i.m_packType == eResourceContainerSpawnType.AmmoWeapon && netInfo.ITEMTYPE == 1 && i.m_serialNumber == netInfo.ITEMSERIAL) item = i.TryCast<ItemInLevel>();
                            if (i.m_packType == eResourceContainerSpawnType.AmmoTool && netInfo.ITEMTYPE == 2 && i.m_serialNumber == netInfo.ITEMSERIAL) item = i.TryCast<ItemInLevel>();
                            if (i.m_packType == eResourceContainerSpawnType.Health && netInfo.ITEMTYPE == 3 && i.m_serialNumber == netInfo.ITEMSERIAL) item = i.TryCast<ItemInLevel>();
                            if (i.m_packType == eResourceContainerSpawnType.Disinfection && netInfo.ITEMTYPE == 4 && i.m_serialNumber == netInfo.ITEMSERIAL) item = i.TryCast<ItemInLevel>();
                        }

            // get bot by slot id
            if (netInfo.SLOT < 8)
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                {
                    if (bt.Value.Agent.PlayerSlotIndex != netInfo.SLOT) continue;

                    bot = bt.Value;
                    zbot = bot.GetComponent<ZombieComp>();
                    break;
                }
            }

            switch (netInfo.FUNC)
            {
                case 0:
                    if (agent == null) return;

                    var enemy = agent.TryCast<EnemyAgent>();
                    if (enemy == null) return;

                    if (netInfo.SLOT == 8)
                    {
                        foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                            SendBotToKillEnemy(bt.Key, enemy, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                    }
                    else if (bot != null)
                    {
                        SendBotToKillEnemy(bot.Agent.PlayerName, enemy, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                    }
                    break;

                case 1:
                    if (netInfo.SLOT == 8)
                    {
                        foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                        {
                            var zombieComp = bt.Value.GetComponent<ZombieComp>();
                            if (zombieComp != null)
                                zombieComp.allowedshare = !zombieComp.allowedshare;
                        }
                    }
                    else if (zbot != null)
                    {
                        zbot.allowedshare = !zbot.allowedshare;
                    }
                    break;

                case 2:
                    if (netInfo.SLOT == 8)
                    {
                        foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                        {
                            var zombieComp = bt.Value.GetComponent<ZombieComp>();
                            if (zombieComp != null)
                                zombieComp.allowedpickups = !zombieComp.allowedpickups;
                        }
                    }
                    else if (zbot != null)
                    {
                        zbot.allowedpickups = !zbot.allowedpickups;
                    }
                    break;

                case 3:
                    if (bot == null || item == null) return;

                    ExecuteBotAction(bot, new PlayerBotActionCollectItem.Descriptor(bot)
                    {
                        TargetItem = item,
                        TargetContainer = item.container,
                        TargetPosition = item.transform.position,
                        Prio = _manualActionsPriority,
                        Haste = _manualActionsHaste,
                    },
                        "Added collect item action to " + bot.Agent.PlayerName, 3, bot.m_playerAgent.PlayerSlotIndex, itemtype, itemserial, 0);
                    break;

                case 4:
                    if (agent == null || bot == null) return;

                    var human = agent.TryCast<PlayerAgent>();
                    if (human == null) return;

                    if (!bot.Backpack.HasBackpackItem(InventorySlot.ResourcePack) ||
                        !bot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out var backpackItem) ||
                        backpackItem == null)
                    {
                        return;
                    }

                    var resourcePack = backpackItem.Instance.Cast<ItemEquippable>();
                    bot.Inventory.DoEquipItem(resourcePack);

                    ExecuteBotAction(bot, new PlayerBotActionShareResourcePack.Descriptor(bot)
                    {
                        Receiver = human,
                        Item = resourcePack,
                        Prio = _manualActionsPriority,
                        Haste = _manualActionsHaste,
                    },
                        "Added share resource action to " + bot.Agent.PlayerName, 4, bot.m_playerAgent.PlayerSlotIndex, 0, 0, human.m_replicator.Key + 1);
                    break;

                case 5:
                    if (netInfo.SLOT == 8)
                    {
                        foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                        {
                            var zombieComp = bt.Value.GetComponent<ZombieComp>();
                            if (zombieComp != null)
                                zombieComp.PreventManualActions();
                        }
                    }
                    else
                    {
                        zbot?.PreventManualActions();
                    }
                    break;
            }
        }

        public void Awake()
        {
            ZombifiedInitiative.BotTable.Clear();
        }

        public void OnFactoryBuildDone()
        {
            ZombifiedInitiative.BotTable.Clear();
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (!player.Owner.IsBot) continue;

                var bot = player.GetComponent<PlayerAIBot>();
                if (bot != null)
                    ZombifiedInitiative.BotTable[player.PlayerName] = bot;
            }

            var communicationMenu = FindObjectOfType<PUI_CommunicationMenu>();
            if (communicationMenu == null)
            {
                ZombifiedInitiative.L.LogError("Could not find the player communication menu.");
                return;
            }

            ZombifiedInitiative._menu = communicationMenu;
            if (!_menuadded)
            {
                AddZombifiedText();
                AddZombifiedMenu(communicationMenu);
                ZombifiedInitiative.rootmenusetup = true;
                _menuadded = true;
            }

        }


        public static void AddZombifiedText()
        {
            TextDataBlock zombtext1 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext1", English = "Zombified Initiative" };
            TextDataBlock zombtext2 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext2", English = "AllBots attack my target" };
            TextDataBlock zombtext3 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext3", English = "AllBots toggle pickup permission" };
            TextDataBlock zombtext4 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext4", English = "AllBots clear command queue" };
            TextDataBlock zombtext5 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext5", English = "AllBots toggle share permission" };
            TextDataBlock zombtext6 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext6", English = "All Bots" };
            TextDataBlock zombtext7 = new() { internalEnabled = true, SkipLocalization = true, name = "zombtext7", English = "AllBots toggle sentry mode" };


            TextDataBlock.AddBlock(zombtext1);
            TextDataBlock.AddBlock(zombtext2);
            TextDataBlock.AddBlock(zombtext3);
            TextDataBlock.AddBlock(zombtext4);
            TextDataBlock.AddBlock(zombtext5);
            TextDataBlock.AddBlock(zombtext6);
            TextDataBlock.AddBlock(zombtext7);

            var localizationService = Text.TextLocalizationService.TryCast<GameDataTextLocalizationService>();
            if (localizationService == null)
            {
                ZombifiedInitiative.L.LogError("Could not access the game-data localization service.");
                return;
            }

            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext1"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext1"), zombtext1.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext2"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext2"), zombtext2.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext3"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext3"), zombtext3.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext4"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext4"), zombtext4.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext5"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext5"), zombtext5.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext6"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext6"), zombtext6.GetText(localizationService.CurrentLanguage));
            if (!localizationService.m_texts.ContainsKey(TextDataBlock.GetBlockID("zombtext7"))) localizationService.m_texts.Add(TextDataBlock.GetBlockID("zombtext7"), zombtext7.GetText(localizationService.CurrentLanguage));

        }

        public void Initialize()
        {
            if (!SNet.IsMaster) foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                {
                    var tmpcomp = bt.Value.gameObject.AddComponent<ZombieComp>();
                    tmpcomp.Initialize();
                }
        }

        private void Update()
        {
            if (!CanHandleInput())
                return;

            if (_toggleDebug.GetDown())
                SwitchDebug();

            if (_toggleAllPickups.GetDown())
            {
                if (SNet.IsMaster) foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable) bt.Value.GetComponent<ZombieComp>().allowedpickups = !bt.Value.GetComponent<ZombieComp>().allowedpickups;
                if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZombifiedInitiative.ZINetInfo>("ZINetInfo", new ZombifiedInitiative.ZINetInfo(2, 8, 0, 0, 0));
                Print("Automatic resource pickups toggled for all bots");
            }

            if (_toggleAllUses.GetDown())
            {
                if (SNet.IsMaster) foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable) bt.Value.GetComponent<ZombieComp>().allowedshare = !bt.Value.GetComponent<ZombieComp>().allowedshare;
                if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZombifiedInitiative.ZINetInfo>("ZINetInfo", new ZombifiedInitiative.ZINetInfo(1, 8, 0, 0, 0));
                Print("Automatic resource uses toggled for all bots");
            }

            if (_selectDauda.GetHeld())
                SendBot("Dauda");

            if (_selectHackett.GetHeld())
                SendBot("Hackett");

            if (_selectBishop.GetHeld())
                SendBot("Bishop");

            if (_selectWoods.GetHeld())
                SendBot("Woods");

            void SendBot(String bot)
            {
                if (_attackCommand.GetDown())
                {
                    var monster = GetMonsterUnderPlayerAim();
                    if (monster != null)
                    {
                        SendBotToKillEnemy(bot, monster,
                            PlayerBotActionAttack.StanceEnum.All,
                            PlayerBotActionAttack.AttackMeansEnum.All,
                            PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                    }
                }

                if (_pickupCommand.GetDown())
                {
                    var item = GetItemUnderPlayerAim();
                    if (item != null)
                        SendBotToPickupItem(bot, item);
                }

                if (_shareCommand.GetDown())
                    SendBotToShareResourcePack(bot, GetHumanUnderPlayerAim());
            }
        }

        private static bool CanHandleInput()
        {
            return FocusStateManager.CurrentState == eFocusState.FPS ||
                   FocusStateManager.CurrentState == eFocusState.Dead;
        }

        public static void AddZombifiedMenu(PUI_CommunicationMenu communicationMenu)
        {
            uint zombtb1 = TextDataBlock.GetBlockID("zombtext1");
            uint zombtb2 = TextDataBlock.GetBlockID("zombtext2");
            uint zombtb3 = TextDataBlock.GetBlockID("zombtext3");
            uint zombtb4 = TextDataBlock.GetBlockID("zombtext4");
            uint zombtb5 = TextDataBlock.GetBlockID("zombtext5");
            uint zombtb6 = TextDataBlock.GetBlockID("zombtext6");
            uint zombtb7 = TextDataBlock.GetBlockID("zombtext7");

            //ZombifiedInitiative.L.LogInfo($"debug {zombtb1} {zombtb2} {zombtb3} {zombtb4} {zombtb5} {zombtb6}");
            CommunicationNode allmenu = new(zombtb6, CommunicationNode.ScriptType.None);
            allmenu.IsLastNode = false;
            allmenu.TextId = zombtb6;
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb2, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb3, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb4, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb5, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes.Add(new CommunicationNode(zombtb7, CommunicationNode.ScriptType.None));
            allmenu.m_ChildNodes[0].DialogID = 314;
            allmenu.m_ChildNodes[1].DialogID = 314;
            allmenu.m_ChildNodes[2].DialogID = 314;
            allmenu.m_ChildNodes[3].DialogID = 314;
            allmenu.m_ChildNodes[4].DialogID = 314;

            CommunicationNode zombmenu = new(zombtb1, CommunicationNode.ScriptType.None);
            zombmenu.IsLastNode = false;
            zombmenu.TextId = zombtb1;
            zombmenu.m_ChildNodes.Add(allmenu);

            communicationMenu.m_menu.CurrentNode.ChildNodes[5].m_ChildNodes.Add(zombmenu);
        }

        #region Attack monster
        public static EnemyAgent? GetMonsterUnderPlayerAim()
        {
            return GetComponentUnderPlayerAim<EnemyAgent>
                (enemy => "Found monster: " + enemy.EnemyData.name, false);
        }


        public static void SendBotToKillEnemy(String chosenBot, Agent enemy,
            PlayerBotActionAttack.StanceEnum stance,
            PlayerBotActionAttack.AttackMeansEnum means,
            PlayerBotActionWalk.Descriptor.PostureEnum posture)
        {
            if (!ZombifiedInitiative.BotTable.TryGetValue(chosenBot, out var bot))
                return;

            ExecuteBotAction(bot, new PlayerBotActionAttack.Descriptor(bot)
            {
                Stance = stance,
                Means = means,
                Posture = posture,
                TargetAgent = enemy,
                Prio = _manualActionsPriority,
                Haste = _manualActionsHaste,
            },
                "Added kill enemy action to " + bot.Agent.PlayerName, 0, bot.m_playerAgent.PlayerSlotIndex, 0, 0, enemy.m_replicator.Key + 1);
        }
        #endregion


        #region Item pickup
        public static ItemInLevel? GetItemUnderPlayerAim()
        {
            return GetComponentUnderPlayerAim<ItemInLevel>
                (item => "Found item: " + item.PublicName);
        }


        public static void SendBotToPickupItem(String chosenBot, ItemInLevel item /*, bool resourcePack = false*/)
        {
            int itemtype = 0;
            int itemserial = 0;
            if (!ZombifiedInitiative.BotTable.TryGetValue(chosenBot, out var bot))
                return;

            var res = item.TryCast<ResourcePackPickup>();
            if (res != null && res.m_packType == eResourceContainerSpawnType.AmmoWeapon) itemtype = 1;
            if (res != null && res.m_packType == eResourceContainerSpawnType.AmmoTool) itemtype = 2;
            if (res != null && res.m_packType == eResourceContainerSpawnType.Health) itemtype = 3;
            if (res != null && res.m_packType == eResourceContainerSpawnType.Disinfection) itemtype = 4;
            if (res != null) itemserial = res.m_serialNumber;

            ExecuteBotAction(bot, new PlayerBotActionCollectItem.Descriptor(bot)
            {
                TargetItem = item,
                TargetContainer = item.container,
                TargetPosition = item.transform.position,
                Prio = _manualActionsPriority,
                Haste = _manualActionsHaste,
            },
                "Added collect item action to " + bot.Agent.PlayerName, 3, bot.m_playerAgent.PlayerSlotIndex, itemtype, itemserial, 0);
        }
        #endregion


        #region Resource pack sharing
        public static PlayerAgent GetHumanUnderPlayerAim()
        {
            var playerAIBot = GetComponentUnderPlayerAim<PlayerAIBot>
                (bot => "Found bot: " + bot.Agent.PlayerName);
            if (playerAIBot != null)
                return playerAIBot.Agent;

            var otherPlayerAgent = GetComponentUnderPlayerAim<PlayerAgent>
                (player => "Found other player: " + player.PlayerName);
            if (otherPlayerAgent != null)
                return otherPlayerAgent;

            var localPlayerAgent = PlayerManager.GetLocalPlayerAgent();
            Print("Found local player: " + localPlayerAgent.PlayerName);
            return localPlayerAgent;
        }


        public static void SendBotToShareResourcePack(String chosenBot, PlayerAgent human)
        {
            if (!ZombifiedInitiative.BotTable.TryGetValue(chosenBot, out var bot))
                return;

            if (!bot.Backpack.HasBackpackItem(InventorySlot.ResourcePack) ||
                !bot.Backpack.TryGetBackpackItem(InventorySlot.ResourcePack, out var backpackItem) ||
                backpackItem == null)
            {
                return;
            }

            var resourcePack = backpackItem.Instance.Cast<ItemEquippable>();
            bot.Inventory.DoEquipItem(resourcePack);

            ExecuteBotAction(bot, new PlayerBotActionShareResourcePack.Descriptor(bot)
            {
                Receiver = human,
                Item = resourcePack,
                Prio = _manualActionsPriority,
                Haste = _manualActionsHaste,
            },
                "Added share resource action to " + bot.Agent.PlayerName, 4, bot.m_playerAgent.PlayerSlotIndex, 0, 0, human.m_replicator.Key + 1);
        }
        #endregion




        public static void ExecuteBotAction(PlayerAIBot bot, PlayerBotActionBase.Descriptor descriptor, string message, int func, int slot, int itemtype, int itemserial, int agentid)
        {
            if (SNet.IsMaster)
            {
                bot.StartAction(descriptor);
                Print(message);
            }
            if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZombifiedInitiative.ZINetInfo>("ZINetInfo", new ZombifiedInitiative.ZINetInfo(func, slot, itemtype, itemserial, agentid));
        }


        public static T? GetComponentUnderPlayerAim<T>(System.Func<T, string> message, bool raycastAll = true) where T : class
        {
            if (raycastAll)
            {
                foreach (var raycastHit in RaycastHits())
                {
                    var component = raycastHit.collider.GetComponentInParent<T>();
                    if (component == null)
                        continue;

                    Print(message(component));
                    return component;
                }
            }
            else
            {
                var raycastHit = RaycastHit();
                if (raycastHit.HasValue)
                {
                    var component = raycastHit.Value.collider.GetComponentInParent<T>();
                    if (component == null)
                        return null;

                    Print(message(component));
                    return component;
                }
            }

            return null;
        }


        public static RaycastHit? RaycastHit()
        {
            if (Physics.Raycast(Camera.current.ScreenPointToRay(Input.mousePosition), out var hitInfo))
                return hitInfo;
            return null;
        }


        public static Il2CppStructArray<RaycastHit> RaycastHits()
        {
            return Physics.RaycastAll(Camera.current.ScreenPointToRay(Input.mousePosition));
        }


        public static void SwitchDebug()
        {
            _debug = !_debug;
            Print("Debug log " + (_debug ? "enabled" : "disabled"), true);
        }


        public static void Print(string text, bool forced = false)
        {
            if (_debug || forced)
                ZombifiedInitiative.L.LogInfo(text);
        } // print
    } // ZombieController mono
}
