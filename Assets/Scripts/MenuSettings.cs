using System;
using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Small, versioned PlayerPrefs store for the menu. Everything here maps to a
    /// real local feature (car choice, body colour, audio, quality, coin balance);
    /// no store, purchase or multiplayer claim is implied.
    /// </summary>
    public static class MenuSettings
    {
        private const string Prefix = "CityDrive.v2.";
        private const string CoinsKey = Prefix + "Coins";
        private const string CarKey = Prefix + "SelectedCar";
        private const string PaintKey = Prefix + "BodyColor";
        private const string MasterKey = Prefix + "MasterVolume";
        private const string SfxKey = Prefix + "SfxVolume";
        private const string QualityKey = Prefix + "Quality";
        private const string BonusDayKey = Prefix + "DailyBonusDay";

        public static int Coins
        {
            get { return PlayerPrefs.GetInt(CoinsKey, 1250); }
            private set { PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value)); }
        }

        public static string SelectedCar
        {
            get { return PlayerPrefs.GetString(CarKey, string.Empty); }
            set { PlayerPrefs.SetString(CarKey, value ?? string.Empty); }
        }

        public static int BodyColor
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(PaintKey, 0), 0, 5); }
            set { PlayerPrefs.SetInt(PaintKey, Mathf.Clamp(value, 0, 5)); }
        }

        public static float MasterVolume
        {
            get { return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 0.9f)); }
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); }
        }

        public static float SfxVolume
        {
            get { return Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 0.85f)); }
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); }
        }

        public static int QualityLevel
        {
            get
            {
                int max = Mathf.Max(0, QualitySettings.names.Length - 1);
                return Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, Mathf.Min(1, max)), 0, max);
            }
            set { PlayerPrefs.SetInt(QualityKey, Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1))); }
        }

        public static bool BonusAvailable
        {
            get { return PlayerPrefs.GetString(BonusDayKey, string.Empty) != DateTime.Now.ToString("yyyy-MM-dd"); }
        }

        /// <summary>One local bonus per calendar day. No ads, purchases or rewards SDK.</summary>
        public static bool ClaimDailyBonus(int amount)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(BonusDayKey, string.Empty) == today) return false;
            PlayerPrefs.SetString(BonusDayKey, today);
            Coins += Mathf.Max(0, amount);
            PlayerPrefs.Save();
            return true;
        }

        public static void SelectCar(string prefabName)
        {
            SelectedCar = prefabName;
            PlayerPrefs.Save();
        }

        public static void Apply(VehicleAudio audio)
        {
            AudioListener.volume = MasterVolume;
            if (audio != null) audio.SetMasterVolume(SfxVolume);
            QualitySettings.SetQualityLevel(QualityLevel, true);
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
