using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LCThirdPerson.Patches;
using UnityEngine;
using UnityEngine.Events;

namespace LCThirdPerson
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ThirdPersonPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);

        public static ThirdPersonPlugin Instance { get; private set; }
        public static Transform Camera { get; internal set; }
        public static Transform OriginalTransform { get; internal set; }

        public static Sprite CrosshairSprite { get; internal set; }

        public ConfigEntry<KeyboardShortcut> Enable { get; internal set; }
        public ConfigEntry<bool> ShowCursor { get; set; }
        public ConfigEntry<bool> StartEnabled { get; set; }
        public ConfigEntry<Vector3> Offset { get; set; }
        public ConfigEntry<float> CameraMaxHeight { get; set; }
        public ConfigEntry<float> CameraLookDownOffset { get; set; }
        public ConfigEntry<bool> AlwaysHideVisor { get; set; }
        public ConfigEntry<bool> FirstPersonVrm { get; set; }
        public ConfigEntry<float> FirstPersonVrmHeadHideDistance { get; set; }

        private bool tpEnabled;
        public bool Enabled {
            get { return tpEnabled; }
            set { SetEnabled(value); }
        }

        private bool enablePressed = false;

        internal static ManualLogSource Log { get; set; }
        internal UnityEvent OnEnable { get; private set; }
        internal UnityEvent OnDisable { get; private set; }

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            if (Instance == null)
            {
                Instance = this;
            }

            Log = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            OnEnable = new UnityEvent();
            OnDisable = new UnityEvent();

            SetConfig();
            SetCrosshairSprite();

            harmony.PatchAll(typeof(ThirdPersonPlugin));
            harmony.PatchAll(typeof(CameraPatch));
            harmony.PatchAll(typeof(CursorPatch));
            harmony.PatchAll(typeof(HUDPatch));
            harmony.PatchAll(typeof(ShovelPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
        }

        internal void SetConfig()
        {
            Enable = Config.Bind("Keybinds", "Toggle", new KeyboardShortcut(KeyCode.V));
            ShowCursor = Config.Bind("Options", "ShowCursor", true);
            StartEnabled = Config.Bind("Options", "StartEnabled", true);
            Offset = Config.Bind("Options", "CameraOffset", new Vector3(0.4f, 0f, -2f));
            CameraMaxHeight = Config.Bind("Options", "CameraMaxHeight", 1f);
            CameraLookDownOffset = Config.Bind("Options", "CameraLookDownOffset", 0.2f);
            AlwaysHideVisor = Config.Bind("Options", "AlwaysHideVisor", false);
            FirstPersonVrm = Config.Bind("Options", "FirstPersonVRM", false);
            FirstPersonVrmHeadHideDistance = Config.Bind("Options", "VrmHeadHideDistance", 0.8f);

            Enabled = StartEnabled.Value;
        }

        internal void SetCrosshairSprite()
        {
            CrosshairSprite = CursorPatch.CreateCrosshairSprite();
        }

        internal void CheckEnable()
        {
            // if (Enable.Value.IsPressed())
            if (UnityInput.Current.GetKey(Enable.Value.MainKey))
            {
                if (!enablePressed)
                {
                    Enabled = !Enabled;
                    enablePressed = true;
                }
            }
            else
            {
                enablePressed = false;
            }
        }

        public void SetEnabled(bool value)
        {
            tpEnabled = value;

            if (tpEnabled)
            {
                OnEnable.Invoke();
            }
            else
            {
                OnDisable.Invoke();
            }
        }
    }
}
