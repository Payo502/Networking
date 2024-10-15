using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Text;
using System.Linq;
using server;
using System.Net.NetworkInformation;

class TCPServerSample
{
    public static int clientCounter = 0;
    private static Dictionary<TcpClient, string> clientNicknames = new Dictionary<TcpClient, string>();
    private static List<TcpClient> clients = new List<TcpClient>();
    private static Dictionary<string, Room> chatRooms = new Dictionary<string, Room>();

    public static void Main(string[] args)
    {
        Console.WriteLine("Server started on port 55555");
        TcpListener listener = new TcpListener(IPAddress.Any, 55555);
        listener.Start();

        while (true)
        {
            AcceptNewClients(listener);
            ProcessMessagesFromClients();
            Thread.Sleep(100);
        }
    }

    private static void ProcessDisconnectedClients(TcpClient disconnectedClient)
    {
        if (disconnectedClient != null)
        {
            disconnectedClient.Close();

            foreach (var room in chatRooms.Values)
            {
                room.RemoveClient(disconnectedClient);
            }

            string clientDisconnectMessage = $"{clientNicknames[disconnectedClient]} disconnected from the server";
            BroadcastMessageToAllClients(Encoding.UTF8.GetBytes(clientDisconnectMessage));

            Console.WriteLine($"{clientNicknames[disconnectedClient]} disconnected and resources were cleaned up.");

            clients.Remove(disconnectedClient);
            clientNicknames.Remove(disconnectedClient);
        }
    }

    private static void AcceptNewClients(TcpListener listener)
    {
        while (listener.Pending())
        {
            TcpClient newClient = listener.AcceptTcpClient();
            clients.Add(newClient);

            string clientNickname = $"Client{++clientCounter}";
            clientNicknames[newClient] = clientNickname;

            if (!chatRooms.ContainsKey("general"))
            {
                chatRooms["general"] = new Room("general");
            }

            chatRooms["general"].AddClient(newClient);

            SendWelcomeMessage(newClient, clientNickname);
            BroadcastClientJoin(newClient, clientNickname);

            Console.WriteLine($"{clientNickname} connected to the server");
        }
    }

    private static void SendWelcomeMessage(TcpClient client, string nickname)
    {
        string welcomeMessage = $"You joined the server as {nickname}";
        byte[] welcomeMessageBytes = Encoding.UTF8.GetBytes(welcomeMessage);
        SendMessageToClient(client, welcomeMessageBytes);
    }

    private static void BroadcastClientJoin(TcpClient newClient, string nickname)
    {
        string roomName = "general";
        string joinMessage = $"{nickname} has joined the server";
        byte[] joinMessageBytes = Encoding.UTF8.GetBytes(joinMessage);
        chatRooms[roomName].BroadcastMessage(joinMessageBytes, newClient);
    }

    private static void ProcessMessagesFromClients()
    {
        foreach (TcpClient client in clients.ToArray())
        {
            if (!IsClientConnected(client))
            {
                ProcessDisconnectedClients(client);
                continue;
            }

            if (client.Available == 0) continue;
            NetworkStream stream = client.GetStream();

            byte[] message = StreamUtil.Read(stream);
            if (message == null) continue;
            string messageText = Encoding.UTF8.GetString(message);
            string senderNickname = clientNicknames[client];
            Room clientRoom = FindClientRoom(client);

            if (messageText.ToLower().StartsWith("/setname") || messageText.ToLower().StartsWith("/sn"))
            {
                ChangeClientNickname(client, messageText, senderNickname);
            }
            else if (messageText == "/list")
            {
                string clientList = "Connected clients: " + string.Join(", ", clientNicknames.Values);
                SendMessageToClient(client, Encoding.UTF8.GetBytes(clientList));
            }
            else if (messageText == "/help")
            {
                string helpMessage = "Chat Commands:\n" +
                                     "/list - Lists all connected clients.\n" +
                                     "/setname [new name] or /sn [new name] - Changes your nickname.\n" +
                                     "/join [roomname] - Automatically create and join a room\n" +
                                     "/listroom - Lists all users in the current client's room\n" +
                                     "/listrooms - Lists of all active rooms\n" +
                                     "/help - Shows this help message.";
                SendMessageToClient(client, Encoding.UTF8.GetBytes(helpMessage));
            }
            else if (messageText.StartsWith("/whisper") || messageText.StartsWith("/w"))
            {
                string[] parts = messageText.Split(new[] { ' ' }, 3);
                if (parts.Length < 3)
                {
                    string errorMessage = "Incorrect whisper usage. Correct format: /whisper nickname message";
                    SendMessageToClient(client, Encoding.UTF8.GetBytes(errorMessage));
                    continue;
                }

                string targetNickname = parts[1];
                string whisperMessage = parts[2];
                TcpClient targetClient = clientNicknames.FirstOrDefault(x => x.Value == targetNickname).Key;

                if (targetClient != null)
                {
                    if (string.IsNullOrEmpty(whisperMessage))
                    {
                        SendMessageToClient(client, Encoding.UTF8.GetBytes("Whisper message cannot be empty"));
                    }
                    else
                    {
                        string messageToTarget = $"{senderNickname} whispers: {whisperMessage}";
                        SendMessageToClient(targetClient, Encoding.UTF8.GetBytes(messageToTarget));

                        string confirmationMessage = $"You whisper to {targetNickname}: {whisperMessage}";
                        SendMessageToClient(client, Encoding.UTF8.GetBytes(confirmationMessage));
                    }
                }
                else
                {
                    string errorMessage = $"Target {targetNickname} does not exist.";
                    SendMessageToClient(client, Encoding.UTF8.GetBytes(errorMessage));
                }
            }
            else if (messageText.StartsWith("/join"))
            {
                HandleJoinRoom(client, messageText);
            }
            else if (messageText == "/listroom")
            {
                if (clientRoom != null)
                {
                    var nicknamesInRoom = clientRoom.GetClientNicknames(clientNicknames);
                    string clientList = "Connected clients in room: " + string.Join(", ", nicknamesInRoom);
                    SendMessageToClient(client, Encoding.UTF8.GetBytes(clientList));
                }
            }
            else if (messageText == "/listrooms")
            {
                string roomsList = GenerateRoomsList();
                SendMessageToClient(client, Encoding.UTF8.GetBytes(roomsList));
            }
            else
            {
                if (clientRoom != null)
                {
                    string timeStampedMessage = $"{DateTime.Now:HH:mm:ss} - {senderNickname}: {messageText}";
                    clientRoom.BroadcastMessage(Encoding.UTF8.GetBytes(timeStampedMessage));
                }
            }
        }
    }

