using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using System.Collections.Generic;

namespace ThunderstormTotem
{
    public class ModEntry : Mod
    {
        public static bool AlreadyRaining = false;
        public static ITranslationHelper I18N;
        public static IMonitor Logger;
        public static IModHelper ModHelper;
        internal Config config;
        internal Harmony Harmony = new("jas.ThunderstormTotem");

        public override void Entry(IModHelper helper)
        {
            I18N = helper.Translation;
            Logger = Monitor;
            ModHelper = helper;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            config = helper.ReadConfig<Config>();
            // Harmony.Patch(
            //     original: AccessTools.Method(typeof(Object), "rainTotem"),
            //     prefix: new HarmonyMethod(AccessTools.Method(typeof(RainTotemPatch), "RainTotemPrefix"))
            // );
            // Logger.Log("Patched Object::rainTotem");
        }

        public void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // 681 is the ParentSheetIndex for the Rain Totem. There's no const for it though.
            if (Game1.player.ActiveObject != null && Game1.player.ActiveObject.ParentSheetIndex == 681)
            {
                if (e.NewMenu is DialogueBox box)
                {
                    List<string> lines = box.dialogues;
                    if (lines != null &&
                        lines.Count == 1 &&
                        lines[0] == Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12822") &&
                        AlreadyRaining)
                    {
                        // Rain Totem DialogueBox
                        lines.Clear();
                        lines.Add(I18N.Get("ThunderstormTotem.RainToThunder"));
                        Game1.netWorldState.Value.WeatherForTomorrow = Game1.weatherForTomorrow = Game1.weather_lightning;
                    }
                }
            }
        }

        private void ConjureStormyWeather()
        {
            Logger.Log("Stormy Weather Conjured.");
            
        }
    }

    [HarmonyPatch(typeof(Object), "rainTotem")]
    public class RainTotemPatch
    {

        private static bool RainTotemPrefix(Item __instance, Farmer who)
        {
            ModEntry.Logger.Log("RainTotemPrefix executed.");
            var weather = Game1.netWorldState.Value.GetWeatherForLocation(Game1.currentLocation.GetLocationContext()).weatherForTomorrow.Value;

            // If tomorrow is a festival day or if the weather for tomorrow is not going to be rain, set AlreadyRaining to false
            if (Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.currentSeason) || weather != Game1.weather_rain)
            {
                ModEntry.AlreadyRaining = false;
            }
            else
            {
                ModEntry.AlreadyRaining = true;
            }
            
            return true;
        }
    }
}
