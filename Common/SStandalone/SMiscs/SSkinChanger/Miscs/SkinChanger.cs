using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Speech.Synthesis.TtsEngine;
using System.Threading;
using System.Web.Script.Serialization;
using LeagueSharp;
using LeagueSharp.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;

namespace SAssemblies.Miscs
{
    using System.Drawing;
    using System.Windows.Forms;

    using Menu = SAssemblies.Menu;
    using MenuItem = LeagueSharp.Common.MenuItem;

    internal class SkinChanger
    {
        public static Menu.MenuItemSettings SkinChangerMisc = new Menu.MenuItemSettings(typeof(SkinChanger));

        public static Dictionary<String, String[]> Skins = new Dictionary<string, string[]>();
        private static List<String> _skins = new List<string>(); 
        private int _lastSkinId = -1;
        private bool _isDead = false;

        SpriteHelper.SpecialBitmap MainBitmap = null;
        Render.Sprite MainFrame = null;

        private bool FinishedLoadingComplete = false;
        private bool Loading = false;

        public SkinChanger()
        {
            Common.ExecuteInOnGameUpdate(() => Init());
            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnUpdate += Game_OnNewGameUpdate;
            Obj_AI_Base.OnCreate += Obj_AI_Base_OnCreate;
            new Thread(LoadSpritesAsync).Start();
            SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName).ValueChanged += SkinChanger_ValueChanged;
            SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameLoading").ValueChanged += SkinChanger_PicValueChanged;
            SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameSplash").ValueChanged += SkinChanger_PicValueChanged;
            SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerActive" + ObjectManager.Player.ChampionName).ValueChanged +=
                (sender, args) =>
                    {
                        if (!FinishedLoadingComplete)
                            return;

                        this.UpdateBitmap();
                    };
            Game.OnWndProc += Game_OnWndProc;
        }

        ~SkinChanger()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            Game.OnUpdate -= Game_OnNewGameUpdate;
            Obj_AI_Base.OnCreate -= Obj_AI_Base_OnCreate;
            Skins = null;
        }

        public static bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && SkinChangerMisc.GetActive();
