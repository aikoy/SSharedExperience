using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Web.Script.Serialization;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Sandbox;
using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D9;

//HandleInput Crashing and SequenceLevler

namespace SAssemblies.Miscs
{
    using System.Security.Permissions;

    using Point = System.Drawing.Point;

    internal class AutoLevler
    {
        public static Menu.MenuItemSettings AutoLevlerMisc = new Menu.MenuItemSettings(typeof(AutoLevler));

        private int[] _priority = {0, 0, 0, 0};
        private int[] _sequence;
        private static int _useMode;
        private static List<SequenceLevler> sLevler = new List<SequenceLevler>();
        private int lastGameUpdateTime = 0;
        private SequenceLevlerGUI Gui = new SequenceLevlerGUI();

        private SpriteHelper.SpecialBitmap MainBitmap = null;
        private SpriteHelper.SpecialBitmap SaveBitmap = null;
        private SpriteHelper.SpecialBitmap CancelBitmap = null;
        private Render.Sprite MainFrame = null;

        private static bool FinishedLoadingComplete = false;

        public AutoLevler()
        {
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").ValueChanged += ChangeBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild").ValueChanged += ShowBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild").ValueChanged += NewBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild").ValueChanged += DeleteBuild_OnValueChanged;

            new Thread(LolBuilder.GetLolBuilderData).Start();
            Common.ExecuteInOnGameUpdate(() => Init());

            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
        }

        ~AutoLevler()
        {
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").ValueChanged -= ChangeBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild").ValueChanged -= ShowBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild").ValueChanged -= NewBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild").ValueChanged -= DeleteBuild_OnValueChanged;

            Game.OnUpdate -= Game_OnGameUpdate;
            Game.OnWndProc -= Game_OnWndProc;
            AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;
            sLevler = null;
        }

        public static bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && AutoLevlerMisc.GetActive();
