// 1. FIXED ORDER: Global using alias placed at the absolute top of the source file
global using il2cpp = Il2Cpp;

using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;
using Il2Cpp;



namespace AUCaptainRoleMod;

[BepInPlugin("com.zionblood.aucaptainrole", "AUCaptainRoleMod", "1.0.0")]
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
        Log.LogInfo("AUCaptainRoleMod: Deploying full integrated codebase...");
        Harmony.CreateAndPatchAll(typeof(Plugin));
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

    [HarmonyPatch(typeof(il2cpp.HudManager), nameof(il2cpp.HudManager.Start))]
    [HarmonyPostfix]
    public static void HudStartPatch(il2cpp.HudManager __instance)
    {
        if (il2cpp.PlayerControl.LocalPlayer == null || il2cpp.PlayerControl.LocalPlayer.PlayerId != CaptainId) return;

        Sprite zoomSprite = LoadCustomSprite("zoom_out.png");
        Sprite invisSprite = LoadCustomSprite("invisible.png");
        Sprite teleSprite = LoadCustomSprite("teleport.png");
        Sprite meetingSprite = LoadCustomSprite("button.png");
    }

    [HarmonyPatch(typeof(il2cpp.PlayerControl), nameof(il2cpp.PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    public static void GlobalAbilityTickPatch(il2cpp.PlayerControl __instance)
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

    [HarmonyPatch(typeof(il2cpp.IntroCutscene), nameof(il2cpp.IntroCutscene.BeginCrewmate))]
    [HarmonyPostfix]
    public static void IntroSplashOverride(il2cpp.IntroCutscene __instance)
    {
        if (il2cpp.PlayerControl.LocalPlayer != null && il2cpp.PlayerControl.LocalPlayer.PlayerId == CaptainId)
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
