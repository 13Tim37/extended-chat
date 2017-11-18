using Medieval.Definitions.Tools;
using Medieval.GameSystems.Tools;
using ObjectBuilders.Definitions.Tools;
using Sandbox.Definitions.Equipment;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Inventory;
using Sandbox.ModAPI;
using VRage.Components;
using VRage.Game.Entity;
using VRage.Game.Input;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Input.Input;
using VRage.Network;
using VRage.Systems;
using VRage.Utils;
using VRageMath;
using System.Text;
using System.Collections.Generic;
using System;

namespace Tim76561198006919587
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ExtendedChatBehavior : MySessionComponentBase
    {
        public override void LoadData()
        {
            MyLog.Default.WriteLineAndConsole("INFO: ExtendedChat loaded...");
        }

        private bool init = false;

        private readonly List<IMyPlayer> players = new List<IMyPlayer>(0);
        private readonly Encoding encode = Encoding.Unicode;
        private const ushort PACKET_MSG = 35900;
        private const int DATA_LIMIT = 3994;

        public void Init()
        {
            try
            {
                MyLog.Default.WriteLineAndConsole("ExtendedChat Test");
                init = true;

                MyLog.Default.WriteLineAndConsole("INFO: ExtendedChat Initialised");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!init)
                {
                    if (MyAPIGateway.Session == null)
                        MyLog.Default.WriteLineAndConsole("ExtendedChat: ModAPI nod ready");
                    return;

                    MyLog.Default.WriteLineAndConsole("ExtendedChat: Update");
                    Init();
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        // when local client sends a message
        public override void EnteredMessage(string message, ref bool visible)
        {
            try
            {
                if (!init)
                    return;

                if (message.Equals("/loc", StringComparison.InvariantCultureIgnoreCase))
                {
                    message = "local message";
                    visible = true;
                    MyLog.Default.WriteLineAndConsole("Local Message!");
                    SendMsg(GetTime() + " " + (MyAPIGateway.Session.Player == null ? "(Server)" : MyAPIGateway.Session.Player.DisplayName) + ": " + message, 0);
                }
                else if (message.Equals("/rules", StringComparison.InvariantCultureIgnoreCase))
                {
                    visible = false;
                    MyLog.Default.WriteLineAndConsole("Rules!");
                    message = "These are the rules: ";
                    SendMsg(GetTime() + " " + "(Server)" + ": " + message, 0);
                }
                else if (visible)
                {
                    SendMsg(GetTime() + " " + (MyAPIGateway.Session.Player == null ? "(Server)" : MyAPIGateway.Session.Player.DisplayName) + ": " + message, 0);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        private void SendMsg(string message, ulong sendTo)
        {
            try
            {
                byte[] bytes = encode.GetBytes(message);

                if (bytes.Length > DATA_LIMIT)
                {
                    MyLog.Default.WriteLineAndConsole("WARNING: message exceeded data limit " + DATA_LIMIT + "; message cropped; old message=" + message);
                    bytes = encode.GetBytes(message.Substring(0, (DATA_LIMIT / sizeof(char))));
                }

                if (sendTo == 0)
                {
                    if (MyAPIGateway.Multiplayer.IsServer)
                        ReceivedMessage(bytes);
                    else
                        MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_MSG, bytes, true);
                }
                else
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, bytes, sendTo, true);
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        public void ReceivedMessage(byte[] bytes)
        {
            try
            {
                string msg = encode.GetString(bytes);

                if (MyAPIGateway.Multiplayer.IsServer) // relay the message to the rest of the clients
                {
                    MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                    {
                        if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client))
                        {
                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, bytes, p.SteamUserId, true);
                        }

                        return false; // no reason to add to the list
                    });
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());
            }
        }

        private long GetTime()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}

