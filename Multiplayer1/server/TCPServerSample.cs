using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.IO.Compression;
using System.Linq;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */
class TCPServerSample
{
    public static void Main(string[] args)
    {
        TCPServerSample server = new TCPServerSample();
        server.run();
    }

    private TcpListener _listener;
    private List<TcpClient> _clients = new List<TcpClient>();
    private Dictionary<TcpClient, ClientData> _clientData = new Dictionary<TcpClient, ClientData>();
    private int _nextAvatarId = 1;

    private void run()
    {
        Console.WriteLine("Server started on port 55555");

        _listener = new TcpListener(IPAddress.Any, 55555);
        _listener.Start();

        while (true)
        {
            ProcessNewClients(_listener);
            ProcessExistingClients();

            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            Thread.Sleep(100);
        }
    }

    private void ProcessNewClients(TcpListener listener)
    {
        while (listener.Pending())
        {
            var newClient = listener.AcceptTcpClient();
            _clients.Add(newClient);
            Console.WriteLine($"Accepted new client. Total clients: {_clients.Count}");

            HandleClientJoin(newClient);
        }
    }
    private void HandleClientJoin(TcpClient newClient)
    {
        int newClientId = _nextAvatarId++;

        int skinId = GetRandomSkinId();
        float x = GetRandomXValue();
        float y = 0;
        float z = GetRandomZValue();

        ClientData newClientData = new ClientData
        {
            clientID = newClientId,
            skinID = skinId,
            x = x,
            y = y,
            z = z
        };

        _clientData.Add(newClient, newClientData);

        Console.WriteLine($"Creating avatar for client ID: {newClientId}, The Total number of clients is {_clients.Count}");


        ClientJoin clientJoin = new ClientJoin
        {
            ClientId = newClientData.clientID,
            SkinId = newClientData.skinID,
            x = newClientData.x,
            y = newClientData.y,
            z = newClientData.z
        };

        for (int i = 0; i < _clients.Count; i++)
            clientJoin.data.Add(_clientData[_clients[i]]);
        BroadcastToAllClients(clientJoin);

    }

    private void ProcessDisconnectedClients(TcpClient disconnectedClient)
    {
        if (_clientData.TryGetValue(disconnectedClient, out ClientData clientData))
        {
            _clientData.Remove(disconnectedClient);
            _clients.Remove(disconnectedClient);

            disconnectedClient.Close();

            ClientLeave clientLeave = new ClientLeave
            {
                ClientId = clientData.clientID
            };

            BroadcastToAllClients(clientLeave);
        }
    }

    private void ProcessExistingClients()
    {
        foreach (TcpClient client in _clients.ToArray())
        {
            if (!IsClientConnected(client))
            {
                ProcessDisconnectedClients(client);
                continue;
            }

            if (client.Available == 0) continue;

            byte[] inBytes = StreamUtil.Read(client.GetStream());
            Packet inPacket = new Packet(inBytes);
            ISerializable inObject = inPacket.ReadObject();
            Console.WriteLine($"Received message type: {inObject.GetType().Name}");

            // handle Messages from clients
            if (inObject is MessageRequest messageRequest)
            {
                HandleMessageRequest(client, messageRequest);
            }
            else if (inObject is MoveRequest moveRequest)
            {
                HandleMoveRequest(client, moveRequest);
            }
        }
    }

    private void HandleMoveRequest(TcpClient client, MoveRequest moveRequest)
    {
        if (_clientData.TryGetValue(client, out ClientData clientData))
        {
            bool isValidPosition = ValidatePosition(moveRequest.x, moveRequest.y, moveRequest.z);

            if (isValidPosition)
            {

                clientData.x = moveRequest.x;
                clientData.y = moveRequest.y;
                clientData.z = moveRequest.z;


                MoveCommand moveCommand = new MoveCommand
                {
                    clientId = clientData.clientID,
                    x = moveRequest.x,
                    y = moveRequest.y,
                    z = moveRequest.z
                };


                BroadcastToAllClients(moveCommand);
            }
            else
            {
                Console.WriteLine($"Invalid move attempt by client {clientData.clientID}.");
            }
        }
        else
        {
            Console.WriteLine("Error: Client not found in clientData dictionary.");
        }
    }

    private bool ValidatePosition(float x, float y, float z)
    {
        float minX = -20f;
        float maxX = 20f;
        float minZ = -5f;
        float maxZ = 15f;

        return x >= minX && x <= maxX && z >= minZ && z <= maxZ;
    }

    private void HandleMessageRequest(TcpClient client, MessageRequest messageRequest)
    {
        if (_clientData.TryGetValue(client, out ClientData clientData))
        {
            ChatMessage chatMessage = new ChatMessage
            {
                ClientId = clientData.clientID,
                Message = messageRequest.Message,
                IsWhisper = messageRequest.IsWhisper,
            };

            if (!messageRequest.IsWhisper)
            {
                BroadcastToAllClients(chatMessage);
            }
            else
            {
                if (_clientData.TryGetValue(client, out ClientData senderData))
                {
                    foreach (var data in _clientData)
                    {
                        TcpClient otherClient = data.Key;
                        ClientData otherData = data.Value;

                        float distance = CalculateDistance(senderData.x, senderData.y, senderData.z, otherData.x, otherData.y, otherData.z);

                        if (distance <= 2.0)
                        {
                            ChatMessage whisperMessage = new ChatMessage
                            {
                                ClientId = senderData.clientID,
                                Message = messageRequest.Message,
                                IsWhisper = messageRequest.IsWhisper,
                            };

                            SendObjectToClient(otherClient, whisperMessage);
                        }
                    }
                }

            }
        }
        else
        {
            Console.WriteLine("Error: Client not found in clientData dictionary.");
        }
    }

    private void SendObjectToClient(TcpClient client, ISerializable pOutObject)
    {
        Packet outPacket = new Packet();
        outPacket.Write(pOutObject);
        byte[] bytesToSend = outPacket.GetBytes();

        try
        {
            StreamUtil.Write(client.GetStream(), bytesToSend);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send data to the client: {ex.Message}");
            ProcessDisconnectedClients(client);
        }
    }

    private void BroadcastToAllClients(ISerializable pOutObject)
    {
        Packet outPacket = new Packet();
        outPacket.Write(pOutObject);
        byte[] bytesToSend = outPacket.GetBytes();

        foreach (TcpClient otherClient in _clients.ToArray())
        {
            try
            {
                StreamUtil.Write(otherClient.GetStream(), bytesToSend);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send to a client: {ex.Message}");
                //ProcessDisconnectedClients(otherClient);
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking client connection: {ex.Message}");
            return false;
        }
    }
    private int GetRandomSkinId() => new Random().Next(1, 5);
    private float GetRandomXValue()
    {
        Random rand = new Random();
        return (float)(rand.NextDouble() * 35.0) - 20.0f;
    }
    private float GetRandomZValue()
    {
        Random rand = new Random();
        return (float)(rand.NextDouble() * 8.0) - 5.0f;
    }

    private float CalculateDistance(float x1, float y1, float z1, float x2, float y2, float z2)
    {
        return (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));
    }
}

