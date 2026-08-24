using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions; // 最新のAmong Usデータ構造用

namespace AUCaptainRoleMod;

[BepInPlugin("com.zionblood.aucaptainrole", "AUCaptainRoleMod", "1.0.0")]
[BepInProcess("Among Us.exe")]
public class Plugin : BasePlugin
{
    public static byte CaptainId = 255;
    
    public static bool IsZoomedOut = false;
    public static float ZoomTimer = 0f;
    public static float ZoomCooldown = 0f;

    public static bool IsInvisible = false;
    public static float InvisCooldown = 0f;

    public static int RemainingMeetings = 1;

    public override void Load()
    {
        Log.LogInfo("AUCaptainRoleMod: 2026 Modern IL2CPP Patches Initializing...");
        
        Harmony harmony = new Harmony("com.zionblood.aucaptainrole");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public static Sprite LoadCustomSprite(string fileName)
    {
        string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", fileName);
        if (!File.Exists(path))
        {
            Texture2D fallbackTex = new Texture2D(1, 1);
            return Sprite.Create(fallbackTex, new Rect(0, 0, 1, 1), Vector2.zero);
        }
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        
        _ = UnityEngine.Imageconversion.LoadImage(texture, fileData);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    // 1. 最新のHudManagerフック（余計なプレフィックスを排除）
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class HudStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(HudManager __instance)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.PlayerId != CaptainId) return;

            Sprite zoomSprite = LoadCustomSprite("zoom_out.png");
            Sprite invisSprite = LoadCustomSprite("invisible.png");
            Sprite teleSprite = LoadCustomSprite("teleport.png");
            Sprite meetingSprite = LoadCustomSprite("button.png");
        }
    }

    // 2. 最新のPlayerControlフック
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    public static class GlobalAbilityTickPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerControl __instance)
        {
            if (__instance.PlayerId != CaptainId || !__instance.AmOwner) return;

            if (ZoomCooldown > 0f) ZoomCooldown -= Time.fixedDeltaTime;
            if (InvisCooldown > 0f) InvisCooldown -= Time.fixedDeltaTime;

            if (IsZoomedOut)
            {
                ZoomTimer -= Time.fixedDeltaTime;
                if (Camera.main != null)
                {
                    Camera.main.orthographicSize = 18f;
                    Camera.main.transform.position = new Vector3(0f, 0f, Camera.main.transform.position.z);
                }

                if (ZoomTimer <= 0f)
                {
                    IsZoomedOut = false;
                    if (Camera.main != null) Camera.main.orthographicSize = 4.5f;
                    ZoomCooldown = 20f;
                }
            }
        }
    }

    // 3. 最新のIntroCutsceneフック
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    public static class IntroSplashOverride
    {
        [HarmonyPostfix]
        public static void Postfix(IntroCutscene __instance)
        {
            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == CaptainId)
            {
                if (__instance.RoleText != null)
                {
                    __instance.RoleText.text = "Captain";
                    __instance.RoleText.color = new Color(0.66f, 0.0f, 1.0f, 1.0f);
                }
                if (__instance.RoleBlurbText != null)
                {
                    __instance.RoleBlurbText.text = "Watch everything and Find the <color=#FF2200>Impostor</color>";
                    __instance.RoleBlurbText.color = new Color(0.66f, 0.0f, 1.0f, 1.0f);
                }
                if (__instance.BackgroundBar != null)
                {
                    __instance.BackgroundBar.material.color = new Color(0.4f, 0.0f, 0.7f, 1.0f);
                }
            }
        }
    }
}