#else
            return SkinChangerMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsSkinChanger");
            if (newMenu == null)
            {
                SkinChangerMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_SKINCHANGER_MAIN"), "SAssembliesMiscsSkinChanger"));
                SkinChangerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName, Language.GetString("MISCS_SKINCHANGER_SKIN")).SetValue(new StringList(GetSkins().ToArray())));
                SkinChangerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsSkinChangerSkinNameLoading", Language.GetString("MISCS_SKINCHANGER_SKIN_LOADING")).SetValue(false));
                SkinChangerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsSkinChangerSkinNameSplash", Language.GetString("MISCS_SKINCHANGER_SKIN_SPLASH")).SetValue(false));
                SkinChangerMisc.CreateActiveMenuItem("SAssembliesMiscsSkinChangerActive" + ObjectManager.Player.ChampionName, () => new SkinChanger());
            }
            return SkinChangerMisc;
        }

        private void Init()
        {
            MainBitmap = new SpriteHelper.SpecialBitmap(new Bitmap(805, 520));
            MainBitmap.AddColoredRectangle(
                new System.Drawing.Point(0, 0),
                new Size(MainBitmap.Bitmap.Width,
                MainBitmap.Bitmap.Height), 
                Color.Black, 90);
            MainBitmap.AddText("Loading Skins...", new System.Drawing.Point(MainBitmap.Bitmap.Width / 2, MainBitmap.Bitmap.Height / 2), Brushes.Orange, true, 18);
            this.MainFrame = new Render.Sprite(MainBitmap.Bitmap,
                new Vector2(Drawing.Width / 2 - MainBitmap.Bitmap.Width,
                Drawing.Height / 2 - MainBitmap.Bitmap.Height / 2));
            this.MainFrame.Position = new Vector2(Drawing.Width / 2 - MainBitmap.Bitmap.Width / 2, Drawing.Height / 2 - MainBitmap.Bitmap.Height / 2 + 100);
            this.MainFrame.VisibleCondition = delegate
            {
                return IsActive();
            };
            this.MainFrame.Add(4);
            MainBitmap.ResetBitmap();
        }

        void Game_OnWndProc(WndEventArgs args)
        {
            if (!IsActive() || !FinishedLoadingComplete)
                return;

            HandleInput((WindowsMessages)args.Msg, Utils.GetCursorPos(), args.WParam);
        }

        private void HandleInput(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            if (message != WindowsMessages.WM_LBUTTONDOWN || !SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameLoading").GetValue<bool>())
            {
                return;
            }
            int active = GetActiveIndex();
            System.Drawing.Point mainPos = new System.Drawing.Point((int)MainFrame.Position.X, (int)MainFrame.Position.Y);
            System.Drawing.Point chamPos = new System.Drawing.Point(
                mainPos.X + ChampSkinGUI.ChampSkins[active].Pos.X,
                mainPos.Y + ChampSkinGUI.ChampSkins[active].Pos.Y);
            if (Common.IsInside(cursorPos, chamPos, ChampSkinGUI.ChampSkins[active].SpriteInfoSmall.Bitmap.Width,
                    ChampSkinGUI.ChampSkins[active].SpriteInfoSmall.Bitmap.Height))
            {
                return;
            }
            for (int i = active - 1; i >= 0; i--)
            {
                chamPos = new System.Drawing.Point(
                    mainPos.X + ChampSkinGUI.ChampSkins[i].Pos.X,
                    mainPos.Y + ChampSkinGUI.ChampSkins[i].Pos.Y);
                if (Common.IsInside(cursorPos, chamPos, ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap.Width,
                    ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap.Height))
                {
                    StringList list =
                        SkinChangerMisc.GetMenuItem(
                            "SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName)
                            .GetValue<StringList>();
                    list.SelectedIndex = i;
                    SkinChangerMisc.GetMenuItem(
                        "SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName).SetValue(list);
                    return;
                }
            }
            for (int i = active + 1; i < ChampSkinGUI.ChampSkins.Length; i++)
            {
                chamPos = new System.Drawing.Point(
                    mainPos.X + ChampSkinGUI.ChampSkins[i].Pos.X,
                    mainPos.Y + ChampSkinGUI.ChampSkins[i].Pos.Y);
                if (Common.IsInside(cursorPos, chamPos, ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap.Width,
                    ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap.Height))
                {
                    StringList list =
                        SkinChangerMisc.GetMenuItem(
                            "SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName)
                            .GetValue<StringList>();
                    list.SelectedIndex = i;
                    SkinChangerMisc.GetMenuItem(
                        "SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName).SetValue(list);
                    return;
                }
            }
        }

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            var unit = sender as Obj_AI_Base;

            if (unit != null && unit.IsValid && unit.Name.Equals(ObjectManager.Player.Name))
            {
                SetSkin(unit, SpriteHelper.ConvertNames(ObjectManager.Player.BaseSkinName), GetActiveIndex());
            }
        }

        private void Game_OnNewGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

            bool finished = true;
            foreach (var champSkin in ChampSkinGUI.ChampSkins)
            {
                if (!champSkin.FinishedLoading)
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

        private void FinishedLoading()
        {
            UpdateBitmap();
            Console.WriteLine("Complete: " + FinishedLoadingComplete);
            FinishedLoadingComplete = true;
        }

        private void UpdateBitmap(int currentId = -1)
        {
            this.MainBitmap.ResetBitmap();
            if (currentId == -1)
            {
                currentId = GetActiveIndex();
            }
            String currentName = SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName).GetValue<StringList>().SList[currentId];
            System.Drawing.Font arialFont = new System.Drawing.Font("Arial", 12);
            Size size = TextRenderer.MeasureText(currentName, arialFont);
            arialFont.Dispose();
            System.Drawing.Point pos =
                new System.Drawing.Point(
                    ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap.Width / 2
                    - ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap.Width / 2,
                    ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap.Height
                    - ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap.Height);
            if (SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameLoading").GetValue<bool>()
                || SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameSplash").GetValue<bool>())
            {
                MainBitmap.AddText(currentName, new System.Drawing.Point(MainBitmap.Bitmap.Width / 2 - size.Width / 2, 0), Brushes.Orange);
            }
            if (SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameSplash").GetValue<bool>())
            {
                MainBitmap.AddColoredRectangle(
                    new System.Drawing.Point(0, 20),
                    new Size(ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap.Width,
                    ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap.Height),
                    Color.Black, 230);
                MainBitmap.AddBitmap(ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap, new System.Drawing.Point(0, 20));
            }
            if (SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameLoading").GetValue<bool>())
            {
                MainBitmap.AddColoredRectangle(
                    pos,
                    new Size(
                        ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap.Width,
                        ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap.Height),
                    Color.Black);
                MainBitmap.AddBitmap(ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap, pos);
            }
            ChampSkinGUI.ChampSkins[currentId].Pos = new System.Drawing.Point(pos.X, pos.Y);
            AddOtherSkins(currentId);
            Common.ExecuteInOnGameUpdate(() =>
            {
                this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);
            });
        }

        private void AddOtherSkins(int currentId = -1)
        {
            if (!SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinNameLoading").GetValue<bool>())
                return;

            ChampSkin[] ChampSkins = ChampSkinGUI.ChampSkins;
            if (currentId == -1)
            {
                currentId = GetActiveIndex();
            }
            System.Drawing.Point pos =
                new System.Drawing.Point(
                    ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap.Width / 2
                    - ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap.Width / 2,
                    ChampSkinGUI.ChampSkins[currentId].SpriteInfoBig.Bitmap.Height
                    - ChampSkinGUI.ChampSkins[currentId].SpriteInfoSmall.Bitmap.Height);

            float count = currentId - 1;
            for (int i = 0; i < currentId; i++)
            {
                int offset = (int)(ChampSkins[i].SpriteInfoSmall.Bitmap.Width + 10 + (30 * count));
                ChampSkins[i].Pos = new System.Drawing.Point(pos.X - offset, pos.Y);
                count--;
                MainBitmap.AddBitmap(ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap, ChampSkins[i].Pos);
            }

            count = ChampSkins.Length - currentId - 2;
            for (int i = ChampSkins.Length - 1; i > currentId; i--)
            {
                int offset = (int)(ChampSkins[i].SpriteInfoSmall.Bitmap.Width + 10 + (30 * count));
                ChampSkins[i].Pos = new System.Drawing.Point(pos.X + offset, pos.Y);
                count--;
                MainBitmap.AddBitmap(ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap, ChampSkins[i].Pos);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || !FinishedLoadingComplete)
                return;

            var mode =
                SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName)
                    .GetValue<StringList>();
            if (!ObjectManager.Player.IsDead && _isDead)
            {
                SetSkin(ObjectManager.Player, SpriteHelper.ConvertNames(ObjectManager.Player.BaseSkinName), GetActiveIndex());
                _isDead = false;
            }
            else if (ObjectManager.Player.IsDead && !_isDead)
            {
                _isDead = true;
            }
            if (mode.SelectedIndex != _lastSkinId)
            {
                _lastSkinId = mode.SelectedIndex;
                SetSkin(ObjectManager.Player, SpriteHelper.ConvertNames(ObjectManager.Player.BaseSkinName), GetActiveIndex());
            }
        }

        private void SetSkin(Obj_AI_Base unit, String name, int id)
        {
            unit.SetSkin(SpriteHelper.ConvertNames(name), id);

            var hero = unit as Obj_AI_Hero;

            if (hero != null && hero.ChampionName.Equals("Lulu") && !hero.IsDead)
            {
                var pix = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(obj => obj.IsValid && obj.Name.Equals("RobotBuddy"));
                if (pix != null && pix.IsValid)
                {
                    pix.SetSkin(pix.BaseSkinName, id);
                }
            }
        }

        void SkinChanger_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            if (!IsActive() || !FinishedLoadingComplete)
                return;

            UpdateBitmap(e.GetNewValue<StringList>().SelectedIndex);
        }

        void SkinChanger_PicValueChanged(object sender, OnValueChangeEventArgs e)
        {
            if (!IsActive() || !FinishedLoadingComplete)
                return;

            Utility.DelayAction.Add(1, () => UpdateBitmap());
        }

        public static String[] GetSkinList(String championName)
        {
            if (Skins.ContainsKey(championName))
            {
                return Skins[championName];
            }
            return new []{""};
        }

        private static void LoadSpritesAsync()
        {
            for (int i = 0; i < ChampSkinGUI.ChampSkins.Length; i++)
            {
                SpriteHelper.DownloadImageRiot(ObjectManager.Player.ChampionName, SpriteHelper.ChampionType.ChampionSkin, SpriteHelper.DownloadType.ChampionSkinSmall, "SkinChanger\\Loading", i);
                ChampSkinGUI.ChampSkins[i].PicName = SpriteHelper.DownloadImageRiot(ObjectManager.Player.ChampionName, SpriteHelper.ChampionType.ChampionSkin, SpriteHelper.DownloadType.ChampionSkinBig, "SkinChanger\\Splash", i);
                ChampSkinGUI.ChampSkins[i].SpriteInfoSmall = new SpriteHelper.SpriteInfo();
                ChampSkinGUI.ChampSkins[i].SpriteInfoSmall.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(SpriteHelper.SpecialBitmap.LoadBitmap(ChampSkinGUI.ChampSkins[i].PicName, @"SkinChanger\\Loading"), 0.4f);
                ChampSkinGUI.ChampSkins[i].SpriteInfoBig = new SpriteHelper.SpriteInfo();
                ChampSkinGUI.ChampSkins[i].SpriteInfoBig.Bitmap = SpriteHelper.SpecialBitmap.ResizeBitmap(SpriteHelper.SpecialBitmap.LoadBitmap(ChampSkinGUI.ChampSkins[i].PicName, @"SkinChanger\\Splash"), 0.7f);
                ChampSkinGUI.ChampSkins[i].FinishedLoading = true;
            }
        }

        private static List<String> GetSkins()
        {
            if (_skins.Count != 0)
                return _skins;
            String version = "";
            List<String> skinList = new List<string>();
            try
            {
                String jsonV = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
                version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(jsonV)["v"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load DDragon Version: Exception: {0}", ex);
                skinList.Add("NOT WORKING!");
                return skinList;
            }
            String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/cdn/" + version + "/data/en_US/champion/" + SpriteHelper.ConvertNames(ObjectManager.Player.ChampionName) + ".json");
            JObject data = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject<Object>(json);
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    skinList.Add(data.SelectToken(data["data"].First.First["skins"].Path + "[" + i + "]")["name"].ToString());
                }
                catch (Exception)
                {
                    break;
                }
            }
            if (skinList.Count == 0)
            {
                skinList.Add("NOT WORKING!");
            }
            else
            {
                ChampSkinGUI.ChampSkins = new ChampSkin[skinList.Count];
                for (int i = 0; i < ChampSkinGUI.ChampSkins.Length; i++)
                {
                    ChampSkinGUI.ChampSkins[i] = new ChampSkin(i, skinList[i]);
                }
            }
            _skins = skinList;
            return skinList;
        }

        private int GetActiveIndex()
        {
            return SkinChangerMisc.GetMenuItem("SAssembliesMiscsSkinChangerSkinName" + ObjectManager.Player.ChampionName).GetValue<StringList>().SelectedIndex;
        }

        class ChampSkinGUI
        {
            public static ChampSkin[] ChampSkins;
        }

        class ChampSkin
        {
            public int Id;
            public SpriteHelper.SpriteInfo SpriteInfoSmall;
            public SpriteHelper.SpriteInfo SpriteInfoBig;
            public String Name;
            public String PicName;
            public System.Drawing.Point Pos;
            public bool FinishedLoading = false;

            public ChampSkin(int id, String name)
            {
                Id = id;
                Name = name;
            }
        }
    }
}
