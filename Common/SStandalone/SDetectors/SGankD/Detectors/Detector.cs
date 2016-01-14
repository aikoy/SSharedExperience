using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SAssemblies.Detectors
{
    class Detector
    {
        public static Menu.MenuItemSettings Detectors = new Menu.MenuItemSettings();

        private Detector()
        {

        }

        ~Detector()
        {

        }

        private static void SetupMainMenu()
        {
            var menu = new LeagueSharp.Common.Menu("SAssemblies", "SAssemblies", true);
            SetupMenu(menu);
            menu.AddToMainMenu();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu, bool useExisitingMenu = false)
        {
            Language.SetLanguage();
            if (!useExisitingMenu)
            {
                Detectors.Menu = Menu.GetSubMenu(menu, "SAssembliesDetectors") ?? menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("DETECTORS_DETECTOR_MAIN"), "SAssembliesDetectors"));
            }
            else
            {
                Detectors.Menu = menu;
            }
            if (!useExisitingMenu)
            {
                Detectors.CreateActiveMenuItem("SAssembliesDetectorsActive");
            }
            return Detectors;
        }
    }
}