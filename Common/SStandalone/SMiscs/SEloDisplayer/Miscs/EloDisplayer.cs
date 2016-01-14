using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;

namespace SAssemblies.Miscs
{
    using System.Web.Script.Serialization;
    using System.Windows.Forms;

    using Menu = SAssemblies.Menu;

    class EloDisplayer
    {
        public static Menu.MenuItemSettings EloDisplayerMisc = new Menu.MenuItemSettings(typeof(EloDisplayer));

        private static SpriteHelper.SpriteInfo RunesSprite;

        private Dictionary<Obj_AI_Hero, ChampionEloDisplayer> _allies = new Dictionary<Obj_AI_Hero,ChampionEloDisplayer>();
        private Dictionary<Obj_AI_Hero, ChampionEloDisplayer> _enemies = new Dictionary<Obj_AI_Hero, ChampionEloDisplayer>();
        private Dictionary<Obj_AI_Hero, TeamEloDisplayer> _teams = new Dictionary<Obj_AI_Hero, TeamEloDisplayer>();

        private int lastGameUpdateTime = 0;
        private int lastGameUpdateSpritesTime = 0;
        private int lastGameUpdateTextsTime = 0;

        private TextInfo Header = new TextInfo();
        private TextInfo SummarizedAlly = new TextInfo();
        private TextInfo SummarizedEnemy = new TextInfo();

        SpriteHelper.SpecialBitmap MainBitmap = null;
        Render.Sprite MainFrame = null;

        private int lastSelectedIndex = -1;

        private bool FinishedLoadingComplete = false;

        private bool Loading = false;

        public EloDisplayer()
        {
            if (GetRegionPrefix().Equals(""))
                return;

            Common.ExecuteInOnGameUpdate(() => Init());

            int index = 0;
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsBot)
                    continue;

                if (hero.IsEnemy)
                {
                    _enemies.Add(hero, new ChampionEloDisplayer());
                }
                else
                {
                    _allies.Add(hero, new ChampionEloDisplayer());
                }
                index++;
            }

            new Thread(LoadSpritesAsync).Start();
            new Thread(LoadTextsAsync).Start();
            Game.OnWndProc += Game_OnWndProc;

            EloDisplayerMisc.GetMenuItem("SAssembliesMiscsEloDisplayerScale").ValueChanged += EloDisplayer_ValueChanged;
        }

        ~EloDisplayer()
        {
            Game.OnUpdate -= Game_OnGameUpdateAsyncTexts;
            Game.OnWndProc -= Game_OnWndProc;
        }

        public static bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && EloDisplayerMisc.GetActive();
