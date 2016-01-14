using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SAssemblies.Detectors
{
    class Gank
    {
        public static Menu.MenuItemSettings GankDetector = new Menu.MenuItemSettings(typeof(Gank));

        private static Dictionary<Obj_AI_Hero, InternalGankDetector> Enemies = new Dictionary<Obj_AI_Hero, InternalGankDetector>();
        private int lastGameUpdateTime = 0;

        public Gank()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Game.OnUpdate += Game_OnGameUpdate;
        }

        ~Gank()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            Enemies = null;
        }

        public bool IsActive()
        {
#if DETECTORS
            return Detector.Detectors.GetActive() && GankDetector.GetActive() &&
                Game.Time < (GankDetector.GetMenuItem("SAssembliesDetectorsGankDisableTime").GetValue<Slider>().Value * 60);
#else
            return GankDetector.GetActive() &&
                Game.Time < (GankDetector.GetMenuItem("SAssembliesDetectorsGankDisableTime").GetValue<Slider>().Value * 60);
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesDetectorsGank");
            if (newMenu == null)
            {
                GankDetector.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("DETECTORS_GANK_MAIN"), "SAssembliesDetectorsGank"));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankPingTimes", Language.GetString("GLOBAL_PING_TIMES")).SetValue(new Slider(0, 5, 0)));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankPingType", Language.GetString("GLOBAL_PING_TYPE")).SetValue(new StringList(new[] 
                { 
                    Language.GetString("GLOBAL_PING_TYPE_NORMAL"), 
                    Language.GetString("GLOBAL_PING_TYPE_DANGER"), 
                    Language.GetString("GLOBAL_PING_TYPE_ENEMYMISSING"), 
                    Language.GetString("GLOBAL_PING_TYPE_ONMYWAY"), 
                    Language.GetString("GLOBAL_PING_TYPE_FALLBACK"), 
                    Language.GetString("GLOBAL_PING_ASSISTME") 
                })));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankLocalPing", Language.GetString("GLOBAL_PING_LOCAL")).SetValue(true));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankChat", Language.GetString("GLOBAL_CHAT")).SetValue(false));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankNotification", Language.GetString("GLOBAL_NOTIFICATION")).SetValue(false));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankTrackRangeMin", Language.GetString("DETECTORS_GANK_RANGE_MIN")).SetValue(new Slider(1, 10000, 1)));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankTrackRangeMax", Language.GetString("DETECTORS_GANK_RANGE_MAX")).SetValue(new Slider(1, 10000, 1)));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankDisableTime", Language.GetString("DETECTORS_GANK_DISABLETIME")).SetValue(new Slider(20, 180, 1)));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankCooldown", Language.GetString("GLOBAL_COOLDOWN")).SetValue(new Slider(5, 60, 1)));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankShowJungler", Language.GetString("DETECTORS_GANK_SHOWJUNGLER")).SetValue(false));
                GankDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsGankVoice", Language.GetString("GLOBAL_VOICE")).SetValue(false));
                GankDetector.CreateActiveMenuItem("SAssembliesDetectorsGankActive", () => new Gank());
            }
            return GankDetector;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            foreach (var enemy in Enemies)
            {
                UpdateTime(enemy);
            }
        }

        private void Init()
        {
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                Render.Text text = new Render.Text(new Vector2(0, 0), hero.IsEnemy ? Language.GetString("DETECTORS_GANK_TEXT_JUNGLER_ENEMY") :
                    Language.GetString("DETECTORS_GANK_TEXT_JUNGLER_ALLY"), 28, hero.IsEnemy ? Color.Red : Color.Green);
                text.PositionUpdate = delegate
                {
                    return Drawing.WorldToScreen(ObjectManager.Player.ServerPosition);
                };
                text.VisibleCondition = sender =>
                {
                    return IsSmiteVisible(hero);
                };
                text.OutLined = true;
                text.Centered = true;
                text.Add();
                Render.Line line = new Render.Line(new Vector2(1, 1), new Vector2(1, 1), 4, hero.IsEnemy ? Color.Red : Color.Green);
                line.StartPositionUpdate = delegate
                {
                    return Drawing.WorldToScreen(ObjectManager.Player.ServerPosition);
                };
                line.EndPositionUpdate = delegate
                {
                    return Drawing.WorldToScreen(hero.ServerPosition);
                };
                line.VisibleCondition = sender =>
                {
                    return IsSmiteVisible(hero);
                };
                line.Add();
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero, new InternalGankDetector(text, line));
                }
            }
        }

        private bool IsSmiteVisible(Obj_AI_Hero hero)
        {
            bool hasSmite = false;
            foreach (SpellDataInst spell in hero.Spellbook.Spells)
            {
                if (spell.Slot.HasFlag(SpellSlot.Summoner1 | SpellSlot.Summoner2))
                {
                    if (spell.Name.ToLower().Contains("smite"))
                    {
                        hasSmite = true;
                        break;
                    }
                }
            }
            return IsActive() &&
                    GankDetector.GetMenuItem("SAssembliesDetectorsGankShowJungler").GetValue<bool>() &&
                    hero.IsVisible && !hero.IsDead &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, hero.ServerPosition) >
                    GankDetector.GetMenuItem("SAssembliesDetectorsGankTrackRangeMin").GetValue<Slider>().Value &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, hero.ServerPosition) <
                    GankDetector.GetMenuItem("SAssembliesDetectorsGankTrackRangeMax").GetValue<Slider>().Value &&
                    hasSmite;
        }

        private void ChatAndPing(KeyValuePair<Obj_AI_Hero, InternalGankDetector> enemy)
        {
            Obj_AI_Hero hero = enemy.Key;
            var pingType = PingCategory.Normal;
            var t = GankDetector.GetMenuItem("SAssembliesDetectorsGankPingType").GetValue<StringList>();
            pingType = (PingCategory)t.SelectedIndex + 1;
            Vector3 pos = hero.ServerPosition;
            GamePacket gPacketT;
            for (int i = 0;
                i < GankDetector.GetMenuItem("SAssembliesDetectorsGankPingTimes").GetValue<Slider>().Value;
                i++)
            {
                if (GankDetector.GetMenuItem("SAssembliesDetectorsGankLocalPing").GetValue<bool>())
                {
                    Game.ShowPing(pingType, pos, true);
                }
                else if (!GankDetector.GetMenuItem("SAssembliesDetectorsGankLocalPing").GetValue<bool>() &&
                         Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                             .GetValue<bool>())
                {
                    Game.SendPing(pingType, pos);
                }
            }

            if (GankDetector.GetMenuItem("SAssembliesDetectorsGankChat").GetValue<bool>() &&
                Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive").GetValue<bool>())
            {
                Game.Say(Language.GetString("DETECTORS_GANK_TEXT") + ": {0}", hero.ChampionName);
            }
            if (GankDetector.GetMenuItem("SAssembliesDetectorsGankVoice").GetValue<bool>())
            {
                Speech.Speak(Language.GetString("DETECTORS_GANK_TEXT") + ": " + hero.ChampionName);
            }
            if (GankDetector.GetMenuItem("SAssembliesDetectorsGankNotification").GetValue<bool>())
            {
                Common.ShowNotification(Language.GetString("DETECTORS_GANK_TEXT") + ": " + hero.ChampionName, System.Drawing.Color.Red, 3);
            }                  
        }

        private void HandleGank(KeyValuePair<Obj_AI_Hero, InternalGankDetector> enemy)
        {
            Obj_AI_Hero hero = enemy.Key;
            if (enemy.Value.Time.InvisibleTime > GankDetector.GetMenuItem("SAssembliesDetectorsGankCooldown").GetValue<Slider>().Value)
            {
                if (!enemy.Value.Time.CalledInvisible && hero.IsValid && !hero.IsDead && hero.IsVisible &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, hero.ServerPosition) >
                    GankDetector.GetMenuItem("SAssembliesDetectorsGankTrackRangeMin").GetValue<Slider>().Value &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, hero.ServerPosition) <
                    GankDetector.GetMenuItem("SAssembliesDetectorsGankTrackRangeMax").GetValue<Slider>().Value)
                {
                    ChatAndPing(enemy);
                    enemy.Value.Time.CalledInvisible = true;
                    enemy.Value.Time.CalledTime = (int)Game.Time;
                    return;
                }
                if (!enemy.Value.Time.CalledVisible && hero.IsValid && !hero.IsDead &&
                enemy.Key.GetWaypoints().Last().Distance(ObjectManager.Player.ServerPosition) >
                GankDetector.GetMenuItem("SAssembliesDetectorsGankTrackRangeMin").GetValue<Slider>().Value &&
                enemy.Key.GetWaypoints().Last().Distance(ObjectManager.Player.ServerPosition) <
                GankDetector.GetMenuItem("SAssembliesDetectorsGankTrackRangeMax").GetValue<Slider>().Value)
                {
                    ChatAndPing(enemy);
                    enemy.Value.Time.CalledVisible = true;
                    enemy.Value.Time.CalledTime = (int)Game.Time;
                    return;
                }
            }
        }

        private void UpdateTime(KeyValuePair<Obj_AI_Hero, InternalGankDetector> enemy)
        {
            Obj_AI_Hero hero = enemy.Key;
            if (!hero.IsValid)
                return;
            if (hero.IsVisible)
            {
                HandleGank(enemy);
                Enemies[hero].Time.InvisibleTime = 0;
                Enemies[hero].Time.VisibleTime = (int)Game.Time;
                enemy.Value.Time.CalledInvisible = false;
            }
            else
            {
                if (Enemies[hero].Time.VisibleTime != 0)
                {
                    Enemies[hero].Time.InvisibleTime = (int)(Game.Time - Enemies[hero].Time.VisibleTime);
                }
                else
                {
                    Enemies[hero].Time.InvisibleTime = 0;
                }
                enemy.Value.Time.CalledVisible = false;
            }
        }

        public class InternalGankDetector
        {
            public Time Time = new Time();
            public Render.Text Text;
            public Render.Line Line;

            public InternalGankDetector(Render.Text text, Render.Line line)
            {
                Text = text;
                Line = line;
            }
        }

        public class Time
        {
            public bool CalledInvisible;
            public bool CalledVisible;
            public int CalledTime;
            public int InvisibleTime;
            public int VisibleTime;
        }
    }
}
