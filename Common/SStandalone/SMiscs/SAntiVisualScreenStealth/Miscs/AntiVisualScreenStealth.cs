using System;
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
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                switch (hero.ChampionName)
                {
                    case "Akali":
                        available = true;
                        break;

                    case "Khazix":
                        available = true;
                        break;

                    case "Leblanc":
                        available = true;
                        break;

                    case "MonkeyKing":
                        available = true;
                        break;

                    case "Nocturne":
                        available = true;
                        break;

                    case "Shaco":
                        available = true;
                        break;

                    case "Talon":
                        available = true;
                        break;

                    case "Teemo":
                        available = true;
                        break;

                    case "Twitch":
                        available = true;
                        break;

                    case "Vayne":
                        available = true;
                        break;
                }
                if (available)
                    break;
            }
            if (available)
                Game.OnProcessPacket += Game_OnGameProcessPacket;
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
            AntiVisualScreenStealthMisc.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("MISCS_ANTIVISUALSCREENSTEALTH_MAIN"), "SAssembliesMiscsAntiVisualScreenStealth"));
            AntiVisualScreenStealthMisc.CreateActiveMenuItem("SAssembliesMiscsAntiVisualScreenStealthActive", () => new AntiVisualScreenStealth());
            return AntiVisualScreenStealthMisc;
        }

        void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (!IsActive())
                return;

            var reader = new BinaryReader(new MemoryStream(args.PacketData));

            byte packetId = reader.ReadByte();
            if (packetId == 36 || packetId == 21)
            {
                //reader.ReadInt32();
                //byte visualStealthActive = reader.ReadByte();
                //if (visualStealthActive == 1)
                    args.Process = false;
            }
        }
    }
}
