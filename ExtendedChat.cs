using Medieval;
//using ObjectBuilders.Definitions.Tools;
//using Sandbox.Definitions.Equipment;
//using Sandbox.Game.Entities;
//using Sandbox.Game.EntityComponents.Character;
//using Sandbox.Game.Gui;
//using Sandbox.Game.Inventory;
using Sandbox.ModAPI;
//using VRage.Components;
//using VRage.Game.Entity;
//using VRage.Game.Input;
using VRage.Game.ModAPI;
using VRage.Game.Components;
//using VRage.Input.Input;
//using VRage.Network;
//using VRage.Systems;
using VRage.Utils;
using VRageMath;
using System.Text;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.IO;


namespace Tim76561198006919587
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ExtendedChatBehavior : MySessionComponentBase
    {
        public override void LoadData()
        {
            WriteLog("INFO: ExtendedChat loaded...");
        }

        private bool init = false;

        private const string RULES = "rules.txt";
        private List<string> rulesStrings = new List<string>();

        private static TextWriter writer = null;
        private static TextReader reader = null;

        private readonly List<IMyPlayer> players = new List<IMyPlayer>(0);
        private readonly Encoding encode = Encoding.Unicode;
        private const ushort PACKET_MSG = 35900;
        private const int DATA_LIMIT = 3994;

        private string serverName = "[SERVER]";
        private string version = "Extended Chat Version: 0.3.5, date: 6th August 2017";
        private bool logDisabled = true;

        public void Init()
        {
            try
            {
                init = true;

                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(RULES, typeof(ExtendedChatBehavior)))
                {
                    readRules();
                }
                else
                {
                    WriteRulesDefault();
                }

                WriteLog("INFO: ExtendedChat Initialised");

                MyAPIGateway.Utilities.MessageEntered += EnteredMessage;
                MyAPIGateway.Multiplayer.RegisterMessageHandler(PACKET_MSG, ReceivedMsg);
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Utilities.MessageEntered -= EnteredMessage;
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(PACKET_MSG, ReceivedMsg);
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!init)
                {
                    if (MyAPIGateway.Session == null)
                    {
                        WriteLog("ExtendedChat: ModAPI not ready");
                        return;
                    }

                    WriteLog("ExtendedChat: Update");
                    Init();
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }

        // when local client sends a message
        public void EnteredMessage(string message, ref bool visible)
        {
            visible = false;

            try
            {
                //WriteLog("EnteredMessage: " + message + ", visible: " + visible.ToString());

                if (!init)
                    return;

                string[] splitMessage = message.Split(' '); 

                if (splitMessage[0].Equals("/loc", StringComparison.InvariantCultureIgnoreCase))
                {
                    visible = false;
                    SendMsg((MyAPIGateway.Session.Player.DisplayName) + ":" + message, 0);
                }
                else if (splitMessage[0].Equals("/pm", StringComparison.InvariantCultureIgnoreCase))
                {
                    visible = false;
                    SendMsg((MyAPIGateway.Session.Player.DisplayName) + ":" + message, 0);
                }
                else if (message.Equals("/rules", StringComparison.InvariantCultureIgnoreCase) || message == "/r" || message == "/R")
                {
                    visible = false;
                    //message = "These are the rules: (WIP)\n" +
                    //"1: This is rule 1\n" + 
                    //"2: This is rule 2\n" +
                    //"3: This is rule 3";
                    //MyAPIGateway.Utilities.ShowMessage("[SERVER]", message);
                    SendMsg((MyAPIGateway.Session.Player.DisplayName) + ":" + message, 0);
                }
                else if (message.Equals("/version", StringComparison.InvariantCultureIgnoreCase))
                {
                    visible = false;
                    message = version;
                    MyAPIGateway.Utilities.ShowNotification(message, 4000, null, Color.DarkBlue);
                }
                else if (message.Equals("/help", StringComparison.InvariantCultureIgnoreCase))
                {
                    visible = false;
                    message = "You can use the following commands;\n" +
                        "/help: Show the help menu\n" +
                        "/rules: Show the server rules\n" +
                        "/loc (/l): Send a local message\n" +
                        "/yell (/y): Yell a message in local chat\n" +
                        "/whisper (/w): Whisper a message in local chat\n" +
                        "/broadcast: Send a notification to all players\n" +
                        "/pm \"<playerName>\" <message> to send a private message\n" +
                        "/house (/h): Send a message to your house members\n" +
                        "/version: Display version of mod";

                    MyAPIGateway.Utilities.ShowMessage(serverName, message);
                }
                else
                {
                    SendMsg((MyAPIGateway.Session.Player == null ? serverName : MyAPIGateway.Session.Player.DisplayName) + ":" + message, 0);
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }

        private void SendMsg(string message, ulong sendTo)
        {
            try
            {
                //WriteLog("SendMsg: " + message + ", to: " + sendTo.ToString());

                byte[] bytes = encode.GetBytes(message);

                if (bytes.Length > DATA_LIMIT)
                {
                    WriteLog("WARNING: message exceeded data limit " + DATA_LIMIT + "; message cropped; old message=" + message);
                    bytes = encode.GetBytes(message.Substring(0, (DATA_LIMIT / sizeof(char))));
                }

                if (sendTo == 0)
                {
                    //WriteLog("SM IsServer: " + MyAPIGateway.Multiplayer.IsServer.ToString());
                    if (MyAPIGateway.Multiplayer.IsServer)
                        ReceivedMsg(bytes);
                    else
                        MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_MSG, bytes, true);
                }
                else
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, bytes, sendTo, true);
                    byte[] serverBytes = encode.GetBytes("A message was sent to " + sendTo.ToString());
                    MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_MSG, serverBytes, true);
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }

        public bool detectLocalPlayer(IMyPlayer sender, IMyPlayer receiver, int distance)
        {
            WriteLog("Player: " + receiver.DisplayName + ", steam:" + receiver.SteamUserId + ", PID:" + receiver.PlayerID + ", IID:" + receiver.IdentityId + ", admin:" + receiver.IsAdmin.ToString() + ", IsServerPlayer:" + MyAPIGateway.Multiplayer.IsServerPlayer(receiver.Client).ToString());
            if (sender.SteamUserId != receiver.SteamUserId)
            {
                bool inRange = true;

                Vector3D senderPosition = sender.GetPosition();
                Vector3D receiverPosition = receiver.GetPosition();

                double sx = senderPosition.X;
                double sy = senderPosition.Y;
                double sz = senderPosition.Z;

                double rx = receiverPosition.X;
                double ry = receiverPosition.Y;
                double rz = receiverPosition.Z;

                int x = (int)Math.Round(Math.Abs(sx - rx));
                int y = (int)Math.Round(Math.Abs(sy - ry));
                int z = (int)Math.Round(Math.Abs(sz - rz));

                if (x > distance)
                {
                    inRange = false;
                }
                else if (y > distance)
                {
                    inRange = false;
                }
                else if (z > distance)
                {
                    inRange = false;
                }

                string distanceMsg = "[Distance XYZ: " + x.ToString() + "-" + y.ToString() + "-" + z.ToString() + "]";

                WriteLog("Sender position: " + senderPosition.ToString() + ", receiver position: " + receiverPosition.ToString() + ", distance: " + distanceMsg);

                if (inRange)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }

        }

        private IMyPlayer getSender(string senderName)
        {
            IMyPlayer sender = null;

            MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
            {
                if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client))
                {
                    WriteLog("Sender: " + senderName + ", cPlayerName: " + p.DisplayName);

                    if (p.DisplayName.Equals(senderName))
                    {
                        sender = p;
                        return true;
                    }
                }
                return false;

            });
            return sender;
        }

        public void ReceivedMsg(byte[] bytes)
        {
            try
            {
                string msg = encode.GetString(bytes);

                WriteLog("ReceivedMsg: " + msg);

                WriteLog("RM IsServer: " + MyAPIGateway.Multiplayer.IsServer.ToString());
                //WriteLog("MultiplayerActive: " + MyAPIGateway.Multiplayer.MultiplayerActive.ToString());

                String senderName = null;
                String[] splitMsg = msg.Split(':');
                senderName = splitMsg[0];
                String message = splitMsg[1];

                for (int k = 2; k < splitMsg.Length; k++)
                {
                    message += (":" + splitMsg[k]);
                }

                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    // If message is received by the server

                    IMyPlayer sender = null;

                    WriteLog("senderName: " + senderName);

                    //WriteLog("Message: " + message);
                    //WriteLog("Message[0] == " + message[0]);
                    //WriteLog("Message[0] == / ? " + (message[0] == ('/')).ToString());

                    // If user types the command character
                    if (message[0] == ('/'))
                    {
                        WriteLog("COMMAND");
                        String[] messageContent = message.Split(' ');
                        string command = messageContent[0];

                        //WriteLog("Command == " + messageContent[0]);
                        //WriteLog("Command == /loc ? " + (messageContent[0] == "/loc").ToString());

                        // See if the user typed a command
                        if (command == "/l" || command == "/loc" || command == "/y" || command == "/yell" || command == "/w" || command == "/whisper")
                        {

                            int distance = 40;
                            string distanceString = "[LOCAL] ";

                            if(command == "/y" || command == "/yell")
                            {
                                distance = 70;
                                distanceString = "[YELL] ";
                            }
                            else if (command == "/w" || command == "/whisper")
                            {
                                distance = 3;
                                distanceString = "[WHISPER] ";
                            }                            
                            
                            sender = getSender(senderName);

                            WriteLog("Sender steam ID: " + sender.SteamUserId.ToString());
                            WriteLog("Sender null? " + (sender == null).ToString());

                            if (sender != null)
                            {
                                MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                                {
                                    if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client))
                                    {
                                        if (detectLocalPlayer(sender, p, distance))
                                        {
                                            // Remove the command from chat
                                            messageContent[0] = "";
                                            string localMessage = distanceString + sender.DisplayName + ":" + String.Join(" ", messageContent);
                                            byte[] localMessageBytes = encode.GetBytes(localMessage);
                                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, localMessageBytes, p.SteamUserId, true);
                                            WriteLog("Sending local message: " + String.Join(" ", messageContent) + ", from: " + sender.DisplayName + ", to: " + p.DisplayName);
                                        }
                                    }
                                    return false;
                                });
                            }
                        }
                        else if (command == "/broadcast" || command == "/bc")
                        {
                            sender = getSender(senderName);

                            if (sender != null && (sender.IsAdmin || sender.IsPromoted))
                            {
                                MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                                {
                                    if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client))
                                    {
                                        string localMessage = sender.DisplayName + ":" + String.Join(" ", messageContent);
                                        byte[] localMessageBytes = encode.GetBytes(localMessage);
                                        MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, localMessageBytes, p.SteamUserId, true);
                                        WriteLog("Sending broadcast: " + String.Join(" ", messageContent) + ", from: " + sender.DisplayName + ", to: " + p.DisplayName);
                                    }
                                    return false;
                                });
                            }
                           
                        }
                        else if (command == "/h" || command == "/house")
                        {
                            sender = getSender(senderName);
                            Medieval.GameSystems.Factions.MyFaction senderFaction = null;
                            IReadOnlyDictionary<long, Medieval.GameSystems.Factions.MyFaction> factions = getFactions();

                            senderFaction = Medieval.GameSystems.Factions.MyFactionManager.GetPlayerFaction(sender.PlayerID);
                            WriteLog("Sender faction: " + senderFaction.FactionName);

                            if (senderFaction != null)
                            {
                                foreach (var member in senderFaction.Members)
                                {
                                    MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                                    {
                                        if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client) && (p.PlayerID == member.Key))
                                        {
                                            messageContent[0] = "";
                                            string houseMessage = "[HOUSE] " + sender.DisplayName + ":" + String.Join(" ", messageContent);
                                            byte[] houseMessageBytes = encode.GetBytes(houseMessage);
                                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, houseMessageBytes, p.SteamUserId, true);
                                            WriteLog("Sending house message: " + String.Join(" ", messageContent) + ", from: " + sender.DisplayName + ", to: " + p.DisplayName);
                                        }
                                        return false;
                                    });
                                }
                                //string sentHouseMessage = "[SERVER]:You sent a message to your house.";
                                //byte[] sentHouseMessageBytes = encode.GetBytes(sentHouseMessage);
                                //MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, sentHouseMessageBytes, sender.SteamUserId, true);
                            }
                            else
                            {
                                string noHouseMessage = serverName + ":" + "You are not in a house!";
                                byte[] noHouseMessageBytes = encode.GetBytes(noHouseMessage);
                                MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, noHouseMessageBytes, sender.SteamUserId, true);
                            }

                        }
                        else if (command == "/hb" || command == "/housebroadcast")
                        {
                            sender = getSender(senderName);
                            Medieval.GameSystems.Factions.MyFaction senderFaction = null;
                            IReadOnlyDictionary<long, Medieval.GameSystems.Factions.MyFaction> factions = getFactions();

                            senderFaction = Medieval.GameSystems.Factions.MyFactionManager.GetPlayerFaction(sender.PlayerID);
                            WriteLog("Sender faction: " + senderFaction.FactionName);

                            if (senderFaction != null)
                            {
                                WriteLog("House broadcast sender rank: " + senderFaction.GetMemberRank(sender.PlayerID));
                                WriteLog("Faction full permission string: " + Medieval.GameSystems.Factions.FactionPermission.DefaultPermissions_Full.ToString());
                                if (senderFaction.GetMemberRank(sender.PlayerID).ToString().Split(':')[1].Trim() == Medieval.GameSystems.Factions.FactionPermission.DefaultPermissions_Full.ToString() || senderFaction.GetMemberRank(sender.PlayerID).ToString().Split(':')[1].Trim() == Medieval.GameSystems.Factions.FactionPermission.DefaultPermissions_Leader.ToString())
                                {
                                    foreach (var member in senderFaction.Members)
                                    {
                                        MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                                        {
                                            if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client) && (p.PlayerID == member.Key))
                                            {
                                                string houseMessage = sender.DisplayName + ":" + String.Join(" ", messageContent);
                                                byte[] houseMessageBytes = encode.GetBytes(houseMessage);
                                                MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, houseMessageBytes, p.SteamUserId, true);
                                                WriteLog("Sending house message: " + String.Join(" ", messageContent) + ", from: " + sender.DisplayName + ", to: " + p.DisplayName);
                                            }
                                            return false;
                                        });
                                    }
                                    //string sentHouseMessage = "[SERVER]:You sent a message to your house.";
                                    //byte[] sentHouseMessageBytes = encode.GetBytes(sentHouseMessage);
                                    //MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, sentHouseMessageBytes, sender.SteamUserId, true);
                                }
                                else
                                {
                                    string noHousePermission = serverName + ":" + "You do not have the permission to send a house broadcast!";
                                    byte[] noHousePermissionBytes = encode.GetBytes(noHousePermission);
                                    MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, noHousePermissionBytes, sender.SteamUserId, true);
                                }
                            }
                            else
                            {
                                string noHouseMessage = serverName + ":" + "You are not in a house!";
                                byte[] noHouseMessageBytes = encode.GetBytes(noHouseMessage);
                                MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, noHouseMessageBytes, sender.SteamUserId, true);
                            }
                        }
                        else if (command == "/pm")
                        {
                            sender = getSender(senderName);

                            var reg = new Regex("\"(.*?)\"");
                            var match = reg.Match(message);

                            if (match != null && match.ToString() != "")
                            {
                                string receiver = match.ToString().Trim(new Char[] { ('"') });
                                WriteLog("PM receiver: '" + receiver + "'");

                                bool foundPlayer = false;

                                MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                                {
                                    if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client))
                                    {
                                        if (p.DisplayName.Equals(receiver, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            foundPlayer = true;
                                            int quoteCount = 0;
                                            messageContent[0] = "";
                                            for (int x = 1; x < messageContent.Length-1; x++)
                                            {
                                                if (messageContent[x][0] == '"' || messageContent[x][messageContent[x].Length -1] == '"')
                                                {
                                                    messageContent[x] = "";
                                                    if (quoteCount > 1)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                            string privateMessage = "[PM] " + sender.DisplayName + ":" + String.Join(" ", messageContent);
                                            byte[] privateMessageBytes = encode.GetBytes(privateMessage);
                                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, privateMessageBytes, p.SteamUserId, true);

                                            string sentPrivateMessage = serverName + ":" + "You sent a private message to: " + p.DisplayName + "\n" + String.Join(" ", messageContent);
                                            byte[] sentPrivateMessageBytes = encode.GetBytes(sentPrivateMessage);
                                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, sentPrivateMessageBytes, sender.SteamUserId, true);

                                            WriteLog("Sending private message: " + String.Join(" ", messageContent) + ", from: " + sender.DisplayName + ", to: " + p.DisplayName);
                                        }
                                    }
                                    return false;
                                });

                                if (!foundPlayer)
                                {
                                    string notFoundMessage = serverName + ":" + "Could not find a player with that name!";
                                    byte[] notFoundMessageBytes = encode.GetBytes(notFoundMessage);
                                    MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, notFoundMessageBytes, sender.SteamUserId, true);
                                }

                            }
                            else
                            {
                                string wrongFormatMessage = serverName + ":" + "Wrong format, should be: \n/pm \"<playerName>\" <message>";
                                byte[] wrongFormatMessageBytes = encode.GetBytes(wrongFormatMessage);
                                MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, wrongFormatMessageBytes, sender.SteamUserId, true);
                            }
                        }
                        else if (command == "/rules" || command == "/r" || command == "/R")
                        {
                            sender = getSender(senderName);

                            int currentPageNum = 1;
                            if (messageContent.Length > 1)
                            {
                                // If user included an input variable check that it is a valid integer.
                                if (int.TryParse(messageContent[1], out currentPageNum))
                                {

                                }
                                else
                                {
                                    WriteLog("Failed to convert page num!");
                                    string invalidRulesMessage = "[SERVER]: " + "Incorrect format, should be\n/rules <pageNumber>";
                                    byte[] invalidRulesMessageBytes = encode.GetBytes(invalidRulesMessage);
                                    MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, invalidRulesMessageBytes, sender.SteamUserId, true);
                                    return;
                                }
                            }

                            WriteLog("Sending rules to " + sender.DisplayName);

                            int rulesPagesCount = (rulesStrings.Count + 4) / 5;

                            if (currentPageNum > rulesPagesCount) {
                                WriteLog("Page value out of range!");
                                string invalidRulesMessage = "[SERVER]: " + "Page value out of range!";
                                byte[] invalidRulesMessageBytes = encode.GetBytes(invalidRulesMessage);
                                MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, invalidRulesMessageBytes, sender.SteamUserId, true);
                                return;
                            }

                            int ruleCounter = (currentPageNum - 1) * 5;
                            List<string> rulesStringsPage;

                            if (rulesStrings.Count > 5)
                            {
                                message = "[SERVER]: " + "Rules page " + currentPageNum.ToString() + " out of " + rulesPagesCount.ToString() + "\n";

                                if (currentPageNum < rulesPagesCount)
                                {
                                    message += "Type /rules " + (currentPageNum + 1).ToString() + " to see the next page\n";
                                }
                            }
                            else
                            {
                                message = serverName + ":" + "These are the rules;\n";
                            }

                            int startIndex = ((currentPageNum - 1) * 5);
                            if (startIndex < 0)
                            {
                                startIndex = 0;
                            }

                            if (currentPageNum < rulesPagesCount)
                            {
                                rulesStringsPage = rulesStrings.GetRange(startIndex, 5);
                            } else
                            {
                                rulesStringsPage = rulesStrings.GetRange(startIndex, rulesStrings.Count % 5);
                            }

                            foreach (string rule in rulesStringsPage)
                            {
                                ruleCounter++;
                                //WriteLog("RULE: " + rule);
                                message += (ruleCounter.ToString() + ". " + rule + "\n");
                            }
                                                       
                            byte[] rulesMessageBytes = encode.GetBytes(message);
                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, rulesMessageBytes, sender.SteamUserId, true);
                        }
                        //else if (command == "/wave")
                        //{
                        //    sender.Controller.ControlledEntity.Entity.
                        //}
                        else if (command == "/serverversion" || command == "/sv")
                        {
                            // Allows users to see what version of the mod the server is running
                            sender = getSender(senderName);
                            string serverVersionMessage = serverName + ":" + version;
                            byte[] serverVersionMessageBytes = encode.GetBytes(serverVersionMessage);
                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, serverVersionMessageBytes, sender.SteamUserId, true);
                        }
                        else
                        {
                            sender = getSender(senderName);
                            string invalidMessage = serverName + ":" + "Invalid command, type /help for a list of commands";
                            byte[] invalidMessageBytes = encode.GetBytes(invalidMessage);
                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, invalidMessageBytes, sender.SteamUserId, true);
                        }
                    }
                    else
                    {
                        // relay the message to the rest of the clients
                        MyAPIGateway.Players.GetPlayers(players, delegate (IMyPlayer p)
                        {
                            //WriteLog("Player: " + p.DisplayName + ", steam:" + p.SteamUserId + ", PID:" + p.PlayerID + ", IID:" + p.IdentityId + ", admin:" + p.IsAdmin.ToString() + ", IsServerPlayer:" + MyAPIGateway.Multiplayer.IsServerPlayer(p.Client).ToString());

                            // If player is not running the server send message to them.
                            if (!MyAPIGateway.Multiplayer.IsServerPlayer(p.Client))
                            {
                                // These messages are sent over the server to each player
                                MyAPIGateway.Multiplayer.SendMessageTo(PACKET_MSG, bytes, p.SteamUserId, true);
                            }
                            
                            return false; // no reason to add to the list
                        });

                    }
                }
                else
                {
                    //WriteLog("Message: " + message);
                    //WriteLog("Message[0] == " + message[0]);
                    //WriteLog("Message[0] == / ? " + (message[0] == ('/')).ToString());

                    if (message[0] == ('/'))
                    {
                        String[] messageContent = message.Split(' ');
                        string command = messageContent[0];

                        //WriteLog("COMMAND: " + command);

                        if (command == "/broadcast" || command == "/bc")
                        {
                            //WriteLog("Receiveing broadcast: " + message + ", from: " + senderName);
                            messageContent[0] = "";
                            
                            if (senderName != null)
                            {
                                MyAPIGateway.Utilities.ShowNotification(senderName + ": " + String.Join(" ", messageContent), 8000, null, Color.Red);
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowNotification(String.Join(" ", messageContent), 8000, null, Color.Red);
                            }

                            return;
                        }
                        else if (command == "/housebroadcast" || command == "/hb")
                        {
                            messageContent[0] = "";

                            if (senderName != null)
                            {
                                MyAPIGateway.Utilities.ShowNotification("[HOUSE]" + senderName + ": " + String.Join(" ", messageContent), 8000, null, Color.White);
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowNotification(String.Join(" ", messageContent), 8000, null, Color.White);
                            }

                            return;
                        }
                    }

                    // Message received from server
                    if (senderName != null)
                    {
                        MyAPIGateway.Utilities.ShowMessage(senderName, message);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage(serverName, msg);
                    }
               
                                       
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }

        private IReadOnlyDictionary<long, Medieval.GameSystems.Factions.MyFaction> getFactions()
        {
            // Returns a dictionary with {factionKey: faction}
            IReadOnlyDictionary<long, Medieval.GameSystems.Factions.MyFaction> factions = Medieval.GameSystems.Factions.MyFactionManager.Instance.Factions;
            return factions;
        }

        private void readRules()
        {
            try
            {
                rulesStrings.Clear();

                if (reader == null)
                {
                    reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(RULES, typeof(ExtendedChatBehavior));
                }

                bool reading = true;
                do
                {
                    string message = reader.ReadLine();
                    if (message != null)
                    {
                        rulesStrings.Add(message);
                    }
                    else
                    {
                        reading = false;
                        reader.Close();
                        reader = null;
                    }

                } while (reading);
            }
            catch (Exception)
            {
                WrongFormat();
            }

        }

        private void WrongFormat()
        {
            reader.Close();
            reader = null;
            WriteRulesDefault();
            rulesStrings.Clear();
        }

        private void WriteRulesDefault()
        {
            try
            {
                if (writer == null)
                {
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(RULES, typeof(ExtendedChatBehavior));
                }
                else
                {
                    writer.Close();
                    writer = null;
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(RULES, typeof(ExtendedChatBehavior));
                }
                writer.WriteLine("This is rule 1");
                writer.WriteLine("This is rule 2");
                writer.WriteLine("This is rule 3");
                writer.Flush();
                writer.Close();
                writer = null;
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }

        }

        private long GetTime()
        {
            return DateTime.UtcNow.Ticks;
        }

        private void WriteLog(string msg)
        {
            if (!logDisabled)
            {
                MyLog.Default.WriteLineAndConsole(msg);
            }
        }   
    }
}