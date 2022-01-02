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
        public static bool debugMode;
        internal Harmony Harmony = new("jas.ThunderstormTotem");

        public override void Entry(IModHelper helper)
        {
            I18N = helper.Translation;
            Logger = Monitor;
            ModHelper = helper;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            config = helper.ReadConfig<Config>();
            debugMode = config.Debug;
            Harmony.Patch(
                original: AccessTools.Method(typeof(Object), "rainTotem"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(RainTotemPatch), "RainTotemPrefix"))
            );
            Logger.Log("Patched Object::rainTotem");
        }

        public void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // Check whether the player's holding something and whether that something is a Rain Totem
            // 681 is the ParentSheetIndex for the Rain Totem. There's no const for it though.
            if (Game1.player.ActiveObject != null && Game1.player.ActiveObject.ParentSheetIndex == 681)
            {
                // If the player's holding something and the new menu is a DialogueBox
                if (e.NewMenu is DialogueBox box)
                {
                    if (box.dialogues != null &&
                        box.dialogues.Count > 0 &&
                        box.dialogues[0] == Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12822"))
                    {
                        if (debugMode) Logger.Log("Rain Totem DialogueBox detected.", LogLevel.Info);
                        // code below only runs if it's the Rain Totem DialogueBox and AlreadyRaining is flagged as true
                        // AlreadyRaining is updated each time the Rain Totem is used, before it's actually used
                        if (AlreadyRaining) {
                            if (debugMode) Logger.Log("Stormy Weather Conjured.", LogLevel.Info);
                            box.dialogues.Clear();
                            box.dialogues.Add(I18N.Get("ThunderstormTotem.RainToThunder"));
                            Game1.netWorldState.Value.WeatherForTomorrow = Game1.weatherForTomorrow = Game1.weather_lightning;
                        }
                        
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Object), "rainTotem")]
    public class RainTotemPatch
    {

        private static bool RainTotemPrefix(Item __instance, Farmer who)
        {
            if (ModEntry.debugMode) ModEntry.Logger.Log("RainTotemPrefix executed.", LogLevel.Info);
            var weather = Game1.netWorldState.Value.GetWeatherForLocation(Game1.currentLocation.GetLocationContext()).weatherForTomorrow.Value;

            // If tomorrow is a festival day or if the weather for tomorrow is not going to be rain, set AlreadyRaining to false
            if (Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.currentSeason))
            {
                if (ModEntry.debugMode) ModEntry.Logger.Log("Festival Tomorrow, no rain", LogLevel.Info);
                ModEntry.AlreadyRaining = false;
            }
            else if (weather == Game1.weather_rain || weather == Game1.weather_lightning)
            {
                if (ModEntry.debugMode) ModEntry.Logger.Log((weather == Game1.weather_rain ? "Rain" : "Storm") + " detected, enabling AlreadyRaining flag", LogLevel.Info);
                ModEntry.AlreadyRaining = true;
            }
            else if (weather != Game1.weather_rain)
            {
                if (ModEntry.debugMode) ModEntry.Logger.Log("No rain tomorrow, weather detected as: " + weather.ToString(), LogLevel.Info);
                ModEntry.AlreadyRaining = false;
            }
            else
            {
                ModEntry.Logger.Log("Something's wrong with the weather! Weather is: " + weather.ToString(), LogLevel.Error);
            }
            return true;
        }
    }
}
