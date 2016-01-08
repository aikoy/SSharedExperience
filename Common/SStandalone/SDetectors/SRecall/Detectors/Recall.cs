using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = SharpDX.Color;

namespace SAssemblies.Detectors
{
    using System.Threading;
    using System.Windows.Forms;

    using Color = System.Drawing.Color;
    using Menu = SAssemblies.Menu;
    using MenuItem = LeagueSharp.Common.MenuItem;

    internal class Recall
    {
        public static Menu.MenuItemSettings RecallDetector = new Menu.MenuItemSettings(typeof(Recall));

        private List<RecallRender> _recalls = new List<RecallRender>();

        public Recall()
        {
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsEnemy)
                {
                    Packet.S2C.Teleport.Struct p = new Packet.S2C.Teleport.Struct(enemy.NetworkId, Packet.S2C.Teleport.Status.Unknown, Packet.S2C.Teleport.Type.Unknown, 0, 0);
                    _recalls.Add(new RecallRender(p));
                }
            }
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            GameUpdate a = null;
            a = delegate(EventArgs args)
            {
                Init();
                Game.OnUpdate -= a;
            };
            Game.OnUpdate += a;
        }

        ~Recall()
        {
            Obj_AI_Base.OnTeleport -= Obj_AI_Base_OnTeleport;
            _recalls = null;
        }

        public static bool IsActive()
        {
#if DETECTORS
            return Detector.Detectors.GetActive() && RecallDetector.GetActive();
#else
            return RecallDetector.GetActive();
#endif
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            var newMenu = Menu.GetSubMenu(menu, "SAssembliesDetectorsRecall");
            if (newMenu == null)
            {
                RecallDetector.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("DETECTORS_RECALL_MAIN"), "SAssembliesDetectorsRecall"));
                RecallDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsRecallPingTimes", Language.GetString("GLOBAL_PING_TIMES")).SetValue(new Slider(0, 5, 0)));
                RecallDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsRecallLocalPing", Language.GetString("GLOBAL_PING_LOCAL")).SetValue(true));
                RecallDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsRecallChat", Language.GetString("GLOBAL_CHAT")).SetValue(false));
                RecallDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsRecallNotification", Language.GetString("GLOBAL_NOTIFICATION")).SetValue(false));
                RecallDetector.Menu.AddItem(new MenuItem("SAssembliesDetectorsRecallSpeech", Language.GetString("GLOBAL_VOICE")).SetValue(false));
                RecallDetector.CreateActiveMenuItem("SAssembliesDetectorsRecallActive", () => new Recall());
            }
            return RecallDetector;
        }

        private void Init()
        {
            Render.Rectangle rec = new Render.Rectangle(Drawing.Width / 2 - 200 / 2, (int)(Drawing.Height / 1.5f), 200, 10, SharpDX.Color.Black);
            rec.VisibleCondition = delegate
            {
                return IsActive() && _recalls.Any(x => x.Recall.Status == Packet.S2C.Teleport.Status.Start);
            };
            rec.Add();
        }

        private void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            if (!IsActive() && sender.Type != GameObjectType.obj_AI_Hero)
                return;

            Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
            HandleRecall(decoded);
        }

        private void HandleRecall(Packet.S2C.Teleport.Struct recallEx)
        {
            for (int i = 0; i < _recalls.Count; i++)
            {
                Packet.S2C.Teleport.Struct recall = _recalls[i].Recall;
                var obj = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recall.UnitNetworkId);
                var objEx = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recallEx.UnitNetworkId);
                if (obj == null)
                    continue;
                if (obj.NetworkId == objEx.NetworkId)
                {
                    _recalls[i].Recall = recallEx;

                    var percentHealth = (int)((obj.Health / obj.MaxHealth) * 100);
                    if (recallEx.Status == Packet.S2C.Teleport.Status.Start)
                    {
                        String text = (recallEx.Type == Packet.S2C.Teleport.Type.Recall
                            ? Language.GetString("DETECTORS_RECALL_TEXT_RECALLING")
                            : Language.GetString("DETECTORS_RECALL_TEXT_PORTING"));
                        recall.Start = (int)Game.Time;
                        if (
                            RecallDetector.GetMenuItem("SAssembliesDetectorsRecallChat").GetValue<bool>() &&
                            Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                                .GetValue<bool>())
                        {
                            Game.Say(obj.ChampionName + " {0} " + Language.GetString("DETECTORS_RECALL_TEXT_WITH") + " {1} " +
                                Language.GetString("DETECTORS_RECALL_TEXT_HP") + " ({2})", text, (int)obj.Health,
                                percentHealth);
                        }
                        for (int j = 0;
                            j < RecallDetector.GetMenuItem("SAssembliesDetectorsRecallPingTimes").GetValue<Slider>().Value;
                            j++)
                        {
                            if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallLocalPing").GetValue<bool>())
                            {
                                Game.ShowPing(PingCategory.EnemyMissing, obj.ServerPosition, true);
                            }
                            else if (!RecallDetector.GetMenuItem("SAssembliesDetectorsRecallLocalPing").GetValue<bool>() &&
                                        Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                                            .GetValue<bool>())
                            {
                                Game.SendPing(PingCategory.EnemyMissing, obj.ServerPosition);
                            }
                        }
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallSpeech").GetValue<bool>())
                        {
                            Speech.Speak(obj.ChampionName + " " + text);
                        }
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallNotification").GetValue<bool>())
                        {
                            Common.ShowNotification(obj.ChampionName + " " + text + " " + (int)obj.Health + " " +
                                Language.GetString("DETECTORS_RECALL_TEXT_HP") + " (" + percentHealth + ")", System.Drawing.Color.OrangeRed, 3);
                        }
                    }
                    else if (recallEx.Status == Packet.S2C.Teleport.Status.Finish)
                    {
                        String text = (recallEx.Type == Packet.S2C.Teleport.Type.Recall
                            ? Language.GetString("DETECTORS_RECALL_TEXT_RECALLED")
                            : Language.GetString("DETECTORS_RECALL_TEXT_PORTED"));
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallChat").GetValue<bool>() &&
                            Menu.GlobalSettings.GetMenuItem(
                                "SAssembliesGlobalSettingsServerChatPingActive").GetValue<bool>())
                        {
                            Game.Say(obj.ChampionName + " {0} " + Language.GetString("DETECTORS_RECALL_TEXT_WITH") + " {1} " +
                                Language.GetString("DETECTORS_RECALL_TEXT_HP") + " ({2})", text,
                                (int)obj.Health, percentHealth);
                        }
                        for (int j = 0;
                            j < RecallDetector.GetMenuItem("SAssembliesDetectorsRecallPingTimes").GetValue<Slider>().Value;
                            j++)
                        {
                            if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallLocalPing").GetValue<bool>())
                            {
                                Game.ShowPing(PingCategory.Fallback, obj.ServerPosition, true);
                            }
                            else if (!RecallDetector.GetMenuItem("SAssembliesDetectorsRecallLocalPing").GetValue<bool>() &&
                                        Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                                            .GetValue<bool>())
                            {
                                Game.SendPing(PingCategory.Fallback, obj.ServerPosition);
                            }
                        }
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallSpeech").GetValue<bool>())
                        {
                            Speech.Speak(obj.ChampionName + " " + text);
                        }
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallNotification").GetValue<bool>())
                        {
                            Common.ShowNotification(obj.ChampionName + " " + text + " " + (int)obj.Health + " " +
                                Language.GetString("DETECTORS_RECALL_TEXT_HP") + " (" + percentHealth + ")", System.Drawing.Color.Red, 3);
                        }
                    }
                    else
                    {
                        if (
                            RecallDetector.GetMenuItem("SAssembliesDetectorsRecallChat").GetValue<bool>() &&
                            Menu.GlobalSettings.GetMenuItem(
                                "SAssembliesGlobalSettingsServerChatPingActive").GetValue<bool>())
                        {
                            Game.Say(obj.ChampionName + " " + Language.GetString("DETECTORS_RECALL_TEXT_CANCELED") + " " 
                                + Language.GetString("DETECTORS_RECALL_TEXT_WITH") + " {0} " +
                                Language.GetString("DETECTORS_RECALL_TEXT_HP") + "", (int)obj.Health);
                        }
                        for (int j = 0;
                            j < RecallDetector.GetMenuItem("SAssembliesDetectorsRecallPingTimes").GetValue<Slider>().Value;
                            j++)
                        {
                            if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallLocalPing").GetValue<bool>())
                            {
                                Game.ShowPing(PingCategory.Danger, obj.ServerPosition, true);
                            }
                            else if (!RecallDetector.GetMenuItem("SAssembliesDetectorsRecallLocalPing").GetValue<bool>() &&
                                        Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                                            .GetValue<bool>())
                            {
                                Game.SendPing(PingCategory.Danger, obj.ServerPosition);
                            }
                        }
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallSpeech").GetValue<bool>())
                        {
                            Speech.Speak(obj.ChampionName + " " + Language.GetString("DETECTORS_RECALL_TEXT_CANCELED"));
                        }
                        if (RecallDetector.GetMenuItem("SAssembliesDetectorsRecallNotification").GetValue<bool>())
                        {
                            Common.ShowNotification(obj.ChampionName + " " + Language.GetString("DETECTORS_RECALL_TEXT_CANCELED") + " " + (int)obj.Health + " " +
                                Language.GetString("DETECTORS_RECALL_TEXT_HP") + " (" + percentHealth + ")", System.Drawing.Color.LawnGreen, 3);
                        }
                    }
                    return;
                }
            }
        }

        class RecallRender
        {
            public Render.Rectangle Rectangle;
            public Render.Text Text;
            public Render.Line Line;
            public Packet.S2C.Teleport.Struct Recall;

            public RecallRender(Packet.S2C.Teleport.Struct recall)
            {
                var recWidth = 200;
                Recall = recall;
                Rectangle = new Render.Rectangle(Drawing.Width / 2, Drawing.Height / 4, recWidth, 10, SharpDX.Color.Green);
                Rectangle.PositionUpdate += delegate
                {
                    float percent = RecallStatusPercent();
                    var newWidth = (int) (recWidth - (recWidth * percent));
                    if (!Rectangle.Width.Equals(newWidth))
                    {
                        Rectangle.Width = newWidth;
                    }
                    SharpDX.Color newCol = Common.PercentColorRedToGreen(percent);
                    if (!Equals(newCol, Rectangle.Color))
                    {
                        Rectangle.Color = newCol;
                    }
                    return new Vector2(Drawing.Width / 2 - recWidth / 2, Drawing.Height / 1.5f);
                };
                Rectangle.VisibleCondition = delegate
                {
                    return IsActive() && Recall.Status == Packet.S2C.Teleport.Status.Start;
                };
                Rectangle.Add(1);
                Line = new Render.Line(new Vector2(0, 0), new Vector2(0, 0), 1, SharpDX.Color.WhiteSmoke);
                Line.StartPositionUpdate += delegate
                {
                    return new Vector2(Rectangle.X + Rectangle.Width, Rectangle.Y - 5);
                };
                Line.EndPositionUpdate += delegate
                {
                    return new Vector2(Rectangle.X + Rectangle.Width, Rectangle.Y);
                };
                Line.VisibleCondition = delegate
                {
                    SharpDX.Color newCol = new SharpDX.Color(255, 255, 255, (int)(255 - (255 * RecallStatusPercent())));
                    if (!Equals(newCol, Line.Color))
                    {
                        Line.Color = newCol;
                    }
                    return IsActive() && Recall.Status == Packet.S2C.Teleport.Status.Start;
                };
                Line.Add();
                Text = new Render.Text(ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(Recall.UnitNetworkId).ChampionName, 0 ,0, 18, SharpDX.Color.WhiteSmoke);
                Text.PositionUpdate += delegate
                {
                    return new Vector2(Line.Start.X, Line.Start.Y - 15);
                };
                Text.TextUpdate = delegate
                {
                    SharpDX.Color newCol = new SharpDX.Color(255, 255, 255, (int)(255 - (150 * RecallStatusPercent())));
                    if (!Equals(newCol, Text.Color))
                    {
                        Text.Color = newCol;
                    }
                    TimeSpan t = TimeSpan.FromMilliseconds(Recall.Start + Recall.Duration - Environment.TickCount);
                    string time = string.Format("{0:D2},{1:D2}", t.Seconds, t.Milliseconds);
                    return ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(Recall.UnitNetworkId).ChampionName + "\n" + time;
                };
                Text.Centered = true;
                Text.VisibleCondition = delegate
                {
                    return IsActive() && Recall.Status == Packet.S2C.Teleport.Status.Start;
                };
                Text.Add();
            }

            public float RecallStatusPercent()
            {
                float percent = (100f / Recall.Duration * (Environment.TickCount - Recall.Start));
                percent = (percent <= 100 && percent >= 0 ? percent / 100 : 1f);
                return percent;
            }
        }
    }
}
