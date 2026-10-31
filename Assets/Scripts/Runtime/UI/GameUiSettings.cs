using System;
using CrashSimulator.CameraSystem;
using UnityEngine;

namespace CrashSimulator.UI
{
    /// <summary>Small, explicit preferences. Only settings connected to runtime behavior are exposed.</summary>
    public static class GameUiSettings
    {
        private const string Prefix = "CrashSimulator.UI.v1.";
        public static event Action Changed;
        public static int Graphics => Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Graphics",1),0,2);
        public static float SteeringSensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+"Steering",1),.5f,1.5f);
        public static float ControlOpacity => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+"Opacity",.7f),.4f,1f);
        public static bool LeftHanded => PlayerPrefs.GetInt(Prefix+"LeftHanded",0)!=0;
        public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Master",1));
        public static bool SoundMuted => PlayerPrefs.GetInt(Prefix+"SoundMuted",0)!=0;
        public static float EffectsVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Effects",1));
        public static bool CameraShake => PlayerPrefs.GetInt(Prefix+"Shake",1)!=0;
        public static bool ReducedMotion => PlayerPrefs.GetInt(Prefix+"ReducedMotion",0)!=0;
        public static bool MilesPerHour => PlayerPrefs.GetInt(Prefix+"Mph",0)!=0;

        public static void SetGraphics(int value) { PlayerPrefs.SetInt(Prefix+"Graphics",Mathf.Clamp(value,0,2)); Commit(); Apply(); }
        public static void SetSteeringSensitivity(float value) { PlayerPrefs.SetFloat(Prefix+"Steering",Mathf.Clamp(value,.5f,1.5f)); Commit(); }
        public static void SetControlOpacity(float value) { PlayerPrefs.SetFloat(Prefix+"Opacity",Mathf.Clamp(value,.4f,1)); Commit(); }
        public static void SetLeftHanded(bool value) { PlayerPrefs.SetInt(Prefix+"LeftHanded",value?1:0); Commit(); }
        public static void SetMasterVolume(float value) { PlayerPrefs.SetFloat(Prefix+"Master",Mathf.Clamp01(value)); Commit(); Apply(); }
        public static void SetSoundMuted(bool value) { PlayerPrefs.SetInt(Prefix+"SoundMuted",value?1:0); Commit(); Apply(); }
        public static void SetEffectsVolume(float value) { PlayerPrefs.SetFloat(Prefix+"Effects",Mathf.Clamp01(value)); Commit(); }
        public static void SetCameraShake(bool value) { PlayerPrefs.SetInt(Prefix+"Shake",value?1:0); Commit(); }
        public static void SetReducedMotion(bool value) { PlayerPrefs.SetInt(Prefix+"ReducedMotion",value?1:0); Commit(); }
        public static void SetMilesPerHour(bool value) { PlayerPrefs.SetInt(Prefix+"Mph",value?1:0); Commit(); }
        private static void Commit()
        {
            UiSettingsSaveScheduler.Schedule();
            Changed?.Invoke();
        }
        internal static void Flush() => PlayerPrefs.Save();
        public static void Apply()
        {
            AudioListener.volume = SoundMuted ? 0f : MasterVolume;
            // The project contains Mobile and PC URP quality tiers. Profiles adjust live
            // shadow distance and LOD without changing ProjectSettings or pipeline assets.
            int tier = Graphics;
            bool desktop = Application.platform==RuntimePlatform.OSXPlayer || Application.platform==RuntimePlatform.WindowsPlayer
                || Application.platform==RuntimePlatform.LinuxPlayer || Application.isEditor;
            QualitySettings.SetQualityLevel(tier==2 && desktop ? Mathf.Min(1,QualitySettings.names.Length-1) : 0,true);
            QualitySettings.shadowDistance = tier==0 ? 18 : tier==1 ? 35 : 50;
            QualitySettings.lodBias = tier==0 ? .7f : tier==1 ? 1 : 1.35f;
            Application.targetFrameRate = tier==2 ? 60 : 30;
        }
        public static void ApplyCamera(VehicleFollowCamera camera)
        { if (camera!=null) camera.SetShakeEnabled(CameraShake); }
    }


}
