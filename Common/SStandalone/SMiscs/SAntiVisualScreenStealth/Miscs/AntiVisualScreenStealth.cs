﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAssemblies.Miscs
{
    class AntiVisualScreenStealth
    {
        public static Menu.MenuItemSettings AntiVisualScreenStealthMisc = new Menu.MenuItemSettings(typeof(AntiVisualScreenStealth));

        public AntiVisualScreenStealth()
        {
            bool available = false;
            List<String> champNames = new List<string>()
            {
                "Akali",
                "Khazix",
                "Leblanc",
                "MonkeyKing",
                "Nocturne",
                "Shaco",
                "Talon",
                "Teemo",
                "Twitch",
                "Vayne",
            };
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (champNames.Contains(hero.ChampionName))
                {
                    Game.OnProcessPacket += Game_OnGameProcessPacket;
                    break;
                }
            }
        }

        ~AntiVisualScreenStealth()
        {
            Game.OnProcessPacket -= Game_OnGameProcessPacket;
        }

        public bool IsActive()
        {
#if MISCS
            return Misc.Miscs.GetActive() && AntiVisualScreenStealthMisc.GetActive();
#else
            return AntiVisualScreenStealthMisc.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesMiscsAntiVisualScreenStealth");
            if (newMenu == null)
            {
                AntiVisualScreenStealthMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_ANTIVISUALSCREENSTEALTH_MAIN"), "SAssembliesMiscsAntiVisualScreenStealth"));
                AntiVisualScreenStealthMisc.CreateActiveMenuItem("SAssembliesMiscsAntiVisualScreenStealthActive", () => new AntiVisualScreenStealth());
            }
            return AntiVisualScreenStealthMisc;
        }

        void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (!IsActive())
                return;

            var reader = new BinaryReader(new MemoryStream(args.PacketData));

            byte packetId = reader.ReadByte();
            List<int> packetIds = new List<int>();
            if (Game.Version.Contains("5.24"))
            {
                packetIds.Add(21);
                packetIds.Add(36);
            }
            if (Game.Version.Contains("6.1"))
            {
                packetIds.Add(77);
            }
            if (Game.Version.Contains("6.2"))
            {
                packetIds.Add(99);
            }
            if (Game.Version.Contains("6.3"))
            {
                packetIds.Add(175);
            }
            foreach (int id in packetIds)
            {
                if (packetId == id)
                {
                    args.Process = false;
                }
            }
        }
    }
}
