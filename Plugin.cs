using System;
using System.IO;
using System.Reflection;

using BepInEx;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;

using UnityEngine;

namespace AUCaptainRoleMod
{
    [BepInPlugin(
        "com.zionblood.aucaptainrole",
        "AUCaptainRoleMod",
        "1.0.0"
    )]
    [BepInProcess("Among Us.exe")]
    public class Plugin : BasePlugin
    {
        public static Harmony HarmonyInstance;

        // ============================================================
        // CAPTAIN STATE
        // ============================================================

        // 255 = nobody assigned yet.
        public static byte CaptainId = 255;

        public static bool IsCaptain
        {
            get
            {
                if (PlayerControl.LocalPlayer == null)
                    return false;

                return PlayerControl.LocalPlayer.PlayerId == CaptainId;
            }
        }

        // ============================================================
        // ZOOM
        // ============================================================

        public static bool IsZoomedOut = false;

        public static float ZoomTimer = 0f;

        public static float ZoomCooldown = 0f;

        private const float NormalZoom = 4.5f;
        private const float CaptainZoom = 18f;
        private const float ZoomDuration = 8f;
        private const float ZoomCooldownDuration = 20f;

        // ============================================================
        // INVISIBILITY
        // ============================================================

        public static bool IsInvisible = false;

        public static float InvisCooldown = 0f;

        // ============================================================
        // OTHER CAPTAIN ABILITIES
        // ============================================================

        public static int RemainingMeetings = 1;

        // ============================================================
        // PLUGIN LOAD
        // ============================================================

        public override void Load()
        {
            Log.LogInfo("======================================");
            Log.LogInfo("AUCaptainRoleMod");
            Log.LogInfo("Captain Role Mod - v1.0.0");
            Log.LogInfo("Initializing...");
            Log.LogInfo("======================================");

            try
            {
                HarmonyInstance = new Harmony("com.zionblood.aucaptainrole");

                HarmonyInstance.PatchAll(
                    Assembly.GetExecutingAssembly()
                );

                Log.LogInfo("Harmony patches loaded successfully.");
            }
            catch (Exception ex)
            {
                Log.LogError("FAILED TO LOAD CAPTAIN MOD:");
                Log.LogError(ex);
            }
        }

        // ============================================================
        // CAPTAIN ASSIGNMENT
        // ============================================================

        public static void SetCaptain(PlayerControl player)
        {
            if (player == null)
            {
                Log.LogWarning("Tried to assign a null player as Captain.");
                return;
            }

            CaptainId = player.PlayerId;

            Log.LogInfo(
                "Captain assigned to PlayerId: " + CaptainId
            );
        }

        public static void ClearCaptain()
        {
            CaptainId = 255;

            IsZoomedOut = false;
            IsInvisible = false;

            ZoomTimer = 0f;
            ZoomCooldown = 0f;
            InvisCooldown = 0f;

            RestoreNormalCamera();

            Log.LogInfo("Captain assignment cleared.");
        }

        // ============================================================
        // CUSTOM SPRITE LOADER
        // ============================================================

        public static Sprite LoadCustomSprite(string fileName)
        {
            try
            {
                string assemblyDirectory =
                    Path.GetDirectoryName(
                        Assembly.GetExecutingAssembly().Location
                    );

                string path = Path.Combine(
                    assemblyDirectory,
                    "Assets",
                    fileName
                );

                if (!File.Exists(path))
                {
                    Log.LogWarning(
                        "Captain asset not found: " + path
                    );

                    return null;
                }

                byte[] fileData = File.ReadAllBytes(path);

                Texture2D texture = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );

                // IMPORTANT:
                // The Unity class is ImageConversion,
                // not Imageconversion.
                bool loaded = ImageConversion.LoadImage(
                    texture,
                    fileData
                );

                if (!loaded)
                {
                    Log.LogWarning(
                        "Unity failed to load image: " + fileName
                    );

                    return null;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height
                    ),
                    new Vector2(0.5f, 0.5f)
                );

                Log.LogInfo(
                    "Loaded Captain asset: " + fileName
                );

                return sprite;
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Failed to load asset " + fileName
                );

                Log.LogError(ex);

