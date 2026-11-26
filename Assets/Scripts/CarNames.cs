using System.Collections.Generic;

namespace CityDrive
{
    /// <summary>Friendly names for the user-supplied garage GLBs.</summary>
    public static class CarNames
    {
        private static readonly Dictionary<string, string> Friendly = new Dictionary<string, string>
        {
            { "930", "Porsche 911 Turbo (930)" },
            { "carrera", "Porsche 911 Carrera 4S" },
            { "centenario", "Lamborghini Centenario" },
            { "r34", "Nissan Skyline GT-R R34" },
            { "s15", "Nissan Silvia S15" },
            { "terzo", "Lamborghini Terzo Millennio" },
            { "CarConcept", "Car Concept" },
            { "Real Car 16", "Real Car 16" }
        };

        public static string Display(string prefabName)
        {
            string name;
            return !string.IsNullOrEmpty(prefabName) && Friendly.TryGetValue(prefabName, out name)
                ? name
                : prefabName;
        }
    }
}
