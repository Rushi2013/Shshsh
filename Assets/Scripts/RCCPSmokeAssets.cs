using UnityEngine;

namespace CityDrive
{
    /// <summary>
    /// Runtime lookup for the original RCC Pro v1.44 particle prefabs that were
    /// staged from the user's archive with their package GUIDs intact. Keeping
    /// the references in a Resources asset lets the procedurally built player car
    /// use the real RCCP smoke prefabs without duplicating them or guessing paths.
    /// </summary>
    [CreateAssetMenu(menuName = "City Drive/RCCP Smoke Assets")]
    public sealed class RCCPSmokeAssets : ScriptableObject
    {
        [SerializeField] private GameObject wheelSlipAsphalt;
        [SerializeField] private GameObject wheelSlipBurnout;

        public GameObject WheelSlipAsphalt => wheelSlipAsphalt;
        public GameObject WheelSlipBurnout => wheelSlipBurnout;

        private static RCCPSmokeAssets _instance;

        public static RCCPSmokeAssets Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prefer direct prefab paths: these are the real RCCP v1.44 particle
                    // prefabs, copied into Resources with fresh GUIDs. The ScriptableObject
                    // remains as a fallback for existing installations.
                    GameObject asphalt = Resources.Load<GameObject>("RCCP/RCCP_WheelSlipAsphalt");
                    GameObject burnout = Resources.Load<GameObject>("RCCP/RCCP_WheelSlipBurnout");
                    if (asphalt != null && burnout != null)
                    {
                        RCCPSmokeAssets direct = ScriptableObject.CreateInstance<RCCPSmokeAssets>();
                        direct.wheelSlipAsphalt = asphalt;
                        direct.wheelSlipBurnout = burnout;
                        _instance = direct;
                    }
                    else _instance = Resources.Load<RCCPSmokeAssets>("RCCP/RCCP_SmokeAssets");
                }
                return _instance;
            }
        }
    }
}