    private static bool IsClientConnected(TcpClient client)
    {
        try
        {
            if (client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[1];
                if (client.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ChangeClientNickname(TcpClient client, string messageText, string oldNickname)
    {
        string newNickname = messageText.Contains("/setname ")
            ? messageText.Replace("/setname ", "")
            : messageText.Replace("/sn ", "");

        if (string.IsNullOrEmpty(newNickname) || clientNicknames.Values.Contains(newNickname) || newNickname == "/sn" ||
            newNickname == "/setname")
        {
            string errorMessage = "Nickname is either empty or already taken.";
            SendMessageToClient(client, Encoding.UTF8.GetBytes(errorMessage));
        }
        else if (newNickname.Contains(" "))
        {
            SendMessageToClient(client, Encoding.UTF8.GetBytes("No spaces allowed in nickname"));
        }
        else
        {
            clientNicknames[client] = newNickname;
            string systemMessage = $"{oldNickname} has changed their nickname to {newNickname}.";
            Console.WriteLine(systemMessage);
            BroadcastMessageToAllClients(Encoding.UTF8.GetBytes(systemMessage), client);

            string yourMessage = $"You changed your name from {oldNickname} to {newNickname} successfully";
            SendMessageToClient(client, Encoding.UTF8.GetBytes(yourMessage));
        }
    }

    private static void SendMessageToClient(TcpClient client, byte[] messageBytes)
    {
        try
        {
            StreamUtil.Write(client.GetStream(), messageBytes);
        }
        catch
        {
            //ProcessDisconnectedClients(client);
        }
    }

    private static void BroadcastMessageToAllClients(byte[] messageBytes, TcpClient sender = null)
    {
        foreach (TcpClient otherClient in clients)
        {
            if (otherClient != sender)
            {
                try
                {
                    if (otherClient.Connected)
                    {
                        StreamUtil.Write(otherClient.GetStream(), messageBytes);
                    }
                }
                catch
                {
                    //ProcessDisconnectedClients(otherClient);
                }
            }
        }
    }

    private static void HandleJoinRoom(TcpClient client, string messageText)
    {
        string[] parts = messageText.Split(new[] { ' ' }, 2);
        if (parts.Length < 2)
        {
            string errorMessage = "Usage: /join <roomname>";
            StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes(errorMessage));
            return;
        }

        string roomName = parts[1].Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            string errorMessage = "Please enter a valid room name. It should not be blank or consist solely of spaces.";
            SendMessageToClient(client, Encoding.UTF8.GetBytes(errorMessage));
            return;
        }

        Room currentRoom = FindClientRoom(client);
        currentRoom?.RemoveClient(client);

        if (!chatRooms.ContainsKey(roomName))
        {
            chatRooms[roomName] = new Room(roomName);
        }

        chatRooms[roomName].AddClient(client);

        string joinMessage = $"You joined room: {roomName}";
        StreamUtil.Write(client.GetStream(), Encoding.UTF8.GetBytes(joinMessage));
    }

    private static Room FindClientRoom(TcpClient client)
    {
        return chatRooms.Values.FirstOrDefault(room => room.ContainsClient(client));
    }

    private static string GenerateRoomsList()
    {
        if (chatRooms.Count == 0)
        {
            return "There are no active rooms.";
        }

        var roomList = new StringBuilder("Active Rooms: ");
        foreach (var room in chatRooms.Keys)
        {
            roomList.AppendLine(room);
        }

        return roomList.ToString();
    }
}