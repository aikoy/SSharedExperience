using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Champions
{
    using System.Linq;
    using System.Runtime.CompilerServices;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class TahmKench
    {
        public static Menu.MenuItemSettings TahmKenchChampion = new Menu.MenuItemSettings(typeof(TahmKench));

        private static Orbwalking.Orbwalker orbwalker;

        private SwallowedUnit swallowedUnit;
        private string tahmPassive = "TahmKenchPDebuffCounter";

        private String tahmEatingPassive = "tahmkenchwdevoured";
        private String tahmEatPassive = "TahmKenchWHasDevouredTarget";

        private Vector3 lastPosBeforeSwallowing;

        //W Blacklist

        public TahmKench()
        {
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Obj_AI_Hero.OnBuffAdd += Obj_AI_Hero_OnBuffAdd;
            Obj_AI_Hero.OnBuffRemove += Obj_AI_Hero_OnBuffRemove;
            Obj_AI_Hero.OnLevelUp += Obj_AI_Hero_OnLevelUp;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
        }

        ~TahmKench()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            Drawing.OnEndScene -= Drawing_OnEndScene;
            Obj_AI_Hero.OnBuffAdd -= Obj_AI_Hero_OnBuffAdd;
            Obj_AI_Hero.OnBuffRemove -= Obj_AI_Hero_OnBuffRemove;
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
                comboMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchComboWMax", Language.GetString("CHAMPIONS_TAHMKENCH_COMBO_WMAX")).SetValue(new Slider(1, 0, 2)));
                var harassMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_HARASS"), "SAssembliesChampionsTahmKenchHarass"));
                harassMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchHarassQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                harassMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchHarassW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                harassMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchHarassMinMana", Language.GetString("CHAMPIONS_CHAMPION_MANAPERCENT")).SetValue(new Slider(50, 0, 100)));
                var clearMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM"), "SAssembliesChampionsTahmKenchFarm"));
                var lasthitMenu = clearMenu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM_LASTHIT"), "SAssembliesChampionsTahmKenchFarmLasthit"));
                lasthitMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLasthitQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                lasthitMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLasthitMinMana", Language.GetString("CHAMPIONS_CHAMPION_MANAPERCENT")).SetValue(new Slider(50, 0, 100)));
                var jungleMenu = clearMenu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM_JUNGLE"), "SAssembliesChampionsTahmKenchFarmJungle"));
                jungleMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmJungleQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                jungleMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmJungleW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                jungleMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmJungleMinMana", Language.GetString("CHAMPIONS_CHAMPION_MANAPERCENT")).SetValue(new Slider(50, 0, 100)));
                var laneMenu = clearMenu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FARM_LANE"), "SAssembliesChampionsTahmKenchFarmLane"));
                laneMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLaneQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                laneMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLaneW", Language.GetString("CHAMPIONS_CHAMPION_W")).SetValue(true));
                laneMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFarmLaneMinMana", Language.GetString("CHAMPIONS_CHAMPION_MANAPERCENT")).SetValue(new Slider(50, 0, 100)));
                //var fleeMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_FLEE"), "SAssembliesChampionsTahmKenchFlee"));
                //fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeQ", Language.GetString("CHAMPIONS_CHAMPION_Q")).SetValue(true));
                //fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeWithAlly", Language.GetString("CHAMPIONS_TAHMKENCH_FLEE_ALLY")).SetValue(true));
                //fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeWithAllyRange", Language.GetString("CHAMPIONS_TAHMKENCH_FLEE_ALLY_RANGE")).SetValue(new Slider(100, 100, 500)));
                //fleeMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchFleeKey", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind('Z', KeyBindType.Press)));
                var qMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_Q"), "SAssembliesChampionsTahmKenchQ"));
                qMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchQInterrupt", Language.GetString("CHAMPIONS_CHAMPION_INTERRUPT")).SetValue(true));
                qMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchQKillsteal", Language.GetString("CHAMPIONS_CHAMPION_KILLSTEAL")).SetValue(true));
                qMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchQDraw", Language.GetString("CHAMPIONS_CHAMPION_DRAW")).SetValue(true));
                var wMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_W"), "SAssembliesChampionsTahmKenchW"));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWInterrupt", Language.GetString("CHAMPIONS_CHAMPION_INTERRUPT")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWKillsteal", Language.GetString("CHAMPIONS_CHAMPION_KILLSTEAL")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWDraw", Language.GetString("CHAMPIONS_CHAMPION_DRAW")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWDrawMax", Language.GetString("CHAMPIONS_TAHMKENCH_W_MAXMOVE")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWAutoMoveToAlly", Language.GetString("CHAMPIONS_TAHMKENCH_W_AUTOMOVE")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWAutoShieldAlly", Language.GetString("CHAMPIONS_TAHMKENCH_W_AUTOSHIELD")).SetValue(true));
                wMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchWAutoShieldAllyPercent", Language.GetString("CHAMPIONS_TAHMKENCH_W_SHIELD_PERCENT")).SetValue(new Slider(20, 1, 99)));
                var eMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_E"), "SAssembliesChampionsTahmKenchE"));
                eMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchEShield", Language.GetString("CHAMPIONS_TAHMKENCH_E_SHIELD")).SetValue(true));
                eMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchEShieldPercent", Language.GetString("CHAMPIONS_TAHMKENCH_E_SHIELD_PERCENT")).SetValue(new Slider(20, 1, 99)));
                var rMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_R"), "SAssembliesChampionsTahmKenchR"));
                //rMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchR", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind('U', KeyBindType.Press)));
                rMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchRDraw", Language.GetString("CHAMPIONS_CHAMPION_DRAW")).SetValue(true));
                //var itemsMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_ITEMS"), "SAssembliesChampionsTahmKenchItems"));
                var trollMenu = TahmKenchChampion.Menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("CHAMPIONS_CHAMPION_TROLL"), "SAssembliesChampionsTahmKenchTroll"));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollW", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind('I', KeyBindType.Press)));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollWToEnemyHero", Language.GetString("CHAMPIONS_CHAMPION_TROLL_ENEMY_HERO")).SetValue(true));
                trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollWToEnemyTurret", Language.GetString("CHAMPIONS_CHAMPION_TROLL_ENEMY_TURRET")).SetValue(true));
                //trollMenu.AddItem(new MenuItem("SAssembliesChampionsTahmKenchTrollWR", Language.GetString("GLOBAL_KEY")).SetValue(true));
                TahmKenchChampion.CreateActiveMenuItem("SAssembliesChampionsTahmKenchActive", () => new TahmKench());
            }
            return TahmKenchChampion;
        }

        static class CustomSpell
        {
            public static Spell Q = new Spell(SpellSlot.Q, 800, TargetSelector.DamageType.Magical);
            public static Spell W = new Spell(SpellSlot.W, 250);
            public static Spell W2 = new Spell(SpellSlot.W, 900, TargetSelector.DamageType.Magical);
            public static Spell E = new Spell(SpellSlot.E);
            public static Spell R;

            static CustomSpell()
            {
                Q.SetSkillshot(0.1f, 75, 2000, true, SkillshotType.SkillshotLine);
                W2.SetSkillshot(0.1f, 75, 900, true, SkillshotType.SkillshotLine);

                switch (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level)
                {
                    case 2:
                        R = new Spell(SpellSlot.R, 5500);
                        break;

                    case 3:
                        R = new Spell(SpellSlot.R, 6000);
                        break;

                    default:
                        R = new Spell(SpellSlot.R, 5000);
                        break;
                }
            }

            public static void CastSpell(Spell spell, Obj_AI_Base target, HitChance hitchance)
            {
                if (target.IsValidTarget(spell.Range) && spell.GetPrediction(target).Hitchance >= hitchance)
                {
                    spell.Cast(target);
                }
            }
        }

        public enum SwallowedUnit
        {
            None,
            Ally,
            Enemy,
            Minion
        }

        private void Obj_AI_Hero_OnLevelUp(Obj_AI_Base sender, EventArgs args)
        {
            switch (CustomSpell.R.Level)
            {
                case 2:
                    CustomSpell.R.Range = 5500;
                    break;

                case 3:
                    CustomSpell.R.Range = 6000;
                    break;
            }
        }

        private void Obj_AI_Hero_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (args.Buff.Name.Equals(tahmEatingPassive))
            {
                lastPosBeforeSwallowing = ObjectManager.Player.Position;
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    if (hero.IsAlly)
                    {
                        swallowedUnit = SwallowedUnit.Ally;
                    }
                    else
                    {
                        swallowedUnit = SwallowedUnit.Enemy;
                    }
                    return;
                }
                var minion = sender as Obj_AI_Minion;
                if (minion != null)
                {
                    swallowedUnit = SwallowedUnit.Minion;
                }
            }
            else if (args.Buff.Name.Equals(tahmEatPassive.ToLower()) && !swallowedUnit.HasFlag(SwallowedUnit.Enemy | SwallowedUnit.Ally))
            {
                lastPosBeforeSwallowing = ObjectManager.Player.Position;
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    if (hero.IsAlly)
                    {
                        swallowedUnit = SwallowedUnit.Ally;
                    }
                }
            }
        }

        private void Obj_AI_Hero_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Buff.Name.Equals(tahmEatPassive.ToLower()))
            {
                swallowedUnit = SwallowedUnit.None;
                lastPosBeforeSwallowing = Vector3.Zero;
            }
        }

        private void Interrupter2_OnInterruptableTarget(
            Obj_AI_Hero unit,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            switch (unit.GetBuffCount(tahmPassive))
            {
                case 1:
                    if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchQ")
                            .Item("SAssembliesChampionsTahmKenchQInterrupt")
                            .GetValue<bool>()
                        && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                               .Item("SAssembliesChampionsTahmKenchWInterrupt")
                               .GetValue<bool>())
                    {
                        if (Orbwalking.InAutoAttackRange(unit) && CustomSpell.Q.IsReady() && (CustomSpell.W.IsReady()
                            && swallowedUnit == SwallowedUnit.None))
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
                            CustomSpell.Q.Cast(unit);
                            CustomSpell.W.CastOnUnit(unit);
                        }
                    }
                    break;

                case 2:
                    if ((CustomSpell.Q.IsReady() && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchQ")
                            .Item("SAssembliesChampionsTahmKenchQInterrupt")
                            .GetValue<bool>()) || (CustomSpell.W.IsReady() && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                               .Item("SAssembliesChampionsTahmKenchWInterrupt")
                               .GetValue<bool>() && swallowedUnit == SwallowedUnit.None))
                    {
                        if (Orbwalking.InAutoAttackRange(unit))
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
                            if (CustomSpell.Q.IsReady())
                            {
                                Utility.DelayAction.Add(100, () => CustomSpell.Q.Cast(unit));
                            }
                            else if (CustomSpell.W.IsReady())
                            {
                                Utility.DelayAction.Add(100, () => CustomSpell.W.CastOnUnit(unit));
                            }
                        }
                        else if (CustomSpell.Q.IsReady() && CustomSpell.W.IsReady())
                        {
                            CustomSpell.Q.Cast(unit);
                            Utility.DelayAction.Add(100, () => CustomSpell.W.CastOnUnit(unit));
                        }
                    }
                    break;

                case 3:
                    if ((CustomSpell.Q.IsReady() && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchQ")
                            .Item("SAssembliesChampionsTahmKenchQInterrupt")
                            .GetValue<bool>()) || (CustomSpell.W.IsReady() && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                               .Item("SAssembliesChampionsTahmKenchWInterrupt")
                               .GetValue<bool>() && swallowedUnit == SwallowedUnit.None))
                    {
                        if (CustomSpell.Q.IsReady())
                        {
                            CustomSpell.Q.Cast(unit);
                        }
                        else if (CustomSpell.W.IsReady())
                        {
                            CustomSpell.W.CastOnUnit(unit);
                        }
                    }
                    break;
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || ObjectManager.Player.IsDead)
                return;

            this.Killsteal();
            this.Shield();
            SaveAlly();
            TrollMode();

            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;

                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (!IsActive())
                return;

            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchQ")
                    .Item("SAssembliesChampionsTahmKenchQDraw")
                    .GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, CustomSpell.Q.Range, Color.Red);
            }
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                    .Item("SAssembliesChampionsTahmKenchWDraw")
                    .GetValue<bool>())
            {
                if (swallowedUnit != SwallowedUnit.Minion)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, CustomSpell.W.Range, Color.BlueViolet);
                }
                else
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, CustomSpell.W2.Range, Color.BlueViolet);
                }
            }
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchR")
                    .Item("SAssembliesChampionsTahmKenchRDraw")
                    .GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, CustomSpell.R.Range, Color.CadetBlue);
            }

            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                    .Item("SAssembliesChampionsTahmKenchWDrawMax")
                    .GetValue<bool>())
            {
                if (swallowedUnit == SwallowedUnit.Enemy)
                {
                    BuffInstance buff = ObjectManager.Player.GetBuff(tahmEatPassive.ToLower());
                    if (buff != null)
                    {
                        float time = buff.EndTime - buff.StartTime;
                        float xPos = lastPosBeforeSwallowing.X + ((time) * ObjectManager.Player.MoveSpeed);
                        float radius = Math.Abs(lastPosBeforeSwallowing.X - xPos);
                        Console.WriteLine(radius);
                        Render.Circle.DrawCircle(lastPosBeforeSwallowing, radius, Color.Aqua);
                    }
                }
            }
        }

        private void Combo()
        {
            orbwalker.SetOrbwalkingPoint(Vector3.Zero);

            var target = TargetSelector.GetTarget(CustomSpell.Q.Range, TargetSelector.DamageType.Magical);
            var closestMinion = MinionManager.GetMinions(250, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();

            if (target != null)
            {
                var buffCount = target.GetBuffCount(tahmPassive);
                switch (buffCount)
                {
                    case 3:
                        if (CustomSpell.W.IsReady()
                            && swallowedUnit == SwallowedUnit.None
                            && target.Distance(ObjectManager.Player) <= CustomSpell.W.Range
                            && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchCombo")
                                   .Item("SAssembliesChampionsTahmKenchComboW")
                                   .GetValue<bool>())
                        {
                            CustomSpell.W.CastOnUnit(target);
                        }
                        else if (CustomSpell.Q.IsReady() 
                            && target.Distance(ObjectManager.Player) <= CustomSpell.Q.Range
                                 && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchCombo")
                                        .Item("SAssembliesChampionsTahmKenchComboQ")
                                        .GetValue<bool>())
                        {
                            CustomSpell.Q.Cast(target);
                        }
                        break;
                    default:
                        if (CustomSpell.W.IsReady() && !Orbwalking.InAutoAttackRange(target)
                            && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchCombo")
                                .Item("SAssembliesChampionsTahmKenchComboW")
                                .GetValue<bool>())
                        {
                            if (buffCount >= TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchCombo")
                                    .Item("SAssembliesChampionsTahmKenchComboWMax")
                                    .GetValue<Slider>()
                                    .Value)
                            {
                                break;
                            }
                            if (swallowedUnit == SwallowedUnit.None && closestMinion != null)
                            {
                                CustomSpell.W.CastOnUnit(closestMinion);
                            }
                            else if (swallowedUnit == SwallowedUnit.Minion)
                            {
                                CustomSpell.W2.CastIfHitchanceEquals(target, HitChance.High);
                            }
                        }
                        if (CustomSpell.Q.IsReady()
                            && target.Distance(ObjectManager.Player) <= CustomSpell.Q.Range
                                 && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchCombo")
                                        .Item("SAssembliesChampionsTahmKenchComboQ")
                                        .GetValue<bool>())
                        {
                            CustomSpell.Q.Cast(target);
                        }
                        break;
                }
            }

            if (swallowedUnit == SwallowedUnit.Enemy
                    && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                        .Item("SAssembliesChampionsTahmKenchWAutoMoveToAlly")
                        .GetValue<bool>())
            {
                var hero = HeroManager.Allies
                    .OrderBy(x => ObjectManager.Player.Distance(x.Position))
                    .FirstOrDefault(x => !x.IsMe && ObjectManager.Player.Distance(x.Position) < 2000);
                var turret =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .OrderBy(x => ObjectManager.Player.Distance(x.Position))
                        .FirstOrDefault(x => ObjectManager.Player.Distance(x.Position) < 2000
                        && x.IsAlly);
                if (hero != null && turret != null)
                {
                    if (ObjectManager.Player.Distance(hero) < ObjectManager.Player.Distance(turret))
                    {
                        orbwalker.SetOrbwalkingPoint(hero.ServerPosition);
                    }
                    else if (ObjectManager.Player.Distance(hero) > ObjectManager.Player.Distance(turret))
                    {
                        orbwalker.SetOrbwalkingPoint(turret.ServerPosition);
                    }
                }
                else if (hero != null)
                {
                    orbwalker.SetOrbwalkingPoint(hero.ServerPosition);
                }
                else if (turret != null)
                {
                    orbwalker.SetOrbwalkingPoint(turret.ServerPosition);
                }
            }
        }

        private void Harass()
        {
            if (ObjectManager.Player.ManaPercent
                < TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchHarass")
                      .Item("SAssembliesChampionsTahmKenchHarassMinMana")
                      .GetValue<Slider>()
                      .Value)
            {
                return; 
            }

            if (CustomSpell.Q.IsReady() &&
                TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchHarass")
                    .Item("SAssembliesChampionsTahmKenchHarassQ")
                    .GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(CustomSpell.Q.Range, TargetSelector.DamageType.Magical);

                if (target != null)
                {
                    CustomSpell.CastSpell(CustomSpell.Q, target, HitChance.High);
                }
            }

            var closestMinion = MinionManager.GetMinions(250, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            if (CustomSpell.W.IsReady()
                && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchHarass")
                                .Item("SAssembliesChampionsTahmKenchHarassW")
                                .GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(CustomSpell.W2.Range, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    if (swallowedUnit == SwallowedUnit.None && closestMinion != null)
                    {
                        CustomSpell.W.CastOnUnit(closestMinion);
                    }
                    else if (swallowedUnit == SwallowedUnit.Minion)
                    {
                        CustomSpell.W2.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
            }

        }

        private void LastHit()
        {
            if (ObjectManager.Player.ManaPercent
                < TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                      .SubMenu("SAssembliesChampionsTahmKenchFarmLasthit")
                      .Item("SAssembliesChampionsTahmKenchFarmLasthitMinMana")
                      .GetValue<Slider>()
                      .Value)
            {
                return;
            }
            var minion = MinionManager.GetMinions(CustomSpell.Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .FirstOrDefault(target => ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health);

            if (minion != null)
            {
                if (CustomSpell.Q.IsReady() && minion.Distance(ObjectManager.Player) >= ObjectManager.Player.AttackRange &&
                TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                    .SubMenu("SAssembliesChampionsTahmKenchFarmLasthit")
                    .Item("SAssembliesChampionsTahmKenchFarmLasthitQ")
                    .GetValue<bool>())
                {
                    CustomSpell.CastSpell(CustomSpell.Q, minion, HitChance.High);
                }
            }
        }

        private void LaneClear()
        {
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                    .SubMenu("SAssembliesChampionsTahmKenchFarmLane")
                    .Item("SAssembliesChampionsTahmKenchFarmLaneQ")
                    .GetValue<bool>() && CustomSpell.Q.IsReady() &&
                    ObjectManager.Player.ManaPercent
                    >= TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                      .SubMenu("SAssembliesChampionsTahmKenchFarmLane")
                      .Item("SAssembliesChampionsTahmKenchFarmLaneMinMana")
                      .GetValue<Slider>()
                      .Value)
            {
                var minion = MinionManager.GetMinions(CustomSpell.Q.Range)
                    .FirstOrDefault();

                if (minion != null)
                {
                    if (CustomSpell.Q.IsReady())
                    {
                        CustomSpell.CastSpell(CustomSpell.Q, minion, HitChance.High);
                    }
                }
            }

            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                    .SubMenu("SAssembliesChampionsTahmKenchFarmLane")
                    .Item("SAssembliesChampionsTahmKenchFarmLaneW")
                    .GetValue<bool>() && CustomSpell.W.IsReady())
            {

                if (ObjectManager.Player.ManaPercent
                    < TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                          .SubMenu("SAssembliesChampionsTahmKenchFarmLane")
                          .Item("SAssembliesChampionsTahmKenchFarmLaneMinMana")
                          .GetValue<Slider>()
                          .Value && swallowedUnit != SwallowedUnit.Minion)
                {
                    return;
                }

                var minion = MinionManager.GetMinions(CustomSpell.W.Range).FirstOrDefault();
                var minions = MinionManager.GetMinions(CustomSpell.W2.Range);

                if (minion != null)
                {
                    if (swallowedUnit == SwallowedUnit.None && minion != null)
                    {
                        CustomSpell.W.CastOnUnit(minion);
                    }
                    else if (swallowedUnit == SwallowedUnit.Minion && minions.Count > 0)
                    {
                        CustomSpell.W2.Cast(CustomSpell.W2.GetCircularFarmLocation(minions).Position);
                    }
                }
            }
        }

        private void JungleClear()
        {
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                    .SubMenu("SAssembliesChampionsTahmKenchFarmJungle")
                    .Item("SAssembliesChampionsTahmKenchFarmJungleQ")
                    .GetValue<bool>() && CustomSpell.Q.IsReady() &&
                    ObjectManager.Player.ManaPercent
                    >= TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                      .SubMenu("SAssembliesChampionsTahmKenchFarmJungle")
                      .Item("SAssembliesChampionsTahmKenchFarmJungleMinMana")
                      .GetValue<Slider>()
                      .Value)
            {
                var minion = MinionManager.GetMinions(CustomSpell.Q.Range, MinionTypes.All, MinionTeam.Neutral)
                    .FirstOrDefault();

                if (minion != null)
                {
                    if (CustomSpell.Q.IsReady())
                    {
                        CustomSpell.CastSpell(CustomSpell.Q, minion, HitChance.High);
                    }
                }
            }

            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                    .SubMenu("SAssembliesChampionsTahmKenchFarmJungle")
                    .Item("SAssembliesChampionsTahmKenchFarmJungleW")
                    .GetValue<bool>() && CustomSpell.W.IsReady())
            {

                if (ObjectManager.Player.ManaPercent
                    < TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchFarm")
                          .SubMenu("SAssembliesChampionsTahmKenchFarmJungle")
                          .Item("SAssembliesChampionsTahmKenchFarmJungleMinMana")
                          .GetValue<Slider>()
                          .Value && swallowedUnit != SwallowedUnit.Minion)
                {
                    return;
                }

                var minion = MinionManager.GetMinions(CustomSpell.W.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault();
                var minions = MinionManager.GetMinions(CustomSpell.W2.Range, MinionTypes.All, MinionTeam.Neutral);

                if (minion != null)
                {
                    if (swallowedUnit == SwallowedUnit.None && minion != null)
                    {
                        CustomSpell.W.CastOnUnit(minion);
                    }
                    else if (swallowedUnit == SwallowedUnit.Minion && minions.Count > 0)
                    {
                        CustomSpell.W2.Cast(CustomSpell.W2.GetCircularFarmLocation(minions).Position);
                    }
                }
            }
        }

        private void Killsteal()
        {
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchQ")
                    .Item("SAssembliesChampionsTahmKenchQKillsteal")
                    .GetValue<bool>() && CustomSpell.Q.IsReady())
            {
                var target = HeroManager.Enemies.FirstOrDefault(enemy => enemy.IsValidTarget(CustomSpell.Q.Range) && 
                    enemy.Health < ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q));

                if (target != null)
                {
                    CustomSpell.Q.CastIfHitchanceEquals(target, HitChance.High);
                }
            }

            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                    .Item("SAssembliesChampionsTahmKenchWKillsteal")
                    .GetValue<bool>() && CustomSpell.W.IsReady())
            {
                var target = HeroManager.Enemies.FirstOrDefault(enemy => enemy.IsValidTarget(CustomSpell.W.Range) &&
                    enemy.Health < ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W));

                if (target != null)
                {
                    if (target.GetBuffCount(tahmPassive) == 3)
                    {
                        CustomSpell.W.CastOnUnit(target);
                        CustomSpell.W.Cast(target);
                    }
                }
            }
        }

        private void Shield() //TODO:Improve for incoming dmg
        {
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchE")
                    .Item("SAssembliesChampionsTahmKenchEShield")
                    .GetValue<bool>() && 
                    ObjectManager.Player.HealthPercent <= 
                    TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchE")
                    .Item("SAssembliesChampionsTahmKenchEShieldPercent")
                    .GetValue<Slider>().Value && CustomSpell.E.IsReady())
            {
                CustomSpell.E.Cast();
            }
        }

        private void SaveAlly()
        {
            if (TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                    .Item("SAssembliesChampionsTahmKenchWAutoShieldAlly")
                    .GetValue<bool>() && swallowedUnit == SwallowedUnit.None
                    && CustomSpell.W.IsReady())
            {
                var target = HeroManager.Allies.FirstOrDefault(ally => !ally.IsMe && !ally.IsDead &&
                   ally.HealthPercent <= TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchW")
                    .Item("SAssembliesChampionsTahmKenchWAutoShieldAllyPercent")
                    .GetValue<Slider>().Value && ObjectManager.Player.Distance(ally) < 500);

                if (target != null)
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, target.ServerPosition);
                    CustomSpell.W.CastOnUnit(target);
                }
            }
        }

        private void TrollMode()
        {
            if (!TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchTroll")
                     .Item("SAssembliesChampionsTahmKenchTrollW")
                     .GetValue<KeyBind>().Active || !CustomSpell.W.IsReady())
            {
                return;
            }

            var hero = HeroManager.Enemies
                .OrderBy(x => ObjectManager.Player.Distance(x.Position))
                .FirstOrDefault(x => !x.IsMe && ObjectManager.Player.Distance(x.Position) < 2000);
            var turret =
                ObjectManager.Get<Obj_AI_Turret>()
                    .OrderBy(x => ObjectManager.Player.Distance(x.Position))
                    .FirstOrDefault(x => ObjectManager.Player.Distance(x.Position) < 2000
                    && x.IsEnemy);

            if (hero != null || turret != null)
            {
                var allyHero = HeroManager.Allies
                    .OrderBy(x => ObjectManager.Player.Distance(x.Position))
                    .FirstOrDefault(x => !x.IsMe && ObjectManager.Player.Distance(x.Position) < CustomSpell.W.Range + 200);

                if (allyHero != null && swallowedUnit == SwallowedUnit.None)
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, allyHero.ServerPosition);
                    CustomSpell.W.CastOnUnit(allyHero);
                }

                if (swallowedUnit == SwallowedUnit.Ally)
                {
                    Obj_AI_Base target = null;
                    if (hero != null && turret != null
                    && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchTroll")
                        .Item("SAssembliesChampionsTahmKenchTrollWToEnemyHero")
                        .GetValue<bool>()
                        && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchTroll")
                        .Item("SAssembliesChampionsTahmKenchTrollWToEnemyTurret")
                        .GetValue<bool>())
                    {
                        if (ObjectManager.Player.Distance(hero) < ObjectManager.Player.Distance(turret))
                        {
                            target = hero;
                        }
                        else if (ObjectManager.Player.Distance(hero) > ObjectManager.Player.Distance(turret))
                        {
                            target = turret;
                        }
                    }
                    else if (hero != null && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchTroll")
                            .Item("SAssembliesChampionsTahmKenchTrollWToEnemyHero")
                            .GetValue<bool>())
                    {
                        target = hero;
                    }
                    else if (turret != null && TahmKenchChampion.GetSubMenu("SAssembliesChampionsTahmKenchTroll")
                            .Item("SAssembliesChampionsTahmKenchTrollWToEnemyTurret")
                            .GetValue<bool>())
                    {
                        target = turret;
                    }

                    if (target != null)
                    {
                        if (ObjectManager.Player.Distance(target) < CustomSpell.W2.Range)
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.Stop, ObjectManager.Player.ServerPosition);
                            CustomSpell.W.Cast(target.ServerPosition);
                        }
                        else
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, target.ServerPosition);
                        }
                    }
                }
            }
        }
    }
}