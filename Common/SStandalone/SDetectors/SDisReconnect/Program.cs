using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDisReconnect
{
    using System.Drawing;
    using System.Reflection;

    using LeagueSharp.Common;

    using SAssemblies;
    using SAssemblies.Detectors;

    using Menu = SAssemblies.Menu;

    internal class MainMenu : SAssemblies.Menu
    {
        private readonly Dictionary<SAssemblies.Menu.MenuItemSettings, Func<dynamic>> MenuEntries;

        public static MenuItemSettings Detector = new MenuItemSettings();
        public static SAssemblies.Menu.MenuItemSettings DisReconnect = new SAssemblies.Menu.MenuItemSettings();

        public MainMenu()
        {
            MenuEntries = new Dictionary<SAssemblies.Menu.MenuItemSettings, Func<dynamic>>
                              {
                                  { DisReconnect, () => new DisReconnect() },
                              };
        }

        public void UpdateDirEntry(ref SAssemblies.Menu.MenuItemSettings oldMenuItem, SAssemblies.Menu.MenuItemSettings newMenuItem)
        {
            Func<dynamic> save = MenuEntries[oldMenuItem];
            MenuEntries.Remove(oldMenuItem);
            MenuEntries.Add(newMenuItem, save);
            oldMenuItem = newMenuItem;
        }

    }

    internal class Program
    {
        private MainMenu mainMenu;

        private static readonly Program instance = new Program();

        public static void Main(string[] args)
        {
            AssemblyResolver.Init();
            Instance().Load();
        }

        public void Load()
        {
            mainMenu = new MainMenu();
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public static Program Instance()
        {
            return instance;
        }

        private void CreateMenu()
        {
            try
            {
                bool newMenu = false;
                LeagueSharp.Common.Menu menu;
                if (Menu.GetMenu("SAssembliesRoot") == null)
                {
                    menu = new LeagueSharp.Common.Menu("SAssemblies", "SAssembliesRoot", true);
                    newMenu = true;
                }
                else
                {
                    menu = Menu.GetMenu("SAssembliesRoot");
                }

                MainMenu.Detector = Detector.SetupMenu(menu);
                mainMenu.UpdateDirEntry(ref MainMenu.DisReconnect, DisReconnect.SetupMenu(MainMenu.Detector.Menu));

                var globalMenu = Menu.GetSubMenu(menu, "SAwarenessGlobalSettings");
                if (globalMenu == null)
                {
                    Menu.GlobalSettings.Menu =
                    menu.AddSubMenu(new LeagueSharp.Common.Menu("Global Settings", "SAwarenessGlobalSettings"));
                    Menu.GlobalSettings.Menu.AddItem(
                        new MenuItem("SAssembliesGlobalSettingsServerChatPingActive", "Server Chat/Ping").SetValue(false));
                    Menu.GlobalSettings.Menu.AddItem(
                        new MenuItem("SAssembliesGlobalSettingsVoiceVolume", "Voice Volume").SetValue(new Slider(100, 0, 100)));
                }

                if (newMenu)
                {
                    menu.AddItem(new MenuItem("By Screeder", "By Screeder V" + Assembly.GetExecutingAssembly().GetName().Version));
                    menu.AddToMainMenu();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAssemblies: {0}", ex);
                throw;
            }
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            CreateMenu();
            Common.ShowNotification("SDisReconnectDetector loaded!", Color.LawnGreen, 5000);
        }
    }
}
