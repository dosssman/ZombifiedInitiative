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
        ZombieComp whocomp = null;
        CommunicationNode node = ZombifiedInitiative._menu.m_menu.CurrentNode;
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
                            whocomp = bt.Value.GetComponent<ZombieComp>();
                            if (whocomp.pickupaction != null) whocomp.pickupaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                            whocomp.allowedpickups = !whocomp.allowedpickups;
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
                            whocomp = bt.Value.GetComponent<ZombieComp>();
                            if (whocomp.shareaction != null) whocomp.shareaction.DescBase.SetCompletionStatus(PlayerBotActionBase.Descriptor.StatusType.Failed);
                            whocomp.allowedshare = !whocomp.allowedshare;
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
                            whocomp = bt.Value.GetComponent<ZombieComp>();
                            whocomp.PreventManualActions();
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
                    {
                        bt.Value.GetComponent<ZombieComp>().allowedmove = !bt.Value.GetComponent<ZombieComp>().allowedmove;
                        if (bt.Value.GetComponent<ZombieComp>().allowedmove == true) bt.Value.GetComponent<ZombieComp>().followaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                        if (bt.Value.GetComponent<ZombieComp>().allowedmove == true) bt.Value.GetComponent<ZombieComp>().travelaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;

                    }
                }
                else
                {
                    L.LogInfo($"bot " + who + " sentry mode");
                    BotTable[who].GetComponent<ZombieComp>().allowedmove = !BotTable[who].GetComponent<ZombieComp>().allowedmove;
                    if (BotTable[who].GetComponent<ZombieComp>().allowedmove == true) BotTable[who].GetComponent<ZombieComp>().followaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                    if (BotTable[who].GetComponent<ZombieComp>().allowedmove == true) BotTable[who].GetComponent<ZombieComp>().travelaction.DescBase.Status = PlayerBotActionBase.Descriptor.StatusType.None;
                }
            }
        } // if islastnode
    } // playconfirm
} // zombifiedpatches
