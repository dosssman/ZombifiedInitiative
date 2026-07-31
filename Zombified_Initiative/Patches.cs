using GameData;
using GTFO.API;
using HarmonyLib;
using Player;
using SNetwork;
using static Zombified_Initiative.ZombifiedInitiative;

namespace Zombified_Initiative;

[HarmonyPatch]
public class ZombifiedPatches
{
    [HarmonyPatch(typeof(PlayerAIBot), nameof(PlayerAIBot.SetEnabled))]
    [HarmonyPostfix]

    public static void AddComp(PlayerAIBot __instance, bool state)
    {
        if (!state) return;
        if (!__instance.gameObject.GetComponent<ZombieComp>())
        {
            L.LogInfo($"adding zombified component to {__instance.Agent.PlayerName} ..");
            var gaa = __instance.Agent.gameObject.AddComponent<ZombieComp>();
            gaa.Initialize();
            return;
        }
    }

    [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.OnDestroy))]
    [HarmonyPrefix]

    public static void DestroyMenu(PlayerAgent __instance)
    {
        var tempcomp = __instance.gameObject.GetComponent<ZombieComp>();
        if (tempcomp != null)
        {
            L.LogInfo($"zombiebot leaving, buh byeeee");
            tempcomp.started = false;
        }
    }

    [HarmonyPatch(typeof(CommunicationMenu), nameof(CommunicationMenu.PlayConfirmSound))]
    [HarmonyPrefix]

    public static void PlayConfirmSound(CommunicationMenu __instance)
    {
        var communicationMenu = ZombifiedInitiative._menu;
        if (communicationMenu == null) return;

        CommunicationNode node = communicationMenu.m_menu.CurrentNode;
        if (node.IsLastNode)
        {
            String jee = TextDataBlock.GetBlock(node.TextId).English;
            L.LogDebug($"teksti on " + jee);
            String who = jee.Split(new char[] { ' ' })[0].Trim();
            String wha = jee.Substring(who.Length).Trim();
            L.LogDebug($"teksti on " + jee + ", who on " + who + " ja wha on " + wha);

            if (wha == "attack my target")
            {
                var monster = ZombieController.GetMonsterUnderPlayerAim();
                if (monster != null)
                {
                    if (who == "AllBots")
                    {
                        L.LogInfo("all bots attack");
                        foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                        {
                            ZombieController.SendBotToKillEnemy(bt.Key, monster,
                                PlayerBotActionAttack.StanceEnum.All,
                                PlayerBotActionAttack.AttackMeansEnum.All,
                                PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                        }
                    }
                    else
                    {
                        L.LogInfo($"bot " + who + " attack");
                        ZombieController.SendBotToKillEnemy(who, monster, PlayerBotActionAttack.StanceEnum.All, PlayerBotActionAttack.AttackMeansEnum.All, PlayerBotActionWalk.Descriptor.PostureEnum.Stand);
                    }
                } // if monster is not null
            } // if wha attack

            if (wha.Contains("pickup permission"))
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                {
                    if (who == "AllBots" || who == bt.Key)
                    {
                        L.LogInfo($"{bt.Key} toggle resource pickups");
                        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZombifiedInitiative.ZINetInfo>("ZINetInfo", new ZombifiedInitiative.ZINetInfo(2, bt.Value.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                        if (SNet.IsMaster)
                        {
                            var zombieComp = bt.Value.GetComponent<ZombieComp>();
                            if (zombieComp == null) continue;

                            if (zombieComp.pickupaction != null) zombieComp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                            zombieComp.allowedpickups = !zombieComp.allowedpickups;
                        }
                    }
                }
            }

            if (wha.Contains("share permission"))
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                {
                    if (who == "AllBots" || who == bt.Key)
                    {
                        L.LogInfo($"{bt.Key} toggle resource use");
                        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZombifiedInitiative.ZINetInfo>("ZINetInfo", new ZombifiedInitiative.ZINetInfo(1, bt.Value.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                        if (SNet.IsMaster)
                        {
                            var zombieComp = bt.Value.GetComponent<ZombieComp>();
                            if (zombieComp == null) continue;

                            if (zombieComp.shareaction != null) zombieComp.shareaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                            zombieComp.allowedshare = !zombieComp.allowedshare;
                        }
                    }
                }
            }

            if (wha == "clear command queue")
            {
                foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                {
                    if (who == "AllBots" || who == bt.Key)
                    {
                        L.LogInfo($"{bt.Key} stop action");
                        if (!SNet.IsMaster) NetworkAPI.InvokeEvent<ZombifiedInitiative.ZINetInfo>("ZINetInfo", new ZombifiedInitiative.ZINetInfo(5, bt.Value.m_playerAgent.PlayerSlotIndex, 0, 0, 0));
                        if (SNet.IsMaster)
                        {
                            var zombieComp = bt.Value.GetComponent<ZombieComp>();
                            if (zombieComp == null) continue;

                            zombieComp.PreventManualActions();
                        }
                    }
                }
            }

            if (wha == "pickup resource under my aim")
            {
                L.LogInfo($"bot " + who + " pickup resource");
                var item = ZombieController.GetItemUnderPlayerAim();
                if (item != null)
                    ZombieController.SendBotToPickupItem(who, item);
            }

            if (wha == "supply resource (aimed or me)")
            {
                L.LogInfo($"bot " + who + " share resource");
                ZombieController.SendBotToShareResourcePack(who, ZombieController.GetHumanUnderPlayerAim());
            }

            if (wha.Contains("sentry mode"))
            {
                if (who == "AllBots")
                {
                    L.LogInfo("all bots sentry mode");
                    foreach (KeyValuePair<String, PlayerAIBot> bt in ZombifiedInitiative.BotTable)
                        ToggleSentryMode(bt.Value);
                }
                else
                {
                    L.LogInfo($"bot " + who + " sentry mode");
                    if (BotTable.TryGetValue(who, out var bot))
                        ToggleSentryMode(bot);
                }
            }
        } // if islastnode
    } // playconfirm

    private static void ToggleSentryMode(PlayerAIBot bot)
    {
        var zombieComp = bot.GetComponent<ZombieComp>();
        if (zombieComp == null) return;

        zombieComp.allowedmove = !zombieComp.allowedmove;
        L.LogInfo($"{bot.Agent.PlayerName} sentry mode {(!zombieComp.allowedmove ? "enabled" : "disabled")}");
    }

    private static bool IsSentryMode(PlayerBotActionBase action)
    {
        var agent = action.m_agent;
        if (agent == null) return false;

        var zombieComp = agent.GetComponent<ZombieComp>();
        return zombieComp != null && !zombieComp.allowedmove;
    }

    // Do not offer the root follow action while the bot is guarding a position.
    // Skipping the candidate lets the bot scheduler stop an active follow action
    // normally instead of leaving its descriptor in an invalid state.
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionFollowPlayer))]
    [HarmonyPrefix]
    private static bool UpdateActionFollowPlayerPrefix(RootPlayerBotAction __instance)
    {
        return !IsSentryMode(__instance);
    }

    // Attack owns a travel child of its own. Use the movement switch provided by
    // the attack descriptor so equipping, reloading, and firing remain available.
    [HarmonyPatch(typeof(RootPlayerBotAction), nameof(RootPlayerBotAction.UpdateActionAttack))]
    [HarmonyPostfix]
    private static void UpdateActionAttackPostfix(RootPlayerBotAction __instance)
    {
        if (!IsSentryMode(__instance) || __instance.m_attackAction == null) return;

        __instance.m_attackAction.MovementAllowed = false;
    }

    // A biotracker has no ammunition. In sentry mode it should scan from the
    // guarded position instead of starting its PlayerBotActionTravel child.
    [HarmonyPatch(typeof(PlayerBotActionUseEnemyScanner), nameof(PlayerBotActionUseEnemyScanner.VerifyCurrentPosition))]
    [HarmonyPostfix]
    private static void VerifyEnemyScannerPositionPostfix(
        PlayerBotActionUseEnemyScanner __instance,
        ref bool __result)
    {
        if (!__result && IsSentryMode(__instance))
            __result = true;
    }

    // Validate the firearm selected by the vanilla/BetterBots attack chooser.
    // This is deliberately limited to the standard and special slots: class/tool
    // ammo (including sentries) is independent, and the biotracker uses no ammo.
    [HarmonyPatch(typeof(PlayerBotActionAttack), nameof(PlayerBotActionAttack.ChooseAttackOption))]
    [HarmonyPostfix]
    [HarmonyAfter("com.east.bb")]
    private static void ChooseAttackOptionPostfix(PlayerBotActionAttack __instance, bool __result)
    {
        if (!__result || !IsSentryMode(__instance)) return;

        var attackOption = __instance.m_currentAttackOption;
        var backpack = __instance.m_backpack;
        var selectedWeapon = attackOption?.ItemToUse;
        if (attackOption == null || backpack == null || selectedWeapon == null ||
            (attackOption.Means & PlayerBotActionAttack.AttackMeansEnum.Bullet) == 0)
        {
            return;
        }

        var selectedSlot = backpack.GetBackpackSlot(selectedWeapon);
        if (selectedSlot != InventorySlot.GearStandard && selectedSlot != InventorySlot.GearSpecial)
            return;

        if (PlayerBotActionAttack.HasAmmo(backpack, selectedSlot)) return;

        var fallbackSlot = selectedSlot == InventorySlot.GearStandard
            ? InventorySlot.GearSpecial
            : InventorySlot.GearStandard;
        if (!TryGetUsableFirearm(backpack, fallbackSlot, out var fallbackWeapon))
            return;

        attackOption.ItemToUse = fallbackWeapon;
        L.LogInfo($"{__instance.m_agent.PlayerName} switched from empty {selectedSlot} to {fallbackSlot}");
    }

    private static bool TryGetUsableFirearm(
        PlayerBackpack backpack,
        InventorySlot slot,
        out ItemEquippable? weapon)
    {
        weapon = null;
        if (!PlayerBotActionAttack.HasAmmo(backpack, slot) ||
            !backpack.TryGetBackpackItem(slot, out var backpackItem) ||
            backpackItem is null)
        {
            return false;
        }

        var itemInstance = backpackItem.Instance;
        if (itemInstance is null) return false;

        weapon = itemInstance.TryCast<ItemEquippable>();
        return weapon != null;
    }
} // zombifiedpatches