#else
            return AutoLevlerMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            LoadLevelFile();
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsAutoLevler");
            if (newMenu == null)
            {
                Menu.MenuItemSettings tempSettings;
                AutoLevlerMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_AUTOLEVLER_MAIN"), "SAssembliesMiscsAutoLevler"));
                tempSettings = AutoLevlerMisc.AddMenuItemSettings(Language.GetString("MISCS_AUTOLEVLER_PRIORITY_MAIN"), "SAssembliesMiscsAutoLevlerPriority");
                tempSettings.Menu.AddItem(
                    new MenuItem("SAssembliesMiscsAutoLevlerPrioritySliderQ", "Q").SetValue(new Slider(0, 3, 0)));
                tempSettings.Menu.AddItem(
                    new MenuItem("SAssembliesMiscsAutoLevlerPrioritySliderW", "W").SetValue(new Slider(0, 3, 0)));
                tempSettings.Menu.AddItem(
                    new MenuItem("SAssembliesMiscsAutoLevlerPrioritySliderE", "E").SetValue(new Slider(0, 3, 0)));
                tempSettings.Menu.AddItem(
                    new MenuItem("SAssembliesMiscsAutoLevlerPrioritySliderR", "R").SetValue(new Slider(0, 3, 0)));
                tempSettings.Menu.AddItem(
                    new MenuItem("SAssembliesMiscsAutoLevlerPriorityFirstSpells", Language.GetString("MISCS_AUTOLEVLER_PRIORITY_MODE")).SetValue(new StringList(new[]
                    {
                        "Q W E",
                        "Q E W",
                        "W Q E",
                        "W E Q",
                        "E Q W",
                        "E W Q"
                    })));
                tempSettings.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerPriorityFirstSpellsActive", Language.GetString("MISCS_AUTOLEVLER_PRIORITY_MODE_ACTIVE")).SetValue(false));
                tempSettings.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerPriorityActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false).DontSave());
                tempSettings = AutoLevlerMisc.AddMenuItemSettings(Language.GetString("MISCS_AUTOLEVLER_SEQUENCE_MAIN"), "SAssembliesMiscsAutoLevlerSequence");
                tempSettings.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice", Language.GetString("MISCS_AUTOLEVLER_SEQUENCE_BUILD_CHOICE"))
                        .SetValue(GetBuildNames())
                            .DontSave());
                tempSettings.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild", Language.GetString("MISCS_AUTOLEVLER_SEQUENCE_BUILD_LOAD")).SetValue(false)
                        .DontSave());
                tempSettings.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild", Language.GetString("MISCS_AUTOLEVLER_SEQUENCE_CREATE_CHOICE")).SetValue(false)
                        .DontSave());
                tempSettings.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild", Language.GetString("MISCS_AUTOLEVLER_SEQUENCE_DELETE_CHOICE")).SetValue(false)
                        .DontSave());
                tempSettings.Menu.AddItem(
                    new MenuItem("SAssembliesMiscsAutoLevlerSequenceActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false).DontSave());
                AutoLevlerMisc.Menu.AddItem(new MenuItem("SAssembliesMiscsAutoLevlerSMode", Language.GetString("GLOBAL_MODE")).SetValue(new StringList(new[]
                {
                    Language.GetString("MISCS_AUTOLEVLER_MODE_PRIORITY"),
                    Language.GetString("MISCS_AUTOLEVLER_MODE_SEQUENCE"),
                    Language.GetString("MISCS_AUTOLEVLER_MODE_R")
                })));
                AutoLevlerMisc.CreateActiveMenuItem("SAssembliesMiscsAutoLevlerActive", () => new AutoLevler());
            }
            return AutoLevlerMisc;
        }

        private void Init()
        {
            using (var ms = new MemoryStream(SpriteHelper.MyResources["SkillOrderGui".ToLower()]))
            {
                MainBitmap = new SpriteHelper.SpecialBitmap(ms, new[] { 1f, 1f });
            }
            using (var ms = new MemoryStream(SpriteHelper.MyResources["SkillOrderGuiSave".ToLower()]))
            {
                SaveBitmap = new SpriteHelper.SpecialBitmap(ms, new[] { 1f, 1f });
                MainBitmap.AddBitmap(
                    SaveBitmap.Bitmap,
                    new Point(0, MainBitmap.Bitmap.Height - SaveBitmap.Bitmap.Height));
            }
            using (var ms = new MemoryStream(SpriteHelper.MyResources["SkillOrderGuiCancel".ToLower()]))
            {
                CancelBitmap = new SpriteHelper.SpecialBitmap(ms, new[] { 1f, 1f });
                MainBitmap.AddBitmap(
                    CancelBitmap.Bitmap,
                    new Point(MainBitmap.Bitmap.Width - CancelBitmap.Bitmap.Width, MainBitmap.Bitmap.Height - CancelBitmap.Bitmap.Height));
            }
            for (int index = 0; index <= 3; index++)
            {
                int i = 0 + index;

                MainBitmap.AddText(
                    ObjectManager.Player.Spellbook.GetSpell(GetSpellSlot(i)).Name,
                    new Point(30, 40 + (i * 33)),
                    Brushes.LawnGreen);
            }

            this.MainFrame = new Render.Sprite(MainBitmap.Bitmap,
                new Vector2(Drawing.Width / 2 - MainBitmap.Bitmap.Width,
                Drawing.Height / 2 - MainBitmap.Bitmap.Height / 2));
            this.MainFrame.Position = new Vector2(Drawing.Width / 2 - MainBitmap.Bitmap.Width / 2, Drawing.Height / 2 - MainBitmap.Bitmap.Height / 2);
            this.MainFrame.VisibleCondition = delegate
            {
                return IsActive() &&
                        (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild").GetValue<bool>() ||
                        AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild").GetValue<bool>());
            };
            this.MainFrame.Add(2);
            MainBitmap.SetOriginalBitmap(MainBitmap.Bitmap);
            MainBitmap.ResetBitmap();
        }

        void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").ValueChanged -= ChangeBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild").ValueChanged -= ShowBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild").ValueChanged -= NewBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild").ValueChanged -= DeleteBuild_OnValueChanged;
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceActive").ValueChanged -= DeleteBuild_OnValueChanged;

            Game.OnUpdate -= Game_OnGameUpdate;
            Game.OnWndProc -= Game_OnWndProc;
            sLevler = null;
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (!IsActive() && 
                            (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild").GetValue<bool>() ||
                            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild").GetValue<bool>()))
                return;
            if (!FinishedLoadingComplete)
                return;
            HandleInput((WindowsMessages)args.Msg, Utils.GetCursorPos(), args.WParam);
        }

        private void HandleInput(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            HandleMainFrameClick(message, cursorPos, key);
            HandleSaveClick(message, cursorPos, key);
            HandleCancelClick(message, cursorPos, key);
        }

        private void HandleMainFrameClick(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            if (message != WindowsMessages.WM_LBUTTONUP)
            {
                return;
            }
            if (Common.IsInside(cursorPos, MainFrame.Position, MainFrame.Bitmap.Width, MainFrame.Bitmap.Height))
            {
                for (int i = 0; i < 4; i++)
                {
                    var row = SequenceLevlerGUI.SkillBlock[i];
                    for (int j = 0; j < 18; j++)
                    {
                        var column = row[j];
                        if (Common.IsInside(cursorPos, MainFrame.Position + column, SequenceLevlerGUI.SkillBlockSize.Width,
                            SequenceLevlerGUI.SkillBlockSize.Height))
                        {
                            SequenceLevlerGUI.CurrentLevler.Sequence[j] = GetSpellSlot(i);
                            LoadSkills();
                        }
                    }
                }
            }
        }

        private void HandleSaveClick(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            if (message != WindowsMessages.WM_LBUTTONUP)
            {
                return;
            }
            if (Common.IsInside(
                cursorPos, 
                new Point((int)this.MainFrame.Position.X, (int)(this.MainFrame.Position.Y + this.MainBitmap.Bitmap.Height - this.SaveBitmap.Bitmap.Height)), 
                SaveBitmap.Bitmap.Width, 
                SaveBitmap.Bitmap.Height))
            {
                ResetMenuEntries();
                SaveSequence(SequenceLevlerGUI.CurrentLevler.New);
                WriteLevelFile();
            }
        }

        private void HandleCancelClick(WindowsMessages message, Vector2 cursorPos, uint key)
        {
            if (message != WindowsMessages.WM_LBUTTONUP)
            {
                return;
            }
            if (Common.IsInside(
                cursorPos, 
                new Point((int)(this.MainFrame.Position.X + this.MainBitmap.Bitmap.Width - this.CancelBitmap.Bitmap.Width), 
                (int)(this.MainFrame.Position.Y + this.MainBitmap.Bitmap.Height - this.CancelBitmap.Bitmap.Height)), 
                CancelBitmap.Bitmap.Width, 
                CancelBitmap.Bitmap.Height))
            {
                ResetMenuEntries();
            }
        }

        private void ResetMenuEntries()
        {
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild")
                .SetValue(false);
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild")
                .SetValue(false);
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild")
                .SetValue(false);
        }

        private void ChangeBuild_OnValueChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
        {
            if(AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild")
                .GetValue<bool>() || AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild")
                .GetValue<bool>() || AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild")
                .GetValue<bool>())
            {
                onValueChangeEventArgs.Process = false;
                return;
            }

            StringList list = onValueChangeEventArgs.GetNewValue<StringList>();
            SequenceLevler curLevler = null;
            foreach (SequenceLevler levler in sLevler.ToArray())
            {
                if (levler.Name.Contains(list.SList[list.SelectedIndex]))
                {
                    curLevler = levler;
                }
            }
            if (curLevler != null)
            {
                SequenceLevlerGUI.CurrentLevler = new SequenceLevler(curLevler.Name, curLevler.Sequence);
            }
            else
            {
                SequenceLevlerGUI.CurrentLevler = new SequenceLevler();
            }
            LoadSkills();
        }

        private void ShowBuild_OnValueChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
        {
            if (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild")
                .GetValue<bool>() || AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild")
                .GetValue<bool>())
            {
                onValueChangeEventArgs.Process = false;
                return;
            }

            if (onValueChangeEventArgs.GetNewValue<bool>())
            {
                StringList list =
                    AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice")
                        .GetValue<StringList>();
                SequenceLevler curLevler = null;
                foreach (SequenceLevler levler in sLevler.ToArray())
                {
                    if (list.SList[list.SelectedIndex].Equals(""))
                        continue;
                    if (levler.Name.Contains(list.SList[list.SelectedIndex]))
                    {
                        curLevler = levler;
                        break;
                    }
                }
                if (curLevler != null)
                {
                    SequenceLevlerGUI.CurrentLevler = new SequenceLevler(curLevler.Name, curLevler.Sequence);
                }
                else
                {
                    onValueChangeEventArgs.Process = false;
                }
                LoadSkills();
            }
        }

        private void NewBuild_OnValueChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
        {
            if (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild")
                .GetValue<bool>() || AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceDeleteBuild")
                .GetValue<bool>())
            {
                onValueChangeEventArgs.Process = false;
                return;
            }

            if (onValueChangeEventArgs.GetNewValue<bool>())
            {
                SequenceLevlerGUI.CurrentLevler = new SequenceLevler();
                SequenceLevlerGUI.CurrentLevler.Name = GetFreeSequenceName();
                SequenceLevlerGUI.CurrentLevler.ChampionName = ObjectManager.Player.ChampionName;
                LoadSkills();
            }
        }

        private void DeleteBuild_OnValueChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
        {
            if (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceShowBuild")
                .GetValue<bool>() || AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceNewBuild")
                .GetValue<bool>())
            {
                onValueChangeEventArgs.Process = false;
                return;
            }

            if (onValueChangeEventArgs.GetNewValue<bool>())
            {
                DeleteSequence();
                SequenceLevlerGUI.CurrentLevler = new SequenceLevler();
                LoadSkills();
                onValueChangeEventArgs.Process = false;
            }
        }

        private void LoadSkills()
        {
            using (var ms = new MemoryStream(SpriteHelper.MyResources["SkillPoint".ToLower()]))
            {
                SpriteHelper.SpecialBitmap bitmap = new SpriteHelper.SpecialBitmap(ms, new[] { 1f, 1f });
                MainBitmap.ResetBitmap();

                for (int index = 0; index < 18; index++)
                {
                    int i = 0 + index;

                    MainBitmap.AddBitmap(
                        bitmap.Bitmap,
                        SequenceLevlerGUI.GetSpellSlotPosition(GetSpellSlotId(SequenceLevlerGUI.CurrentLevler.Sequence[i]), i));
                }
                this.MainFrame.UpdateTextureBitmap(this.MainBitmap.Bitmap);

                bitmap.Dispose();
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            var stringList = AutoLevlerMisc.GetMenuItem("SAssembliesMiscsAutoLevlerSMode").GetValue<StringList>();
            if (stringList.SelectedIndex == 0)
            {
                _useMode = 0;
                _priority = new[]
                {
                    AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerPrioritySliderQ").GetValue<Slider>().Value,
                    AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerPrioritySliderW").GetValue<Slider>().Value,
                    AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerPrioritySliderE").GetValue<Slider>().Value,
                    AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerPrioritySliderR").GetValue<Slider>().Value
                };
            }
            else if (stringList.SelectedIndex == 1)
            {
                _useMode = 1;
            }
            else
            {
                _useMode = 2;
            }

            Obj_AI_Hero player = ObjectManager.Player;
            if (player.SpellTrainingPoints > 0)
            {
                //TODO: Add level logic// try levelup spell, if fails level another up etc.
                if (_useMode == 0 && AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                    .GetMenuItem("SAssembliesMiscsAutoLevlerPriorityActive").GetValue<bool>())
                {
                    if (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerPriorityFirstSpellsActive").GetValue<bool>())
                    {
                        player.Spellbook.LevelSpell(GetCurrentSpell());
                        return;
                    }
                    SpellSlot[] spellSlots = GetSortedPriotitySlots();
                    for (int slotId = 0; slotId <= 3; slotId++)
                    {
                        int spellLevel = player.Spellbook.GetSpell(spellSlots[slotId]).Level;
                        player.Spellbook.LevelSpell(spellSlots[slotId]);
                        if (player.Spellbook.GetSpell(spellSlots[slotId]).Level != spellLevel)
                            break;
                    }
                }
                else if (_useMode == 1)
                {
                    if (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence")
                        .GetMenuItem("SAssembliesMiscsAutoLevlerSequenceActive").GetValue<bool>())
                    {
                        SpellSlot spellSlot = SequenceLevlerGUI.CurrentLevler.Sequence[player.Level - 1];
                        if (spellSlot == SpellSlot.Q || spellSlot == SpellSlot.W || spellSlot == SpellSlot.E ||
                            spellSlot == SpellSlot.R)
                        {
                            player.Spellbook.LevelSpell(spellSlot);
                        }
                    }
                }
                else
                {
                    if (AutoLevlerMisc.GetMenuItem("SAssembliesMiscsAutoLevlerSMode").GetValue<StringList>().SelectedIndex == 2)
                    {
                        if (ObjectManager.Player.Level == 6 ||
                            ObjectManager.Player.Level == 11 ||
                            ObjectManager.Player.Level == 16)
                        {
                            player.Spellbook.LevelSpell(SpellSlot.R);
                        }
                    }
                }
            }
        }

        public void SetPriorities(int priorityQ, int priorityW, int priorityE, int priorityR)
        {
            _sequence[0] = priorityQ;
            _sequence[1] = priorityW;
            _sequence[2] = priorityE;
            _sequence[3] = priorityR;
        }

        private static void SaveSequence(bool newEntry)
        {
            StringList list = AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").GetValue<StringList>();
            if (SequenceLevlerGUI.CurrentLevler.New)
            {
                SequenceLevlerGUI.CurrentLevler.New = false;
                sLevler.Add(SequenceLevlerGUI.CurrentLevler);
                List<String> temp = list.SList.ToList();
                if (temp.Count == 1)
                {
                    if (temp[0].Equals(""))
                    {
                        temp.RemoveAt(0);
                    }
                    else
                    {
                        list.SelectedIndex += 1;
                    }
                }
                else
                {
                    list.SelectedIndex += 1;
                }
                temp.Add(SequenceLevlerGUI.CurrentLevler.Name);
                list.SList = temp.ToArray();
            }
            else
            {
                foreach (var levler in sLevler.ToArray())
                {
                    if (levler.Name.Equals(SequenceLevlerGUI.CurrentLevler.Name))
                    {
                        sLevler[list.SelectedIndex] = SequenceLevlerGUI.CurrentLevler;
                    }
                }
            }
            AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").SetValue<StringList>(list);
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static void WriteLevelFile()
        {
            string loc = Path.Combine(new[]
            {
                SandboxConfig.DataDirectory, "Assemblies", "cache",
                "SAssemblies", "AutoLevler", "autolevel.conf"
            });
            try
            {
                String output = JsonConvert.SerializeObject(sLevler.Where(x => !x.Web));
                Directory.CreateDirectory(
                    Path.Combine(SandboxConfig.DataDirectory, "Assemblies", "cache", "SAssemblies", "AutoLevler"));
                if (output.Contains("[]"))
                {
                    throw new Exception("[], your latest changes are not getting saved!");
                }
                else
                {
                    File.WriteAllText(loc, output);
                }
            }
            catch (SecurityException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                SecurityException ex2 = (SecurityException)ex.InnerException;
                Console.WriteLine(ex2.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't save autolevel.conf. Ex; {0}", ex);
            }
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private static void LoadLevelFile()
        {
            string loc = Path.Combine(new[]
            {
                SandboxConfig.DataDirectory, "Assemblies", "cache",
                "SAssemblies", "AutoLevler", "autolevel.conf"
            });
            try
            {
                sLevler = JsonConvert.DeserializeObject<List<SequenceLevler>>(File.ReadAllText(loc));
            }
            catch (Exception)
            {
                //Console.WriteLine("Couldn't load autolevel.conf.");
            }
        }

        public static StringList GetBuildNames()
        {
            StringList list = new StringList();
            if (sLevler == null)
            {
                sLevler = new List<SequenceLevler>();
            }
            if (sLevler.Count == 0)
            {
                list.SList = new[] { "" };
            }
            else
            {
                List<String> elements = new List<string>();
                foreach (SequenceLevler levler in sLevler)
                {
                    if (levler.ChampionName.Contains(ObjectManager.Player.ChampionName))
                    {
                        elements.Add(levler.Name);
                    }
                }
                if (elements.Count == 0)
                {
                    list.SList = new[] { "" };
                }
                else
                {
                    list = new StringList(elements.ToArray());
                }
            }
            return list;
        }

        private void DeleteSequence()
        {
            StringList list = AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").GetValue<StringList>();
            foreach (SequenceLevler levler in sLevler.ToArray())
            {
                if (levler.Name.Contains(list.SList[list.SelectedIndex]))
                {
                    sLevler.Remove(levler);
                    List<String> temp = list.SList.ToList();
                    temp.RemoveAt(list.SelectedIndex);
                    if (temp.Count == 0)
                    {
                        temp.Add("");
                    }
                    if (list.SelectedIndex > 0)
                    {
                        list.SelectedIndex -= 1;
                    }
                    list.SList = temp.ToArray();
                    AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerSequence").GetMenuItem("SAssembliesMiscsAutoLevlerSequenceLoadChoice").SetValue<StringList>(list);
                    break;
                }
            }
        }

        private static SpellSlot GetSpellSlot(int id)
        {
            var spellSlot = SpellSlot.Unknown;
            switch (id)
            {
                case 0:
                    spellSlot = SpellSlot.Q;
                    break;

                case 1:
                    spellSlot = SpellSlot.W;
                    break;

                case 2:
                    spellSlot = SpellSlot.E;
                    break;

                case 3:
                    spellSlot = SpellSlot.R;
                    break;
            }
            return spellSlot;
        }

        private static int GetSpellSlotId(SpellSlot spellSlot)
        {
            int id = -1;
            switch (spellSlot)
            {
                case SpellSlot.Q:
                    id = 0;
                    break;

                case SpellSlot.W:
                    id = 1;
                    break;

                case SpellSlot.E:
                    id = 2;
                    break;

                case SpellSlot.R:
                    id = 3;
                    break;
            }
            return id;
        }

        private SpellSlot[] GetSortedPriotitySlots()
        {
            int[] listOld = _priority;
            var listNew = new SpellSlot[4];

            listNew = ToSpellSlot(listOld, listNew);

            return listNew;
        }

        private SpellSlot[] ToSpellSlot(int[] listOld, SpellSlot[] listNew)
        {
            for (int i = 0; i <= 3; i++)
            {
                switch (listOld[i])
                {
                    case 0:
                        listNew[0] = GetSpellSlot(i);
                        break;

                    case 1:
                        listNew[1] = GetSpellSlot(i);
                        break;

                    case 2:
                        listNew[2] = GetSpellSlot(i);
                        break;

                    case 3:
                        listNew[3] = GetSpellSlot(i);
                        break;
                }
            }
            return listNew;
        }

        private SpellSlot GetCurrentSpell()
        {
            SpellSlot[] spellSlot = null;
            switch (AutoLevlerMisc.GetMenuSettings("SAssembliesMiscsAutoLevlerPriority")
                .GetMenuItem("SAssembliesMiscsAutoLevlerPriorityFirstSpells").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    spellSlot = new[] {SpellSlot.Q, SpellSlot.W, SpellSlot.E};
                    break;
                case 1:
                    spellSlot = new[] { SpellSlot.Q, SpellSlot.E, SpellSlot.W };
                    break;
                case 2:
                    spellSlot = new[] { SpellSlot.W, SpellSlot.Q, SpellSlot.E };
                    break;
                case 3:
                    spellSlot = new[] { SpellSlot.W, SpellSlot.E, SpellSlot.Q };
                    break;
                case 4:
                    spellSlot = new[] { SpellSlot.E, SpellSlot.Q, SpellSlot.W };
                    break;
                case 5:
                    spellSlot = new[] { SpellSlot.E, SpellSlot.W, SpellSlot.Q };
                    break;
            }
            return spellSlot[ObjectManager.Player.Level - 1];
        }

        private SpellSlot ConvertSpellSlot(String spell)
        {
            switch (spell)
            {
                case "Q":
                    return SpellSlot.Q;

                case "W":
                    return SpellSlot.W;

                case "E":
                    return SpellSlot.E;

                case "R":
                    return SpellSlot.R;

                default:
                    return SpellSlot.Unknown;
            }
        }

        private String GetFreeSequenceName()
        {
            List<int> endings = new List<int>();
            List<SequenceLevler> sequences = new List<SequenceLevler>();
            for (int i = 0; i < sLevler.Count; i++)
            {
                if (sLevler[i].ChampionName.Contains(ObjectManager.Player.ChampionName))
                {
                    String ending = sLevler[i].Name.Substring(ObjectManager.Player.ChampionName.Length);
                    try
                    {
                        endings.Add(Convert.ToInt32(ending));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            for (int i = 0; i < 10000; i++)
            {
                if (!endings.Contains(i))
                {
                    return ObjectManager.Player.ChampionName + i;
                }
            }
            return ObjectManager.Player.ChampionName + 0;
        }

        private class LolBuilder
        {

            public static void GetLolBuilderData()
            {
                String lolBuilderData = null;
                try
                {
                    lolBuilderData = Website.GetWebSiteContent("http://lolbuilder.net/" + ObjectManager.Player.ChampionName);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                String patternSkillOrder = "window.skillOrder\\[(.*?)\\] = \\[(.*?)\\];";
                String patternSkillName = "<a href=\"#ability-order-(.*?)\">([A-Z]*?)(.*?)<div";

                for (int i = 0; ; i++)
                {
                    String matchSkillOrder = Website.GetMatch(lolBuilderData, patternSkillOrder, i, 2);
                    if (matchSkillOrder.Equals(""))
                    {
                        break;
                    }
                    String matchSkillName = Website.GetMatch(lolBuilderData, patternSkillName, i, 3);
                    String[] splitSkillOrder = matchSkillOrder.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    SpellSlot[] spellSlots = new SpellSlot[18];
                    for (int j = 0; j < splitSkillOrder.Length; j++)
                    {
                        var skill = splitSkillOrder[j];
                        try
                        {
                            spellSlots[j] = (SpellSlot)(Convert.ToInt32(skill) - 1);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Cannot convert SkillOrder to SpellSlot {0}: {1}", skill, e);
                        }
                    }
                    SequenceLevler seqLevler = new SequenceLevler(ObjectManager.Player.ChampionName + " LolBuilder " + matchSkillName, spellSlots, true);
                    seqLevler.New = true;
                    SequenceLevlerGUI.CurrentLevler = seqLevler;
                    Common.ExecuteInOnGameUpdate(() => SaveSequence(seqLevler.New));
                }
                FinishedLoadingComplete = true;
            }
        }

        [Serializable]
        private class SequenceLevler
        {
            public String Name;
            public String ChampionName;
            public SpellSlot[] Sequence = new SpellSlot[18];
            public bool New = true;
            public bool Web = false;

            public SequenceLevler(String name, SpellSlot[] sequence, bool web = false)
            {
                Name = name;
                Sequence = sequence;
                New = false;
                ChampionName = ObjectManager.Player.ChampionName;
                Web = web;
            }

            public SequenceLevler()
            {

            }
        }

        private class SequenceLevlerGUI
        {
            public static SequenceLevler CurrentLevler = new SequenceLevler();
            public static Vector2 SkillStart = new Vector2(225, 45);
            public static Vector2 SkillIncrement = new Vector2(32.5f, 33); //35,35
            public static Vector2[][] SkillBlock;
            public static Size SkillBlockSize = new Size(28, 28); //30,30

            static SequenceLevlerGUI()
            {
                Vector2[][] list = new Vector2[4][];
                for (int j = 0; j < 4; j++)
                {
                    list[j] = new Vector2[18];
                    for (int i = 0; i < 18; i++)
                    {
                        list[j][i] = new Vector2(215 + ((i * SkillBlockSize.Width) + (i * 5)), 35 + ((j * SkillBlockSize.Height) + (j * 5)));
                    }
                }
                SkillBlock = list;
            }

            public static Point GetSpellSlotPosition(int row, int column)
            {
                return new Point((int)(SkillStart.X + (SkillIncrement.X * column)), (int)(SkillStart.Y + (SkillIncrement.Y * row)));
            }
        }
    }
}