#else
            return EloDisplayerMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsEloDisplayer");
            if (newMenu == null)
            {
                EloDisplayerMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_ELODISPLAYER_MAIN"), "SAssembliesMiscsEloDisplayer"));
                EloDisplayerMisc.Menu.AddItem(new LeagueSharp.Common.MenuItem("SAssembliesMiscsEloDisplayerKey", Language.GetString("GLOBAL_KEY")).SetValue(new KeyBind(9, KeyBindType.Toggle)));
                EloDisplayerMisc.Menu.AddItem(new LeagueSharp.Common.MenuItem("SAssembliesMiscsEloDisplayerScale", Language.GetString("GLOBAL_SCALE")).SetValue(new Slider(100, 1, 100)));
                EloDisplayerMisc.CreateActiveMenuItem("SAssembliesMiscsEloDisplayerActive", () => new EloDisplayer());
            }
            return EloDisplayerMisc;
        }

        private void Init()
        {           
            using (var ms = new MemoryStream(SpriteHelper.MyResources["EloGui".ToLower()]))
            {
                MainBitmap = new SpriteHelper.SpecialBitmap(ms, new[] { 1f, 1f });
            }
            MainBitmap.AddText("Loading...", new System.Drawing.Point(MainBitmap.Bitmap.Width / 2, MainBitmap.Bitmap.Height / 2 + 25), Brushes.Orange, true, 18);
            this.MainFrame = new Render.Sprite(MainBitmap.Bitmap, 
                new Vector2(Drawing.Width / 2 - MainBitmap.Bitmap.Width, 
                Drawing.Height / 2 - MainBitmap.Bitmap.Height / 2));
            this.MainFrame.Position = new Vector2(Drawing.Width / 2 - MainBitmap.Bitmap.Width / 2, Drawing.Height / 2 - MainBitmap.Bitmap.Height / 2);
            this.MainFrame.VisibleCondition = delegate
            {
                return IsActive() && EloDisplayerMisc.GetMenuItem("SAssembliesMiscsEloDisplayerKey").GetValue<KeyBind>().Active;
            };
            this.MainFrame.Add(5);
            MainBitmap.ResetBitmap();
        }

        void FinishedLoading()
        {
            //When loading finished
            CalculatePositions(true);
            CalculatePositions(false);

            //Header
            String SummonerName = "SummonerName";
            String ChampionName = "ChampionName";
            String Divison = "Divison";
            String RankedStatistics = "RankedStatistics";
            String MMR = "MMR";
            String RecentStatistics = "RecentStatistics";
            String ChampionWinRatio = "ChampionWinRatio";
            String OverallKDA = "OverallKDA";
            String ChampionKDA = "ChampionKDA";
            String Masteries = "Masteries";
            String Runes = "Runes";

            var value = _allies.First().Value;

            MainBitmap.AddText(SummonerName, new System.Drawing.Point((int)value.SummonerName.Position.X, (int)this.Header.Position.Y), Brushes.Orange);
            MainBitmap.AddText(ChampionName, new System.Drawing.Point((int)value.ChampionName.Position.X, (int)this.Header.Position.Y + 20), Brushes.Orange);
            MainBitmap.AddText(Divison, new System.Drawing.Point((int)value.Divison.Position.X, (int)this.Header.Position.Y), Brushes.Orange);
            MainBitmap.AddText(RankedStatistics, new System.Drawing.Point((int)value.RankedStatistics.Position.X, (int)this.Header.Position.Y), Brushes.Orange);
            MainBitmap.AddText(MMR, new System.Drawing.Point((int)value.MMR.Position.X, (int)this.Header.Position.Y + 20), Brushes.Orange);
            MainBitmap.AddText(RecentStatistics, new System.Drawing.Point((int)value.RecentStatistics.Position.X, (int)this.Header.Position.Y), Brushes.Orange);
            MainBitmap.AddText(ChampionWinRatio, new System.Drawing.Point((int)value.ChampionGames.Position.X, (int)this.Header.Position.Y + 20), Brushes.Orange);
            MainBitmap.AddText(OverallKDA, new System.Drawing.Point((int)value.OverallKDA.Position.X, (int)this.Header.Position.Y), Brushes.Orange);
            MainBitmap.AddText(ChampionKDA, new System.Drawing.Point((int)value.ChampionKDA.Position.X, (int)this.Header.Position.Y + 20), Brushes.Orange);
            MainBitmap.AddText(Masteries, new System.Drawing.Point((int)value.MasteriesSmart.Position.X, (int)this.Header.Position.Y), Brushes.Orange);
            MainBitmap.AddText(Runes, new System.Drawing.Point((int)value.Runes.Position.X, (int)this.Header.Position.Y + 20), Brushes.Orange);

            //Ally
            foreach (var ally in _allies)
            {
                Obj_AI_Hero hero = ally.Key;
                ChampionEloDisplayer champ = ally.Value;

                MainBitmap.AddBitmap(champ.SummonerIcon.Sprite.Bitmap, new System.Drawing.Point((int)champ.SummonerIcon.Position.X, (int)champ.SummonerIcon.Position.Y));
                MainBitmap.AddText(hero.Name, new System.Drawing.Point((int)champ.SummonerName.Position.X, (int)champ.SummonerName.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.Divison.WebsiteContent, new System.Drawing.Point((int)champ.Divison.Position.X, (int)champ.Divison.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.RankedStatistics.WebsiteContent, new System.Drawing.Point((int)champ.RankedStatistics.Position.X, (int)champ.RankedStatistics.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.RecentStatistics.WebsiteContent, new System.Drawing.Point((int)champ.RecentStatistics.Position.X, (int)champ.RecentStatistics.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.OverallKDA.WebsiteContent, new System.Drawing.Point((int)champ.OverallKDA.Position.X, (int)champ.OverallKDA.Position.Y), Brushes.Orange);
                MainBitmap.AddText(this.GetMasteriesSmart(hero, champ), new System.Drawing.Point((int)champ.MasteriesSmart.Position.X, (int)champ.MasteriesSmart.Position.Y), Brushes.Orange);
                MainBitmap.AddText(hero.ChampionName, new System.Drawing.Point((int)champ.ChampionName.Position.X, (int)champ.ChampionName.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.MMR.WebsiteContent, new System.Drawing.Point((int)champ.MMR.Position.X, (int)champ.MMR.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.ChampionGames.WebsiteContent, new System.Drawing.Point((int)champ.ChampionGames.Position.X, (int)champ.ChampionGames.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.ChampionKDA.WebsiteContent, new System.Drawing.Point((int)champ.ChampionKDA.Position.X, (int)champ.ChampionKDA.Position.Y), Brushes.Orange);
                MainBitmap.AddText("Click here!", new System.Drawing.Point((int)champ.Runes.Position.X, (int)champ.Runes.Position.Y), Brushes.Orange);
            }

            //Enemy
            foreach (var enemy in _enemies)
            {
                Obj_AI_Hero hero = enemy.Key;
                ChampionEloDisplayer champ = enemy.Value;

                MainBitmap.AddBitmap(champ.SummonerIcon.Sprite.Bitmap, new System.Drawing.Point((int)champ.SummonerIcon.Position.X, (int)champ.SummonerIcon.Position.Y));
                MainBitmap.AddText(hero.Name, new System.Drawing.Point((int)champ.SummonerName.Position.X, (int)champ.SummonerName.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.Divison.WebsiteContent, new System.Drawing.Point((int)champ.Divison.Position.X, (int)champ.Divison.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.RankedStatistics.WebsiteContent, new System.Drawing.Point((int)champ.RankedStatistics.Position.X, (int)champ.RankedStatistics.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.RecentStatistics.WebsiteContent, new System.Drawing.Point((int)champ.RecentStatistics.Position.X, (int)champ.RecentStatistics.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.OverallKDA.WebsiteContent, new System.Drawing.Point((int)champ.OverallKDA.Position.X, (int)champ.OverallKDA.Position.Y), Brushes.Orange);
                MainBitmap.AddText(this.GetMasteriesSmart(hero, champ), new System.Drawing.Point((int)champ.MasteriesSmart.Position.X, (int)champ.MasteriesSmart.Position.Y), Brushes.Orange);
                MainBitmap.AddText(hero.ChampionName, new System.Drawing.Point((int)champ.ChampionName.Position.X, (int)champ.ChampionName.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.MMR.WebsiteContent, new System.Drawing.Point((int)champ.MMR.Position.X, (int)champ.MMR.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.ChampionGames.WebsiteContent, new System.Drawing.Point((int)champ.ChampionGames.Position.X, (int)champ.ChampionGames.Position.Y), Brushes.Orange);
                MainBitmap.AddText(champ.ChampionKDA.WebsiteContent, new System.Drawing.Point((int)champ.ChampionKDA.Position.X, (int)champ.ChampionKDA.Position.Y), Brushes.Orange);
                MainBitmap.AddText("Click here!", new System.Drawing.Point((int)champ.Runes.Position.X, (int)champ.Runes.Position.Y), Brushes.Orange);
            }
            Common.ExecuteInOnGameUpdate(() =>
                {
                    this.MainBitmap.SetOriginalBitmap(this.MainBitmap.Bitmap);
                    float scale = EloDisplayerMisc.GetMenuItem("SAssembliesMiscsEloDisplayerScale").GetValue<Slider>().Value / 100f;
                    this.MainBitmap.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(this.MainBitmap.Bitmap, scale);
                    this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);
                    this.MainFrame.Position = new Vector2(Drawing.Width / 2 - this.MainBitmap.Bitmap.Width / 2, Drawing.Height / 2 - this.MainBitmap.Bitmap.Height / 2);
            });
            FinishedLoadingComplete = true;
        }

        void CalculatePositions(bool calcEnemy)
        {
            Dictionary<Obj_AI_Hero, ChampionEloDisplayer> heroes;
            int index = 0;
            int textFontSize = 20;
            int yOffset = 0;
            int yOffsetTeam = 0;
            if (calcEnemy)
            {
                heroes = _enemies;
                yOffset = 430;
                yOffsetTeam = 380;
            }
            else
            {
                heroes = _allies;
                yOffset = 110;
                yOffsetTeam = 360;
            }

            foreach (var hero in heroes)
            {
                if (hero.Value.SummonerIcon != null && hero.Value.SummonerIcon.Sprite != null && hero.Value.SummonerIcon.Sprite.Bitmap != null)
                {
                    hero.Value.SummonerIcon.Position = new Vector2(70, yOffset + (index * hero.Value.SummonerIcon.Sprite.Bitmap.Height));
                    hero.Value.SummonerName.Position = new Vector2(hero.Value.SummonerIcon.Position.X + hero.Value.SummonerIcon.Sprite.Bitmap.Width + 10, hero.Value.SummonerIcon.Position.Y);
                    hero.Value.ChampionName.Position = new Vector2(hero.Value.SummonerName.Position.X, hero.Value.SummonerName.Position.Y + textFontSize);
                    hero.Value.Divison.Position = new Vector2(hero.Value.SummonerName.Position.X + 150, hero.Value.SummonerName.Position.Y + textFontSize / 2);
                    hero.Value.RankedStatistics.Position = new Vector2(hero.Value.Divison.Position.X + 150, hero.Value.Divison.Position.Y - textFontSize / 2);
                    hero.Value.MMR.Position = new Vector2(hero.Value.RankedStatistics.Position.X, hero.Value.RankedStatistics.Position.Y + textFontSize);
                    hero.Value.RecentStatistics.Position = new Vector2(hero.Value.MMR.Position.X + 150, hero.Value.MMR.Position.Y - textFontSize);
                    hero.Value.ChampionGames.Position = new Vector2(hero.Value.RecentStatistics.Position.X, hero.Value.RecentStatistics.Position.Y + textFontSize);
                    hero.Value.OverallKDA.Position = new Vector2(hero.Value.ChampionGames.Position.X + 150, hero.Value.ChampionGames.Position.Y - textFontSize);
                    hero.Value.ChampionKDA.Position = new Vector2(hero.Value.OverallKDA.Position.X, hero.Value.OverallKDA.Position.Y + textFontSize);
                    hero.Value.MasteriesSmart.Position = new Vector2(hero.Value.ChampionKDA.Position.X + 150, hero.Value.ChampionKDA.Position.Y - textFontSize);
                    hero.Value.Runes.Position = new Vector2(hero.Value.MasteriesSmart.Position.X, hero.Value.MasteriesSmart.Position.Y + textFontSize);
                }
                index++;
            }

            Header.Position = new Vector2(_allies.First().Value.SummonerName.Position.X, _allies.First().Value.SummonerName.Position.Y - yOffset / 1.3f);
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (!IsActive() || !FinishedLoadingComplete)
                return;

            HandleInput((WindowsMessages)args.Msg, Utils.GetCursorPos(), args.WParam);
        }

        private void HandleInput(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            if (message != WindowsMessages.WM_LBUTTONUP)
            {
                return;
            }
            int textFontSize = 20;
            bool updated = false;
            int index = 0;
            float scale = EloDisplayerMisc.GetMenuItem("SAssembliesMiscsEloDisplayerScale").GetValue<Slider>().Value / 100f;
            foreach (var ally in _allies)
            {
                Obj_AI_Hero hero = ally.Key;
                ChampionEloDisplayer champ = ally.Value;

                if (Utils.IsUnderRectangle(
                    cursorPos,
                    this.MainFrame.X + champ.Runes.Position.X * scale,
                    this.MainFrame.Y + champ.Runes.Position.Y * scale,
                    150,
                    textFontSize))
                {
                    if (lastSelectedIndex != index)
                    {
                        MainBitmap.ResetBitmap();
                        System.Drawing.Font arialFont = new System.Drawing.Font("Arial", 12);
                        Size size = TextRenderer.MeasureText(champ.Runes.WebsiteContent, arialFont);
                        MainBitmap.AddColoredRectangle(new System.Drawing.Point((int)champ.Runes.Position.X + 150, (int)champ.Runes.Position.Y), new Size(size.Width, size.Height), System.Drawing.Color.Black, 90);
                        MainBitmap.AddText(champ.Runes.WebsiteContent, new System.Drawing.Point((int)champ.Runes.Position.X + 150, (int)champ.Runes.Position.Y), Brushes.Orange);
                        Common.ExecuteInOnGameUpdate(
                            () =>
                                {
                                    this.MainBitmap.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(this.MainBitmap.Bitmap, scale);
                                    this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);
                                });
                        arialFont.Dispose();
                    }
                    updated = true;
                    lastSelectedIndex = index;
                }
                index++;
            }
            foreach (var enemy in _enemies)
            {
                Obj_AI_Hero hero = enemy.Key;
                ChampionEloDisplayer champ = enemy.Value;

                if (Utils.IsUnderRectangle(
                    cursorPos,
                    this.MainFrame.X + champ.Runes.Position.X * scale,
                    this.MainFrame.Y + champ.Runes.Position.Y * scale,
                    150,
                    textFontSize))
                {
                    if (lastSelectedIndex != index)
                    {
                        MainBitmap.ResetBitmap();
                        System.Drawing.Font arialFont = new System.Drawing.Font("Arial", 12);
                        Size size = TextRenderer.MeasureText(champ.Runes.WebsiteContent, arialFont);
                        MainBitmap.AddColoredRectangle(new System.Drawing.Point((int)champ.Runes.Position.X + 150, (int)champ.Runes.Position.Y), new Size(size.Width, size.Height), System.Drawing.Color.Black, 90);
                        MainBitmap.AddText(champ.Runes.WebsiteContent, new System.Drawing.Point((int)champ.Runes.Position.X + 150, (int)champ.Runes.Position.Y), Brushes.Orange);
                        Common.ExecuteInOnGameUpdate(
                            () =>
                            {
                                this.MainBitmap.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(this.MainBitmap.Bitmap, scale);
                                this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);
                            });
                        arialFont.Dispose();
                    }
                    updated = true;
                    lastSelectedIndex = index;
                }
                index++;
            }
            if (!updated)
            {
                if (this.MainBitmap.ResetBitmap())
                {
                    this.MainBitmap.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(this.MainBitmap.Bitmap, scale);
                    this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);
                }
                lastSelectedIndex = -1;
            }
        }

        private void EloDisplayer_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            MainBitmap.ResetBitmap();
            float scale = e.GetNewValue<Slider>().Value / 100f;
            this.MainBitmap.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(this.MainBitmap.Bitmap, scale);
            this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);
            this.MainFrame.Position = new Vector2(Drawing.Width / 2 - this.MainBitmap.Bitmap.Width / 2, Drawing.Height / 2 - this.MainBitmap.Bitmap.Height / 2);
        }

        void Game_OnGameUpdateAsyncTexts(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTextsTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTextsTime = Environment.TickCount;

            bool finished = true;
            foreach (var ally in _allies)
            {
                if (ally.Value.IsFinished())
                {
                    continue;
                }
                else
                {
                    finished = false;
                    break;
                }
            }
            foreach (var enemy in _enemies)
            {
                if (enemy.Value.IsFinished())
                {
                    continue;
                }
                else
                {
                    finished = false;
                    break;
                }
            }
            if (finished && !Loading && !FinishedLoadingComplete)
            {
                Loading = true;
                new Thread(this.FinishedLoading).Start();
            }
        }

        private void LoadSpritesAsync()
        {
            bool finished = true;
            do
            {
                foreach (var ally in _allies)
                {
                    try
                    {
                        ally.Value.SummonerIcon.WebsiteContent = GetSummonerIcon(ally.Key, ally.Value);
                        if (!ally.Value.SummonerIcon.WebsiteContent.Equals(""))
                        {
                            SpriteHelper.DownloadImageRiot(
                            ally.Value.SummonerIcon.WebsiteContent,
                            SpriteHelper.ChampionType.None,
                            SpriteHelper.DownloadType.ProfileIcon,
                            @"EloDisplayer\");
                            ally.Value.SummonerIcon.Sprite = new SpriteHelper.SpriteInfo();
                            ally.Value.SummonerIcon.Sprite.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(SpriteHelper.SpecialBitmap.LoadBitmap(ally.Value.SummonerIcon.WebsiteContent, @"EloDisplayer\"), 0.35f);
                        }
                        else
                        {
                            throw new Exception();
                        }
                        ally.Value.SummonerIcon.FinishedLoading = true;
                    }
                    catch (Exception)
                    {
                        finished = false;
                    }
                }
                foreach (var enemy in _enemies)
                {
                    try
                    {
                        enemy.Value.SummonerIcon.WebsiteContent = GetSummonerIcon(enemy.Key, enemy.Value);
                        if (!enemy.Value.SummonerIcon.WebsiteContent.Equals(""))
                        {
                            SpriteHelper.DownloadImageRiot(
                            enemy.Value.SummonerIcon.WebsiteContent,
                            SpriteHelper.ChampionType.None,
                            SpriteHelper.DownloadType.ProfileIcon,
                            @"EloDisplayer\");
                            enemy.Value.SummonerIcon.Sprite = new SpriteHelper.SpriteInfo();
                            enemy.Value.SummonerIcon.Sprite.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(SpriteHelper.SpecialBitmap.LoadBitmap(enemy.Value.SummonerIcon.WebsiteContent, @"EloDisplayer\"), 0.35f);
                        }
                        else
                        {
                            throw new Exception();
                        }
                        enemy.Value.SummonerIcon.FinishedLoading = true;
                    }
                    catch (Exception)
                    {
                        finished = false;
                    }
                }
            } while (!finished);
        }

        private void LoadTextsAsync()
        {
            bool finished = true;
            do
            {
                finished = true;
                foreach (var ally in _allies)
                {
                    try
                    {
                        if (!UpdateStatus(ally.Key, ally.Value))
                        {
                            finished = false;
                            continue;
                        }
                        ally.Value.Divison.WebsiteContent = GetDivision(ally.Key, ally.Value, ref ally.Value.Ranked);
                        ally.Value.Divison.FinishedLoading = true;
                        ally.Value.RankedStatistics.WebsiteContent = GetRankedStatistics(ally.Key, ally.Value, true);
                        ally.Value.RankedStatistics.FinishedLoading = true;
                        ally.Value.MMR.WebsiteContent = GetMmr(ally.Key, ally.Value, true);
                        ally.Value.MMR.FinishedLoading = true;
                        ally.Value.RecentStatistics.WebsiteContent = GetRecentStatistics(ally.Key, ally.Value);
                        ally.Value.RecentStatistics.FinishedLoading = true;
                        ally.Value.ChampionGames.WebsiteContent = this.GetChampionWinRatioLastSeason(ally.Key, ally.Value, true);
                        if (ally.Value.ChampionGames.WebsiteContent.Equals("-1%"))
                        {
                            ally.Value.ChampionGames.WebsiteContent = this.GetChampionWinRatioNormal(ally.Key, ally.Value);
                        }
                        ally.Value.ChampionGames.FinishedLoading = true;
                        ally.Value.OverallKDA.WebsiteContent = GetOverallKDA(ally.Key, ally.Value);
                        ally.Value.OverallKDA.FinishedLoading = true;
                        ally.Value.ChampionKDA.WebsiteContent = GetChampionKDALastSeason(ally.Key, ally.Value, true);
                        if (ally.Value.ChampionKDA.WebsiteContent.Equals("0/0/0"))
                        {
                            ally.Value.ChampionKDA.WebsiteContent = GetChampionKDANormal(ally.Key, ally.Value);
                        }
                        ally.Value.ChampionKDA.FinishedLoading = true;
                        ally.Value.Runes.WebsiteContent = GetRunes(ally.Key, ally.Value);
                        ally.Value.Runes.FinishedLoading = true;
                    }
                    catch (Exception e)
                    {
                        finished = false;
                    }
                }
                foreach (var enemy in _enemies)
                {
                    try
                    {
                        if (!UpdateStatus(enemy.Key, enemy.Value))
                        {
                            finished = false;
                            continue;
                        }
                        enemy.Value.Divison.WebsiteContent = GetDivision(enemy.Key, enemy.Value, ref enemy.Value.Ranked);
                        enemy.Value.Divison.FinishedLoading = true;
                        enemy.Value.RankedStatistics.WebsiteContent = GetRankedStatistics(enemy.Key, enemy.Value, true);
                        enemy.Value.RankedStatistics.FinishedLoading = true;
                        enemy.Value.MMR.WebsiteContent = GetMmr(enemy.Key, enemy.Value, true);
                        enemy.Value.MMR.FinishedLoading = true;
                        enemy.Value.RecentStatistics.WebsiteContent = GetRecentStatistics(enemy.Key, enemy.Value);
                        enemy.Value.RecentStatistics.FinishedLoading = true;
                        enemy.Value.ChampionGames.WebsiteContent = this.GetChampionWinRatioLastSeason(enemy.Key, enemy.Value, true);
                        if (enemy.Value.ChampionGames.WebsiteContent.Equals("-1%"))
                        {
                            enemy.Value.ChampionGames.WebsiteContent = this.GetChampionWinRatioNormal(enemy.Key, enemy.Value);
                        }
                        enemy.Value.ChampionGames.FinishedLoading = true;
                        enemy.Value.OverallKDA.WebsiteContent = GetOverallKDA(enemy.Key, enemy.Value);
                        enemy.Value.OverallKDA.FinishedLoading = true;
                        enemy.Value.ChampionKDA.WebsiteContent = GetChampionKDALastSeason(enemy.Key, enemy.Value, true);
                        if (enemy.Value.ChampionKDA.WebsiteContent.Equals("0/0/0"))
                        {
                            enemy.Value.ChampionKDA.WebsiteContent = GetChampionKDANormal(enemy.Key, enemy.Value);
                        }
                        enemy.Value.ChampionKDA.FinishedLoading = true;
                        enemy.Value.Runes.WebsiteContent = GetRunes(enemy.Key, enemy.Value);
                        enemy.Value.Runes.FinishedLoading = true;
                    }
                    catch (Exception e)
                    {
                        finished = false;
                    }
                }
            } while (!finished);
            if (finished && !Loading && !FinishedLoadingComplete)
            {
                Loading = true;
                new Thread(this.FinishedLoading).Start();
            }
        }

        public static String GetLolWebSiteContent(String webSite)
        {
            return GetLolWebSiteContent(webSite, null);
        }

        public static String GetLolWebSiteContent(String webSite, String param)
        {
            return GetWebSiteContent(GetWebSite() + webSite, param);
        }

        public static String GetWebSiteContent(String webSite, String param = null)
        {
            List<Cookie> cookies = new List<Cookie>();
            cookies.Add(new Cookie("customLocale", "en_US", "", GetWebSiteWithoutHttp()));
            return Website.GetWebSiteContent(webSite, cookies, param);
        }

        public static String GetLolWebSiteContentOverview(Obj_AI_Hero hero)
        {
            string playerName = GetEncodedPlayerName(hero);
            return GetLolWebSiteContent("summoner/userName=" + playerName);
        }

        private String GetLolWebSiteContentChampions(Obj_AI_Hero hero)
        {
            string playerName = GetEncodedPlayerName(hero);
            return GetLolWebSiteContent("summoner/champions/userName=" + playerName);
        }

        private String GetLolWebSiteContentRunes(Obj_AI_Hero hero)
        {
            string playerName = GetEncodedPlayerName(hero);
            return GetLolWebSiteContent("summoner/rune/userName=" + playerName);
        }

        private String GetLolWebSiteContentMasteries(Obj_AI_Hero hero)
        {
            string playerName = GetEncodedPlayerName(hero);
            return GetLolWebSiteContent("summoner/mastery/userName=" + playerName);
        }

        public static String GetEncodedPlayerName(Obj_AI_Hero hero)
        {
            return HttpUtility.UrlEncode(hero.Name);
        }

        public static String GetWebSite()
        {
            String prefix = GetRegionPrefix();
            if (prefix == "")
                return "http://op.gg/";
            else
                return "http://" + prefix + ".op.gg/";
        }

        public static String GetWebSiteWithoutHttp()
        {
            String prefix = GetRegionPrefix();
            if (prefix == "")
                return "op.gg";
            else
                return prefix + ".op.gg";
        }

        public static String GetRegionPrefix()
        {
            switch (Game.Region.ToLower())
            {
                case "euw1":
                    return "euw";

                case "eun1":
                    return "eune";

                case "la1":
                    return "lan";

                case "la2":
                    return "las";

                case "oc1":
                    return "oce";

                case "kr":
                    return "www";

                case "ru":
                    return "ru";

                case "na1":
                    return "na";

                case "br1":
                    return "br";

                case "tr1":
                    return "tr";

                case "sg1":
                    return "sg";

                case "vn1":
                    return "vn";

                case "ph1":
                    return "ph";

                case "tw1":
                    return "tw";

                case "th1":
                    return "th";

                case "id1":
                    return "id";

                default:
                    return "";
            }
        }

        private String GetSummonerIcon(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            String websiteContent = elo.GetLolWebSiteContentOverview(hero);
            String patternWin = "<img src=\"//(.*?)op.gg/images/profile_icons/profileIcon(.*?).jpg\" class=\"ProfileImage\">";
            return Website.GetMatch(websiteContent, patternWin, 0, 2) + ".png";
        }

        private String GetDivision(Obj_AI_Hero hero, ChampionEloDisplayer elo, ref bool ranked)
        {
            if (!elo.Divison.WebsiteContent.Equals(""))
            {
                return elo.Divison.WebsiteContent;
            }
            String division = "";
            String websiteContent = elo.GetLolWebSiteContentOverview(hero);
            String patternTierRank = "<div class=\"TierRank\">";
            String patternLeaguePoints = "<div class=\"leaguePoints\">";

            String patternSoloUnrankedTierRank = "<span class=\"TierRank\">Unranked</span>";
            String patternLevel = "<span class=\"Level tip\" title=\"Level\">30</span>";

            String patternSoloRankedTierRank = "<span class=\"tierRank\">(.*?)</span>";
            String patternSoloRankedLeaguePoints = "<span class=\"LeaguePoints\">(.*?) LP</span>";

            String patternTeamRankedTierRank = "<div class=\"TierRank\">(\\w.*?)</div>";
            String patternTeamRankedLeaguePoints = "<div class=\"leaguePoints\">(.*?) LP</div>";
            String patternTeamRankedTypeTeam = "<div class=\"StatSummaryType\">(.*?)(\\d.*?)</div>";

            if (!Website.GetMatch(websiteContent, patternLevel, 0, 0).Equals(""))
            {
                if (!Website.GetMatch(websiteContent, patternSoloRankedTierRank).Equals("") && !Website.GetMatch(websiteContent, patternSoloRankedLeaguePoints).Equals(""))
                {
                    division = Website.GetMatch(websiteContent, patternSoloRankedTierRank) + " (" + Website.GetMatch(websiteContent, patternSoloRankedLeaguePoints) + " LP)";
                }
                else if (!Website.GetMatch(websiteContent, patternTeamRankedTierRank).Equals("") && !Website.GetMatch(websiteContent, patternTeamRankedLeaguePoints).Equals(""))
                {
                    division = Website.GetMatch(websiteContent, patternTeamRankedTierRank) + " (" + Website.GetMatch(websiteContent, patternTeamRankedLeaguePoints) + " LP, " +
                        Website.GetMatch(websiteContent, patternTeamRankedTypeTeam, 0, 2) + ")";
                }
                else
                {
                    division = "Unranked";
                }
            }
            else
            {
                division = "Unranked (<30)";
            }
            return division;
        }

        private String GetRankedStatistics(Obj_AI_Hero hero, ChampionEloDisplayer elo, bool ranked)
        {
            if (!elo.RankedStatistics.WebsiteContent.Equals(""))
            {
                return elo.RankedStatistics.WebsiteContent;
            }
            if (!ranked)
                return "0W/0L";

            String websiteContent = elo.GetLolWebSiteContentOverview(hero);
            String patternWin = "<span class=\"Win\">(.*?)W</span>";
            String patternLoose = "W</span> (.*?)L \\(Win Ratio (.*?)%\\)";
            String matchWin = Website.GetMatch(websiteContent, patternWin);
            String matchLose = Website.GetMatch(websiteContent, patternLoose);
            return (!matchWin.Equals("") ? matchWin : "0") + "W/" +
                  (!matchLose.Equals("") ? matchLose : "0") + "L";
        }

        private String GetRecentStatistics(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            if (!elo.RecentStatistics.WebsiteContent.Equals(""))
            {
                return elo.RecentStatistics.WebsiteContent;
            }
            String websiteContent = elo.GetLolWebSiteContentOverview(hero);
            String patternWl = @"<div class=""WinRatioTitle"">(.*?)</div>";
            String matchWl = Website.GetMatch(websiteContent, patternWl);
            String patternWin = @"(\d*?)W";
            String patternLoose = @"(\d*?)L";
            String matchWin = Website.GetMatch(matchWl, patternWin);
            String matchLose = Website.GetMatch(matchWl, patternLoose);
            return (!matchWin.Equals("") ? matchWin : "0") + "W/" +
                  (!matchLose.Equals("") ? matchLose : "0") + "L";
        }

        private String GetOverallKDA(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            if (!elo.OverallKDA.WebsiteContent.Equals(""))
            {
                return elo.OverallKDA.WebsiteContent;
            }
            String websiteContent = elo.GetLolWebSiteContentOverview(hero);
            String patternKill = "<span class=\"Kill\">(.*?)</span>";
            String patternDeath = "<span class=\"Death\">(.*?)</span>";
            String patternAssist = "<span class=\"Assist\">(.*?)</span>";
            String matchKill = Website.GetMatch(websiteContent, patternKill);
            String matchDeath = Website.GetMatch(websiteContent, patternDeath);
            String matchAssist = Website.GetMatch(websiteContent, patternAssist);
            return (!matchKill.Equals("") ? matchKill : "0") + "/" +
                  (!matchDeath.Equals("") ? matchDeath : "0") + "/" +
                  (!matchAssist.Equals("") ? matchAssist : "0");
        }

        private String GetMmr(Obj_AI_Hero hero, ChampionEloDisplayer elo, bool ranked)
        {
            if (!elo.MMR.WebsiteContent.Equals(""))
            {
                return elo.MMR.WebsiteContent;
            }
            if (!ranked)
                return "0";

            String websiteContent = "";
            try
            {
                websiteContent = GetLolWebSiteContent("summoner/ajax/mmr/" + "summonerName=" + GetEncodedPlayerName(hero));
            }
            catch (Exception ex)
            {
                if (!ex.Message.Equals("The remote server returned an error: (418)."))
                {
                    throw;
                }
                else
                {
                    return "0/0";
                }
            }
            String patternMmr = "<div class=\"MMR\">(.*?)(\\d.\\d{1,4}.*?)(.*?)</div>";
            String patternAverageMmr = "<span class=\"InlineMiddle\">(.*?)(\\d.\\d{1,4}.*?)(.*?)</span>";
            String matchMmr = Website.GetMatch(websiteContent, patternMmr, 0, 2);
            String matchAverageMmr = Website.GetMatch(websiteContent, patternAverageMmr, 0, 2);
            return (!matchMmr.Equals("") ? matchMmr : "0") + "/" +
                   (!matchAverageMmr.Equals("") ? matchAverageMmr : "0");
        }

        private String GetMasteriesSmart(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            if (!elo.MasteriesSmart.WebsiteContent.Equals(""))
            {
                return elo.MasteriesSmart.WebsiteContent;
            }
            int offense = 0;
            int defense = 0;
            int utility = 0;
            for (int i = 0; i < hero.Masteries.Count(); i++)
            {
                if (hero.Masteries[i] != null)
                {
                    var mastery = hero.Masteries[i];
                    if (mastery.Page == MasteryPage.Ferocity)
                    {
                        offense += mastery.Points;
                    }
                    else if (mastery.Page == MasteryPage.Cunning)
                    {
                        defense += mastery.Points;
                    }
                    else
                    {
                        utility += mastery.Points;
                    }
                }
            }
            return offense + "/" + defense + "/" + utility;
        }

        private String GetMasteries(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            if (!elo.Masteries.WebsiteContent.Equals(""))
            {
                return elo.Masteries.WebsiteContent;
            }
            String masteries = "";
            String patternActiveRuneSite = "<button class=\"Button tabHeader active\" data-tab-show-class=\"(.*?)\">.*?</button>";
            String matchActiveRuneSite = Website.GetMatch(GetLolWebSiteContentRunes(hero), patternActiveRuneSite);
            String patternOuterRunePage =
                "<div class=\"MasteryPageWrap tabItem " + matchActiveRuneSite + "\">(.*?)</div></div></div>";
            String matchOuterRunePage = Website.GetMatch(GetLolWebSiteContentRunes(hero), patternOuterRunePage);
            String patternInnerRunePage = "<span class=\"Title\">(.*?)</span>.*?<span class=\"Stat\">(.*?)</span>";

            return masteries;
        }

        private String GetRunes(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            if (!elo.Runes.WebsiteContent.Equals(""))
            {
                return elo.Runes.WebsiteContent;
            }
            String runes = "";
            String patternActiveRuneSite = "<button class=\"Button tabHeader active\" data-tab-show-class=\"(.*?)\">";
            String matchActiveRuneSite = Website.GetMatch(GetLolWebSiteContentRunes(hero), patternActiveRuneSite);
            String patternOuterRunePage =
                "<div class=\"RunePageWrap tabItem " + matchActiveRuneSite + "\">(.*?)</dd></dl></div>";
            String matchOuterRunePage = Website.GetMatch(GetLolWebSiteContentRunes(hero), patternOuterRunePage);
            String patternInnerRunePage = "<span class=\"Title\">(.*?)</span>.*?<span class=\"Stat\">(.*?)</span>";
            for (int i = 0; ; i++)
            {
                String matchInnerRunePageTitle = Website.GetMatch(matchOuterRunePage, patternInnerRunePage, i, 1);
                String matchInnerRunePageStat = Website.GetMatch(matchOuterRunePage, patternInnerRunePage, i, 2);
                if (matchInnerRunePageTitle.Equals("") || matchInnerRunePageStat.Equals(""))
                {
                    break;
                }
                runes += matchInnerRunePageTitle + ": " + matchInnerRunePageStat + "\n";
            }
            return runes;
        }

        private String GetChampionKDA(Obj_AI_Hero hero, ChampionEloDisplayer elo, String season)
        {
            if (!elo.ChampionKDA.WebsiteContent.Equals("") && !elo.ChampionKDA.WebsiteContent.Equals("0/0/0"))
            {
                return elo.ChampionKDA.WebsiteContent;
            }
            String championContent = elo.GetLolWebSiteContentChampion(hero, GetSummonerId(elo.GetLolWebSiteContentOverview(hero)), season);
            String patternChampion = "<td class=\"ChampionName Cell\">(.*?)</td>";
            String patternKill = "<td class=\"Kill Cell\"><div class=\"Value\">(.*?)</div></td>";
            String patternDeath = "<td class=\"Death Cell\"><div class=\"Value\">(.*?)</div></td>";
            String patternAssist = "<td class=\"Assist Cell\"><div class=\"Value\">(.*?)</div></td>";
            String matchKill = "";
            String matchDeath = "";
            String matchAssist = "";

            for (int i = 0; ; i++)
            {
                String matchChampion = Website.GetMatch(championContent, patternChampion, i);
                matchChampion = matchChampion.Replace(" ", "");
                matchChampion = matchChampion.Replace("&#039;", "");
                if (matchChampion.Contains(hero.ChampionName))
                {
                    matchKill = Website.GetMatch(championContent, patternKill, i);
                    matchDeath = Website.GetMatch(championContent, patternDeath, i);
                    matchAssist = Website.GetMatch(championContent, patternAssist, i);
                    break;
                }
                else if (matchChampion.Equals(""))
                {
                    break;
                }
            }
            if (matchKill.Equals("") && matchDeath.Equals("") && matchAssist.Equals(""))
                return "0/0/0";
            return matchKill + "/" + matchDeath + "/" + matchAssist;
        }

        private String GetChampionKDALastSeason(Obj_AI_Hero hero, ChampionEloDisplayer elo, bool ranked)
        {
            if (!ranked)
                return "0/0/0";

            return GetChampionKDA(hero, elo, "4");
        }

        private String GetChampionKDANormal(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            return GetChampionKDA(hero, elo, "normal");
        }

        private String GetChampionWinRatio(Obj_AI_Hero hero, ChampionEloDisplayer elo, String season)
        {
            if (!elo.ChampionGames.WebsiteContent.Equals("") && !elo.ChampionGames.WebsiteContent.Equals("-1%"))
            {
                return elo.ChampionGames.WebsiteContent;
            }
            String championContent = elo.GetLolWebSiteContentChampion(hero, GetSummonerId(elo.GetLolWebSiteContentOverview(hero)), season);
            String patternChampion = "<td class=\"ChampionName Cell\">(.*?)</td>";
            String patternWinRatio = "<span class=\"WinRatio(.*?)\">(.*?)</span>";
            String matchWinRatio = "";

            for (int i = 0; ; i++)
            {
                String matchChampion = Website.GetMatch(championContent, patternChampion, i);
                matchChampion = matchChampion.Replace(" ", "");
                matchChampion = matchChampion.Replace("&#039;", "");
                if (matchChampion.Contains(hero.ChampionName))
                {
                    matchWinRatio = Website.GetMatch(championContent, patternWinRatio, i, 2);
                    break;
                }
                else if (matchChampion.Equals(""))
                {
                    break;
                }
            }
            if (matchWinRatio.Equals(""))
                return "-1%";
            return matchWinRatio;
        }

        private String GetChampionWinRatioLastSeason(Obj_AI_Hero hero, ChampionEloDisplayer elo, bool ranked)
        {
            if (!ranked)
                return "-1%";

            return this.GetChampionWinRatio(hero, elo, "4");
        }

        private String GetChampionWinRatioNormal(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            return this.GetChampionWinRatio(hero, elo, "normal");
        }

        private void CalculateTeamStats(String websiteContent)
        {
            
        }

        private void GetTeamBans(Obj_AI_Hero hero) //TODO: Create pattern for bans.
        {
            string playerName = HttpUtility.UrlEncode(hero.Name);
            String championContent = GetLolWebSiteContent("summoner/ajax/spectator/", "userName=" + playerName + "&force=true");
        }

        private bool UpdateStatus(Obj_AI_Hero hero, ChampionEloDisplayer elo)
        {
            String websiteContent = elo.GetLolWebSiteContentOverview(hero);
            if (
                GetLolWebSiteContent("summoner/ajax/update.json/summonerId=" + GetSummonerId(websiteContent))
                    .Contains("\"error\":true"))
            {
                return true;
            }
            return false;
            //WebRequest.Create(updateUrl).GetResponse();
        }

        private String GetSummonerId(String websiteContent)
        {
            String pattern = "data-summoner-id=\"(.*?)\"";
            return Website.GetMatch(websiteContent, pattern);
        }

        private static T FromJson<T>(string input)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Deserialize<T>(input);
        }

        private static string ToJson(object input)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(input);
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private T GetJSonResponse<T>(String url, object request)
        {
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            String json = ToJson(request);
            Byte[] bytes = Encoding.ASCII.GetBytes(json);
            webRequest.ContentLength = bytes.Length;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            String content = "";
            using (var response = (HttpWebResponse)webRequest.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    if (receiveStream != null)
                    {
                        if (response.CharacterSet == null)
                        {
                            using (StreamReader readStream = new StreamReader(receiveStream))
                            {
                                content = @readStream.ReadToEnd();
                            }
                        }
                        else
                        {
                            using (
                                StreamReader readStream = new StreamReader(receiveStream,
                                    Encoding.GetEncoding(response.CharacterSet)))
                            {
                                content = @readStream.ReadToEnd();
                            }
                        }
                    }
                }
            }
            try
            {
                return FromJson<T>(content);
            }
            catch (Exception)
            {
                return (T) new Object();
            }
        }

        class ChampionEloDisplayer
        {
            public bool Ranked = false;
            public TextInfo SummonerIcon = new TextInfo();
            public TextInfo ChampionName = new TextInfo();
            public TextInfo SummonerName = new TextInfo();
            public TextInfo Divison = new TextInfo();
            public TextInfo RankedStatistics = new TextInfo();
            public TextInfo RecentStatistics = new TextInfo();
            public TextInfo MMR = new TextInfo();
            public TextInfo Masteries = new TextInfo();
            public TextInfo MasteriesSmart = new TextInfo();
            public TextInfo Runes = new TextInfo();
            public TextInfo OverallKDA = new TextInfo();
            public TextInfo ChampionKDA = new TextInfo();
            public TextInfo ChampionGames = new TextInfo();

            public TextInfo MasteriesRunesSprite = new TextInfo();

            private String websiteContentOverview = "";
            private String websiteContentChampion = "";
            private String _currentSeason = "";

            public String GetLolWebSiteContentOverview(Obj_AI_Hero hero)
            {
                if (websiteContentOverview == "")
                {
                    websiteContentOverview = EloDisplayer.GetLolWebSiteContentOverview(hero);
                }
                return websiteContentOverview;
            }

            public String GetLolWebSiteContentChampion(Obj_AI_Hero hero, String summonerId, String season)
            {
                if (websiteContentChampion == "" || _currentSeason == "" || !_currentSeason.Equals(season))
                {
                    _currentSeason = season;
                    websiteContentChampion =
                        GetLolWebSiteContent(
                            "/summoner/champions/ajax/champions.rank/summonerId=" + summonerId + "&season=" + season);
                }
                return websiteContentChampion;
            }

            public bool IsFinished()
            {
                return SummonerIcon.FinishedLoading &&
                    Divison.FinishedLoading &&
                    RankedStatistics.FinishedLoading &&
                    RecentStatistics.FinishedLoading &&
                    MMR.FinishedLoading &&
                    OverallKDA.FinishedLoading &&
                    ChampionKDA.FinishedLoading &&
                    ChampionGames.FinishedLoading;
            }
        }

        class TeamEloDisplayer
        {
            public TextInfo TeamBans = new TextInfo();
            public TextInfo TeamDivison = new TextInfo();
            public TextInfo TeamRankedStatistics = new TextInfo();
            public TextInfo TeamRecentStatistics = new TextInfo();
            public TextInfo TeamMMR = new TextInfo();
            public TextInfo TeamChampionGames = new TextInfo();
        }

        internal class TextInfo
        {
            public bool FinishedLoading = false;
            public String WebsiteContent = "";

            public SpriteHelper.SpriteInfo Sprite;
            public Render.Text Text;
            public Vector2 Position;
        }
    }
}
