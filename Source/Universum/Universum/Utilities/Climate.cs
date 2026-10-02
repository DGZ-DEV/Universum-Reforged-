using Verse;

namespace Universum.Utilities {
    [HarmonyLib.HarmonyPatch(typeof(RimWorld.WeatherDecider), "CurrentWeatherCommonality")]
    public static class WeatherDecider_CurrentWeatherCommonality {
        public static void Postfix(WeatherDef weather, ref float __result, RimWorld.WeatherDecider __instance) {
            if (!Cache.allowed_utility(MirrorWeather.map(__instance), "universum.disable_weather_change")) return;
            if (MirrorWeather.map(__instance).weatherManager.curWeather == null || weather.defName == MirrorWeather.map(__instance).weatherManager.curWeather.defName) {
                __result = 1.0f;
                return;
            }
            if (weather.defName == "OuterSpaceWeather") {
                __result = 1.0f;
                return;
            }
            __result = 0.0f;
        }
    }
}
