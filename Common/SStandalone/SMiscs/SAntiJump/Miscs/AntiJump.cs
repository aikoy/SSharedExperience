using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SAssemblies;
using SAssemblies.Miscs;
using SharpDX;
using Menu = SAssemblies.Menu;

namespace SAssemblies.Miscs
{
    class AntiJump
    {
        public static Menu.MenuItemSettings AntiJumpMisc = new Menu.MenuItemSettings(typeof(AntiJump));
        public static Champ Champion = null;

        public AntiJump()
        {
            switch (ObjectManager.Player.ChampionName)
            {
                case "Ashe":
                    Champion = new Champ(1000, SpellSlot.R, true);
                    break;

                case "Ahri":
                    Champion = new Champ(925, SpellSlot.E, true, 1500, 0.25f, 100);
                    break;

                case "Alistar":
                    Champion = new Champ(600, SpellSlot.W, false);
                    break;

                case "Anivia":
                    Champion = new Champ(1100, SpellSlot.Q, true, 850, 250, 110);
                    break;

                case "Azir":
                    Champion = new Champ(200, SpellSlot.R, true);
                    break;

                case "Blitzcrank":
                    Champion = new Champ(950, SpellSlot.Q, true, 1800, 0.25f, 70);
                    break;

                case "Braum":
                    Champion = new Champ(200, SpellSlot.R, true, 2000, 0.25f, 500);
                    break;

                case "Cassiopeia":
                    Champion = new Champ(825, SpellSlot.R, true);
                    break;

                case "Caitlyn":
                    Champion = new Champ(500, SpellSlot.E, true);
                    break;

                case "Chogath":
                    Champion = new Champ(950, SpellSlot.Q, true, 450, 0.75f, 200);
                    break;

                case "Diana":
                    Champion = new Champ(450, SpellSlot.E, true);
                    break;

                case "Draven":
                    Champion = new Champ(1000, SpellSlot.E, true, 1400, 0.28f, 90);
                    break;

                case "Elise":
                    Champion = new Champ(1075, SpellSlot.E, true, 1300, 0.25f, 55);
                    break;

                case "FiddleSticks":
                    Champion = new Champ(525, SpellSlot.Q, false);
                    break;
					
				case "Galio":
                    Champion = new Champ(550, SpellSlot.R, false);
                    break;

                case "Gragas":
                    Champion = new Champ(1100, SpellSlot.R, true, 1000, 0.3f, 700);
                    break;

                case "Irelia":
                    Champion = new Champ(150, SpellSlot.E, false);
                    break;

                case "Janna":
                    Champion = new Champ(675, SpellSlot.R, false);
                    break;

                case "Jax":
                    Champion = new Champ(125, SpellSlot.E, false);
                    break;

                case "Jayce":
                    Champion = new Champ(240, SpellSlot.E, false);
                    break;

                case "Jinx":
                    Champion = new Champ(900, SpellSlot.E, true, 1750, 1.2f, 300);
                    break;

                case "Leona":
                    Champion = new Champ(1200, SpellSlot.R, true, int.MaxValue, 1, 300);
                    break;

                case "LeeSin":
                    Champion = new Champ(325, SpellSlot.R, false);
                    break;
					
				case "Lissandra":
                    Champion = new Champ(500, SpellSlot.R, false);
                    break;
					
				case "Lulu":
                    Champion = new Champ(600, SpellSlot.W, false);
                    break;

                case "Lux":
                    Champion = new Champ(1175, SpellSlot.Q, true, 1200, 0.25f, 70);
                    break;

                case "Malzahar":
                    Champion = new Champ(650, SpellSlot.R, false);
                    break;

                case "Maokai":
                    Champion = new Champ(525, SpellSlot.Q, true, 1200, 0.5f, 110);
                    break;

                case "MonkeyKing":
                    Champion = new Champ(160, SpellSlot.R, false);
                    break;

                case "Nami":
                    Champion = new Champ(875, SpellSlot.Q, true, int.MaxValue, 1f, 150);
                    break;

                case "Pantheon":
                    Champion = new Champ(550, SpellSlot.W, false);
                    break;
					
				case "Quinn":
                    Champion = new Champ(700, SpellSlot.E, false);
                    break;
					
				case "Rammus":
                    Champion = new Champ(200, SpellSlot.Q, false);
                    break;

                case "Riven":
                    Champion = new Champ(260, SpellSlot.W, false);
                    break;

                case "Ryze":
                    Champion = new Champ(550, SpellSlot.W, false);
                    break;
					
				case "Shaco":
                    Champion = new Champ(200, SpellSlot.R, false);
                    break;

				case "Skarner":
                    Champion = new Champ(300, SpellSlot.R, false);
                    break;
					
				case "Singed":
                    Champion = new Champ(100, SpellSlot.E, false);
                    break;

                case "Swain":
                    Champion = new Champ(900, SpellSlot.W, true, 1250, 0.5f, 275);
                    break;

                case "Syndra":
                    Champion = new Champ(650, SpellSlot.E, true, 2500, 0.25f, 22);
                    break;
					
				case "Teemo":
                    Champion = new Champ(550, SpellSlot.Q, false);
                    break;

                case "Thresh":
                    Champion = new Champ(700, SpellSlot.E, true);
                    break;

                case "Tristana":
                    Champion = new Champ(500, SpellSlot.R, false);
                    break;

                case "Vayne":
                    Champion = new Champ(500, SpellSlot.E, false);
                    break;

                case "Velkoz":
                    Champion = new Champ(800, SpellSlot.E, true, 1500, 0.5f, 100);
                    break;

                case "Viktor":
                    Champion = new Champ(700, SpellSlot.W, true, int.MaxValue, 0.5f, 300);
                    break;

                case "Warwick":
                    Champion = new Champ(650, SpellSlot.R, false);
                    break;

                case "Xerath":
                    Champion = new Champ(1150, SpellSlot.E, true, 1400, 0.25f, 60);
                    break;

                case "XinZhao":
                    Champion = new Champ(150, SpellSlot.R, false);
                    break;
					
				case "Zac":
                    Champion = new Champ(250, SpellSlot.R, false);
                    break;
					
				case "Zyra":
                    Champion = new Champ(1050, SpellSlot.E, true, 1400, 0.5f, 70);
                    break;

                default:
                    return;
            }
            Obj_AI_Hero.OnPlayAnimation += Obj_AI_Hero_OnPlayAnimation;
        }

