using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Champions
{
    internal class TahmKench
    {
        public static Menu.MenuItemSettings TahmKenchChampion = new Menu.MenuItemSettings(typeof(TahmKench));

        private static Orbwalking.Orbwalker orbwalker;

        public TahmKench()
        {
            Game.OnUpdate += Game_OnGameUpdate;
        }

        ~TahmKench()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
        }

        public static bool IsActive()
        {
#if MISCS
            return Champion.Champions.GetActive() && TahmKenchChampion.GetActive();
#else
            return TahmKenchChampion.GetActive() && ObjectManager.Player.ChampionName.Equals("TahmKench");
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            if (!ObjectManager.Player.ChampionName.Equals("TahmKench"))
            {
                return null;
            }
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesChampionsTahmKench");
            if (newMenu == null)
            {
                TahmKenchChampion.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_TAHMKENCH_MAIN"), "SAssembliesChampionsTahmKench"));
                var orbwalkerMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_ORBWALKER"), "SAssembliesChampionsTahmKenchOrbwalker"));
                orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
                var comboMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_COMBO"), "SAssembliesChampionsTahmKenchCombo"));
                comboMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchComboQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                comboMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchComboW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                comboMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchComboE", Language.GetString("CHAMPIONS_CHAMPION_E")).SetValue(true));
                var harassMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_HARASS"), "SAssembliesChampionsTahmKenchHarass"));
                harassMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchHarassQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                harassMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchHarassW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                var clearMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM"), "SAssembliesChampionsTahmKenchFarm"));
                var lasthitMenu = clearMenu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM_LASTHIT"), "SAssembliesChampionsTahmKenchFarmLasthit"));
                lasthitMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLasthitQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                var jungleMenu = clearMenu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM_JUNGLE"), "SAssembliesChampionsTahmKenchFarmJungle"));
                jungleMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmJungleQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                jungleMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmJungleW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                var laneMenu = clearMenu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM_LANE"), "SAssembliesChampionsTahmKenchFarmLane"));
                laneMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLaneQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                laneMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLaneW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                var fleeMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FLEE"), "SAssembliesChampionsTahmKenchFlee"));
                fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeWithAlly", Language.GetString("CHAMPIONS_TAHMKENCH_FLEE_ALLY")).SetValue(true));
                fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeWithAllyRange", Language.GetString("CHAMPIONS_TAHMKENCH_FLEE_ALLY_RANGE")).SetValue(new Slider(100, 100, 500)));
                fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeKey", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind('Z', KeyBindType.Press)));
                var qMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_Q"), "SAssembliesChampionsTahmKenchQ"));
                qMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchQInterrupt", Language.GetString("CHAMPIONS_CHAMPION_INTERRUPT")).SetValue(true));
                qMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchQKillsteal", Language.GetString("CHAMPIONS_CHAMPION_KILLSTEAL")).SetValue(true));
                qMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchQDraw", Language.GetString("CHAMPIONS_CHAMPION_DRAW")).SetValue(true));
                var wMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_W"), "SAssembliesChampionsTahmKenchW"));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWInterrupt", Language.GetString("CHAMPIONS_CHAMPION_INTERRUPT")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWKillsteal", Language.GetString("CHAMPIONS_CHAMPION_KILLSTEAL")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWDraw", Language.GetString("CHAMPIONS_CHAMPION_DRAW")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWDrawMax", Language.GetString("CHAMPIONS_TAHMKENCH_W_MAXMOVE")).SetValue(true));
                var eMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_E"), "SAssembliesChampionsTahmKenchE"));
                eMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchEShield", Language.GetString("CHAMPIONS_TAHMKENCH_E_SHIELD")).SetValue(true));
                eMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchEShieldPercent", Language.GetString("CHAMPIONS_TAHMKENCH_E_SHIELD_PERCENT")).SetValue(new Slider(5, 1, 99)));
                var rMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_R"), "SAssembliesChampionsTahmKenchR"));
                rMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchR", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind('U', KeyBindType.Press)));
                var itemsMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_ITEMS"), "SAssembliesChampionsTahmKenchItems"));
                var trollMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_TROLL"), "SAssembliesChampionsTahmKenchTroll"));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollW", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind('I', KeyBindType.Press)));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollWToEnemy", Language.GetString("GLOBAL_KEY")).SetValue(true));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollWToTower", Language.GetString("GLOBAL_KEY")).SetValue(true));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollWR", Language.GetString("GLOBAL_KEY")).SetValue(true));
                TahmKenchChampion.CreateActiveMenuItem("SAssembliesChampionsTahmKenchActive", () => new TahmKench());
            }
            return TahmKenchChampion;
        }

        static class CustomSpell
        {
            public static Spell Q = new Spell(SpellSlot.Q, 800);
            public static Spell W = new Spell(SpellSlot.W, 250);
            public static Spell W2 = new Spell(SpellSlot.W, 900);
            public static Spell E = new Spell(SpellSlot.E);
            public static Spell R = new Spell(SpellSlot.R, 4000);

            static CustomSpell()
            {
                Q.SetSkillshot(0.1f, 75, 2000, true, SkillshotType.SkillshotLine);
                W2.SetSkillshot(0.1f, 75, 900, true, SkillshotType.SkillshotLine);
            }

            public static void CastSpell(Spell spell, Obj_AI_Base target, HitChance hitchance)
            {
                if (target.IsValidTarget(spell.Range) && spell.GetPrediction(target).Hitchance >= hitchance)
                    spell.Cast(target);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

        }
    }
}