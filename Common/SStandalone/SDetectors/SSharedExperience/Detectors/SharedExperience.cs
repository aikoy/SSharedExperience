using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

//TODO: Add invisible enemy count

namespace SAssemblies.Detectors
{
    using System.Drawing;

    using Color = SharpDX.Color;

    class SharedExperience
    {
        public static Menu.MenuItemSettings SharedExperienceDetector = new Menu.MenuItemSettings(typeof(SharedExperience));

        private float experienceMelee = 58.88f;
        private float experienceRanged = 29.44f;
        private float experienceSiege = 92f;
        private float experienceSuper = 97f;

        private Dictionary<Obj_AI_Hero, InternalSharedExperience> enemies = new Dictionary<Obj_AI_Hero, InternalSharedExperience>();

        public SharedExperience()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsEnemy)
                {
                    enemies.Add(enemy, new InternalSharedExperience(enemy));
                }
            }
            Game.OnUpdate += Game_OnUpdate;
        }

        ~SharedExperience()
        {
            Game.OnUpdate -= Game_OnUpdate;
        }

        public bool IsActive()
        {
#if DETECTORS
            return Detector.Detectors.GetActive() && SharedExperienceDetector.GetActive();
#else
            return SharedExperienceDetector.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesDetectorsSharedExperience");
            if (newMenu == null)
            {
                SharedExperienceDetector.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("DETECTORS_SHAREDEXPERIENCE_MAIN"), "SAssembliesDetectorsSharedExperience"));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceResetTime", Language.GetString("DETECTORS_SHAREDEXPERIENCE_RESETTIME")).SetValue(new Slider(60, 120, 1)));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceTrackRange", Language.GetString("DETECTORS_SHAREDEXPERIENCE_TRACKRANGE")).SetValue(new Slider(2000, 10000, 100)));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperiencePingTimes", Language.GetString("GLOBAL_PING_TIMES")).SetValue(new Slider(0, 5, 0)));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperiencePingType", Language.GetString("GLOBAL_PING_TYPE")).SetValue(new StringList(new[]
                {
                    Language.GetString("GLOBAL_PING_TYPE_NORMAL"),
                    Language.GetString("GLOBAL_PING_TYPE_DANGER"),
                    Language.GetString("GLOBAL_PING_TYPE_ENEMYMISSING"),
                    Language.GetString("GLOBAL_PING_TYPE_ONMYWAY"),
                    Language.GetString("GLOBAL_PING_TYPE_FALLBACK"),
                    Language.GetString("GLOBAL_PING_ASSISTME")
                })));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceLocalPing", Language.GetString("GLOBAL_PING_LOCAL")).SetValue(true));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceChat", Language.GetString("GLOBAL_CHAT")).SetValue(false));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceNotification", Language.GetString("GLOBAL_NOTIFICATION")).SetValue(false));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceVoice", Language.GetString("GLOBAL_VOICE")).SetValue(false));
                SharedExperienceDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsSharedExperienceDrawing", Language.GetString("GLOBAL_DRAWING")).SetValue(false));
                SharedExperienceDetector.CreateActiveMenuItem("SAssembliesDetectorsSharedExperienceActive", () => new SharedExperience());
            }
            return SharedExperienceDetector;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.Key.Experience != enemy.Value.Experience)
                {
                    enemy.Value.LastExpTime = Game.Time;
                    double oldExp = Math.Round(enemy.Value.Experience, 2);
                    double newExp = Math.Round(enemy.Key.Experience, 2);
                    if (ObjectManager.Get<Obj_AI_Minion>().Any(x => x.Distance(enemy.Key) < 1600 && x.IsDead))
                    {
                        if (oldExp + experienceMelee != newExp &&
                            oldExp + experienceRanged != newExp &&
                            oldExp + experienceSiege != newExp &&
                            oldExp + experienceSuper != newExp)
                        {
                            int enemyCount = CalcExpChamps(enemy);
                            if (enemyCount > 1)
                            {
                                int visibleEnemyCount = Utility.CountEnemiesInRange(enemy.Key, 1600);
                                if (visibleEnemyCount != enemyCount)
                                {
                                    enemy.Value.Text.Color = Color.Red;
                                }
                                else
                                {
                                    enemy.Value.Text.Color = Color.Azure;
                                }
                                int missingEnemies = (enemyCount - visibleEnemyCount);
                                if (enemy.Value.LastCallTime + 60 < Game.Time && missingEnemies > 0)
                                {
                                    enemy.Value.LastCallTime = Game.Time;
                                    Obj_AI_Hero hero = enemy.Key;
                                    var t = SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperiencePingType").GetValue<StringList>();
                                    var pingType = (PingCategory)t.SelectedIndex + 1;
                                    Vector3 pos = hero.ServerPosition;
                                    for (int i = 0;
                                        i < SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperiencePingTimes").GetValue<Slider>().Value;
                                        i++)
                                    {
                                        if (SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceLocalPing").GetValue<bool>())
                                        {
                                            Game.ShowPing(pingType, pos, true);
                                        }
                                        else if (!SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceLocalPing").GetValue<bool>() &&
                                                 Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                                                     .GetValue<bool>())
                                        {
                                            Game.SendPing(pingType, pos);
                                        }
                                    }

                                    if (SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceChat").GetValue<bool>() &&
                                        Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive").GetValue<bool>())
                                    {
                                        Game.Say(missingEnemies + " " + Language.GetString("DETECTORS_SHAREDEXPERIENCE_TEXT") + " " + enemy.Key.ChampionName);
                                    }
                                    if (SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceVoice").GetValue<bool>())
                                    {
                                        Speech.Speak(missingEnemies + " " + Language.GetString("DETECTORS_SHAREDEXPERIENCE_TEXT") + " " + enemy.Key.ChampionName);
                                    }
                                    if (SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceNotification").GetValue<bool>())
                                    {
                                        Common.ShowNotification(missingEnemies + " " + Language.GetString("DETECTORS_SHAREDEXPERIENCE_TEXT") + " " + enemy.Key.ChampionName, 
                                            System.Drawing.Color.Red, 3);
                                    }
                                }
                                if (SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceDrawing").GetValue<bool>())
                                {
                                    enemy.Value.Text.text = "Enemies: " + visibleEnemyCount + "(+" + missingEnemies + ")";
                                    enemy.Value.Circle.Visible = true;
                                    enemy.Value.Text.Visible = true;
                                }
                            }
                        }
                        else
                        {
                            enemy.Value.Text.Visible = false;
                            enemy.Value.Circle.Visible = false;
                        }
                    }
                    enemies[enemy.Key].Experience = enemy.Key.Experience;
                }
                if (enemy.Key.IsVisible)
                {
                    enemy.Value.VisibleTime = Game.Time;
                }
                if (enemy.Value.VisibleTime + SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceResetTime").GetValue<Slider>().Value < Game.Time ||
                    enemy.Value.LastExpTime + SharedExperienceDetector.GetMenuItem("SAssembliesDetectorsSharedExperienceResetTime").GetValue<Slider>().Value < Game.Time ||
                    enemy.Key.IsDead || !enemy.Key.IsVisible)
                {
                    enemy.Value.Text.Visible = false;
                    enemy.Value.Circle.Visible = false;
                }
            }
        }

        private int CalcExpChamps(KeyValuePair<Obj_AI_Hero, InternalSharedExperience> enemy)
        {
            double oldExp = Math.Round(enemy.Value.Experience, 2);
            double newExp = Math.Round(enemy.Key.Experience, 2);
            double neededExp = Math.Round(newExp - oldExp, 0);
            if (Math.Round(experienceMelee * 0.652, 0) == neededExp ||
                Math.Round(experienceRanged * 0.652, 0) == neededExp ||
                Math.Round(experienceSiege * 0.652, 0) == neededExp ||
                Math.Round(experienceSuper * 0.652, 0) == neededExp)
            {
                return 2;
            }
            if (Math.Round(experienceMelee * 0.4346, 0) == neededExp ||
                Math.Round(experienceRanged * 0.4346, 0) == neededExp ||
                Math.Round(experienceSiege * 0.4346, 0) == neededExp ||
                Math.Round(experienceSuper * 0.4346, 0) == neededExp)
            {
                return 3;
            }
            if (Math.Round(experienceMelee * 0.326, 0) == neededExp ||
                Math.Round(experienceRanged * 0.326, 0) == neededExp ||
                Math.Round(experienceSiege * 0.326, 0) == neededExp ||
                Math.Round(experienceSuper * 0.326, 0) == neededExp)
            {
                return 4;
            }
            if (Math.Round(experienceMelee * 0.2608, 0) == neededExp ||
                Math.Round(experienceRanged * 0.2608, 0) == neededExp ||
                Math.Round(experienceSiege * 0.2608, 0) == neededExp ||
                Math.Round(experienceSuper * 0.2608, 0) == neededExp)
            {
                return 5;
            }
            return 1;
        }

        public class InternalSharedExperience
        {
            public float Experience;

            public Render.Text Text;

            public Render.Circle Circle;

            public float VisibleTime;

            public float LastExpTime;

            public float LastCallTime;

            public InternalSharedExperience(Obj_AI_Hero hero)
            {
                Experience = hero.Experience;
                VisibleTime = Game.Time;
                LastExpTime = Game.Time;
                LastCallTime = Game.Time;

                Text = new Render.Text("", hero, new Vector2(10, -30), 20, Color.Azure);
                Text.Visible = false;
                Text.Add();

                Circle = new Render.Circle(hero, 1600, System.Drawing.Color.OrangeRed);
                Circle.Visible = false;
                Circle.Add();
            }
        }
    }
}