        ~AntiJump()
        {
            Obj_AI_Hero.OnPlayAnimation -= Obj_AI_Hero_OnPlayAnimation;
        }

        public bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && AntiJumpMisc.GetActive();
#else
            return AntiJumpMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsAntiJump");
            if (newMenu == null)
            {
                AntiJumpMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_ANTIJUMP_MAIN"), "SAssembliesMiscsAntiJump"));
                AntiJumpMisc.CreateActiveMenuItem("SAssembliesMiscsAntiJumpActive", () => new AntiJump());
            }
            return AntiJumpMisc;
        }

        void Obj_AI_Hero_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender is Obj_AI_Hero)
            {
                var hero = (Obj_AI_Hero)sender;
                if (hero.Team != ObjectManager.Player.Team)
                {
                    if (IsJumping(hero, args.Animation))
                    {
                        if (Champion.SpellSlot.IsReady())
                        {
                            if (Champion.PosSpell)
                            {
                                Vector3 pos = hero.ServerPosition;
                                if (Champion.Predict)
                                {
                                    PredictionOutput output = Prediction.GetPrediction(hero, Champion.Delay, Champion.Width, Champion.Speed);
                                    if (output.Hitchance >= HitChance.High)
                                    {
                                        pos = output.CastPosition;
                                    }
                                }
                                ObjectManager.Player.Spellbook.CastSpell(Champion.SpellSlot, pos);
                            }
                            else
                            {
                                ObjectManager.Player.Spellbook.CastSpell(Champion.SpellSlot, hero);
                            }
                        }
                    }
                }
            }
        }

        bool IsJumping(Obj_AI_Hero champion, String animation)
        {
            if (ObjectManager.Player.Distance(champion) <= Champion.Range)
            {
                switch (champion.ChampionName)
                {
                    case "Rengar":
                        if (animation.Contains("Spell5"))
                            return true;
                        break;

                    case "Khazix":
                        if (animation.Contains("Spell3"))
                            return true;
                        break;
                }
            }
            return false;
        }

        internal class Champ
        {
            public String Name = ObjectManager.Player.ChampionName;
            public SpellSlot SpellSlot;
            public int Range;
            public bool PosSpell;
            public int Speed;
            public float Delay;
            public int Width;
            public bool Predict = false;
            public Champ(int range, SpellSlot spellSlot, bool posSpell)
            {
                Range = range;
                SpellSlot = spellSlot;
                PosSpell = posSpell;
            }

            public Champ(int range, SpellSlot spellSlot, bool posSpell, int speed, float delay, int width)
            {
                SpellSlot = spellSlot;
                Range = range;
                PosSpell = posSpell;
                Speed = speed;
                Delay = delay;
                Width = width;
                Predict = true;
            }
        }
    }
}
