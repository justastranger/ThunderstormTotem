using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;
using HarmonyLib;

namespace ThunderstormTotem
{
    public class ModEntry : Mod
    {
        // internal Config config;
        public static ITranslationHelper I18N;
        public static IMonitor Logger;
        public static IModHelper ModHelper;
        internal Harmony Harmony = new Harmony("jas.ThunderstormTotem");

        public override void Entry(IModHelper helper)
        {
            I18N = helper.Translation;
            Logger = Monitor;
            ModHelper = helper;
            // config = helper.ReadConfig<Config>();
            Harmony.Patch(
                original: AccessTools.Method(typeof(Object), "rainTotem"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(RainTotemPatch), "RainTotemPrefix"))
            );
            Logger.Log("Patched Object::rainTotem");
        }
    }

    [HarmonyPatch(typeof(Object), "rainTotem")]
    public class RainTotemPatch
    {

        private static bool RainTotemPrefix(Item __instance, Farmer who)
        {
            ModEntry.Logger.Log("RainTotemPrefix executed.");
            // If tomorrow is a festival day, let the original function run so it can do nothing for us
            if (Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.currentSeason))
            {
                return true;
            }

            var locationContext = Game1.currentLocation.GetLocationContext();
            var weather = Game1.netWorldState.Value.GetWeatherForLocation(locationContext).weatherForTomorrow.Value;

            // If it's not already going to rain, go to the original function
            if (weather != 1) return true;
            
            ModEntry.Logger.Log("Stormy Weather Conjured.");

            Game1.pauseThenMessage(2000, ModEntry.I18N.Get("ThunderstormTotem.RainToThunder"), false);
            Game1.netWorldState.Value.WeatherForTomorrow = (Game1.weatherForTomorrow = 3);

            Game1.screenGlow = false;
            who.currentLocation.playSound("thunder");
            who.canMove = false;
            Game1.screenGlowOnce(Color.SlateBlue, hold: false);
            Game1.player.faceDirection(2);
            Game1.player.FarmerSprite.animateOnce(new[]
            {
                new FarmerSprite.AnimationFrame(57, 2000, secondaryArm: false, flip: false, Farmer.canMoveNow, behaviorAtEndOfFrame: true)
            });
            var multiplayer = ModEntry.ModHelper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
            for (var i = 0; i < 6; i++)
            {
                multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 2f, 0.01f, 0f, 0f)
                {
                    motion = new Vector2(Game1.random.Next(-10, 11) / 10f, -2f),
                    delayBeforeAnimationStart = i * 200
                });
                multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f)
                {
                    motion = new Vector2(Game1.random.Next(-30, -10) / 10f, -1f),
                    delayBeforeAnimationStart = 100 + i * 200
                });
                multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Rectangle(648, 1045, 52, 33), 9999f, 1, 999, who.Position + new Vector2(0f, -128f), flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f)
                {
                    motion = new Vector2(Game1.random.Next(10, 30) / 10f, -1f),
                    delayBeforeAnimationStart = 200 + i * 200
                });
            }
            multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(__instance.ParentSheetIndex, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
            {
                motion = new Vector2(0f, -7f),
                acceleration = new Vector2(0f, 0.1f),
                scaleChange = 0.015f,
                alpha = 1f,
                alphaFade = 0.0075f,
                shakeIntensity = 1f,
                initialPosition = Game1.player.Position + new Vector2(0f, -96f),
                xPeriodic = true,
                xPeriodicLoopTime = 1000f,
                xPeriodicRange = 4f,
                layerDepth = 1f
            });
            DelayedAction.playSoundAfterDelay("rainsound", 2000);

            return false; // skips the original function

        }
    }
}