                return null;
            }
        }

        // ============================================================
        // ZOOM ABILITY
        // ============================================================

        public static bool ActivateZoom()
        {
            if (!IsCaptain)
                return false;

            if (IsZoomedOut)
                return false;

            if (ZoomCooldown > 0f)
                return false;

            IsZoomedOut = true;
            ZoomTimer = ZoomDuration;

            ApplyCaptainZoom();

            Log.LogInfo("Captain zoom activated.");

            return true;
        }

        public static void ApplyCaptainZoom()
        {
            Camera camera = Camera.main;

            if (camera == null)
                return;

            camera.orthographicSize = CaptainZoom;
        }

        public static void RestoreNormalCamera()
        {
            Camera camera = Camera.main;

            if (camera == null)
                return;

            camera.orthographicSize = NormalZoom;
        }

        public static void EndZoom()
        {
            IsZoomedOut = false;
            ZoomTimer = 0f;

            RestoreNormalCamera();

            ZoomCooldown = ZoomCooldownDuration;

            Log.LogInfo("Captain zoom ended.");
        }

        // ============================================================
        // INVISIBILITY STATE
        // ============================================================

        public static bool ActivateInvisibility()
        {
            if (!IsCaptain)
                return false;

            if (IsInvisible)
                return false;

            if (InvisCooldown > 0f)
                return false;

            IsInvisible = true;

            Log.LogInfo(
                "Captain invisibility activated."
            );

            return true;
        }

        public static void EndInvisibility()
        {
            IsInvisible = false;

            Log.LogInfo(
                "Captain invisibility ended."
            );
        }

        // ============================================================
        // PLAYER UPDATE
        // ============================================================

        [HarmonyPatch(
            typeof(PlayerControl),
            nameof(PlayerControl.FixedUpdate)
        )]
        public static class PlayerControlFixedUpdatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(
                PlayerControl __instance
            )
            {
                try
                {
                    if (__instance == null)
                        return;

                    if (!__instance.AmOwner)
                        return;

                    if (!IsCaptain)
                        return;

                    UpdateTimers();

                    UpdateZoom();

                    UpdateInvisibility();
                }
                catch (Exception ex)
                {
                    Log.LogError(
                        "Captain FixedUpdate error:"
                    );

                    Log.LogError(ex);
                }
            }
        }

        // ============================================================
        // TIMER UPDATE
        // ============================================================

        private static void UpdateTimers()
        {
            float deltaTime = Time.fixedDeltaTime;

            if (ZoomCooldown > 0f)
            {
                ZoomCooldown -= deltaTime;

                if (ZoomCooldown < 0f)
                    ZoomCooldown = 0f;
            }

            if (InvisCooldown > 0f)
            {
                InvisCooldown -= deltaTime;

                if (InvisCooldown < 0f)
                    InvisCooldown = 0f;
            }
        }

        // ============================================================
        // ZOOM UPDATE
        // ============================================================

        private static void UpdateZoom()
        {
            if (!IsZoomedOut)
                return;

            ZoomTimer -= Time.fixedDeltaTime;

            ApplyCaptainZoom();

            if (ZoomTimer <= 0f)
            {
                EndZoom();
            }
        }

        // ============================================================
        // INVISIBILITY UPDATE
        // ============================================================

        private static void UpdateInvisibility()
        {
            /*
             * The state is intentionally handled here separately
             * from the visual/network implementation.
             *
             * Invisibility needs to be synchronized correctly between
             * clients. Simply disabling the local PlayerControl GameObject
             * would NOT be a correct multiplayer implementation.
             */
        }

        // ============================================================
        // HUD INITIALIZATION
        // ============================================================

        [HarmonyPatch(
            typeof(HudManager),
            nameof(HudManager.Start)
        )]
        public static class HudStartPatch
        {
            [HarmonyPostfix]
            public static void Postfix(
                HudManager __instance
            )
            {
                try
                {
                    if (PlayerControl.LocalPlayer == null)
                        return;

                    if (!IsCaptain)
                        return;

                    Log.LogInfo(
                        "Captain HUD initialized."
                    );

                    /*
                     * Load the assets here for now.
                     *
                     * The actual Captain buttons should be created
                     * using the current v18 HUD/button implementation,
                     * rather than copying an older Among Us button
                     * tutorial.
                     */

                    Sprite zoomSprite =
                        LoadCustomSprite("zoom_out.png");

                    Sprite invisSprite =
                        LoadCustomSprite("invisible.png");

                    Sprite teleportSprite =
                        LoadCustomSprite("teleport.png");

                    Sprite meetingSprite =
                        LoadCustomSprite("button.png");

                    if (zoomSprite != null)
                        Log.LogInfo("Zoom sprite loaded.");

                    if (invisSprite != null)
                        Log.LogInfo("Invisibility sprite loaded.");

                    if (teleportSprite != null)
                        Log.LogInfo("Teleport sprite loaded.");

                    if (meetingSprite != null)
                        Log.LogInfo("Meeting sprite loaded.");
                }
                catch (Exception ex)
                {
                    Log.LogError(
                        "Captain HUD initialization failed:"
                    );

                    Log.LogError(ex);
                }
            }
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        public static void ResetCaptainState()
        {
            IsZoomedOut = false;
            IsInvisible = false;

            ZoomTimer = 0f;
            ZoomCooldown = 0f;
            InvisCooldown = 0f;

            RestoreNormalCamera();
        }
    }
}
