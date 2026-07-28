using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using Player;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using LevelGeneration;

namespace Zombified_Initiative;

[BepInDependency("dev.gtfomodding.gtfo-api")]
[BepInPlugin("com.hirnukuono.zombified_initiative", "Zombified Initiative", "0.9.6")]
public class ZombifiedInitiative : BasePlugin
{
    public static Dictionary<String, PlayerAIBot> BotTable = new();

    public static PUI_CommunicationMenu _menu;
    public static bool rootmenusetup = false;

    public static ManualLogSource L;
    public static float _manualActionsHaste = 1f;

    public struct ZINetInfo
    {

        // funktio assign target 0
        // funktio unassign target 1
        // funktio assignaim 2
        // funktio unassignaim 3
        // funktio fireguns 4
        public static string NetworkIdentity { get => nameof(ZINetInfo); }
        public int FUNC;
        public int SLOT;
        public int ITEMTYPE;
        public int ITEMSERIAL;
        public int AGENTID;

        public ZINetInfo(int func, int slot, int itemtype, int itemserial, int agentid) : this()
        {
            FUNC = func; SLOT = slot; ITEMTYPE = itemtype; ITEMSERIAL = itemserial;  AGENTID = agentid; 
            L.LogInfo($"sent a package {func} - {slot} - {itemtype} - {itemserial} - {agentid}");
        }
    }

    public struct ZIInfo
    {
        public int FUNC;
        public int SLOT;
        public int ITEMTYPE;
        public int ITEMSERIAL;
        public int AGENTID;
        public ZIInfo(int func, int slot, int itemtype, int itemserial, int agentid) : this()
        {
            FUNC = func; SLOT = slot; ITEMTYPE = itemtype; ITEMSERIAL = itemserial; AGENTID = agentid;
        }
        public ZIInfo(ZINetInfo network) : this()
        {
            FUNC = network.FUNC;
            SLOT = network.SLOT;
            ITEMTYPE = network.ITEMTYPE;
            ITEMSERIAL = network.ITEMSERIAL;
            AGENTID = network.AGENTID;
        }
    }

    public override void Load()
    {
        Harmony m_Harmony = new Harmony("ZombieController");
        m_Harmony.PatchAll();
        ClassInjector.RegisterTypeInIl2Cpp<ZombieComp>();
        var ZombieController = AddComponent<ZombieController>();
        NetworkAPI.RegisterEvent<ZINetInfo>(ZINetInfo.NetworkIdentity, ZombieController.ReceiveZINetInfo);
        LG_Factory.add_OnFactoryBuildDone((Action)ZombieController.OnFactoryBuildDone);
        EventAPI.OnExpeditionStarted += ZombieController.Initialize;
        L = Log;
    }
} // plugin
