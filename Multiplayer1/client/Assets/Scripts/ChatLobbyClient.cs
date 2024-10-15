using shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour
{
    //reference to the helper class that hides all the avatar management behind a blackbox
    private AvatarAreaManager _avatarAreaManager;
    //reference to the helper class that wraps the chat interface
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private TcpClient _client;

    private void Start()
    {
        ConnectToServer();

        //register for the important events
        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += OnAvatarAreaClicked;

        _panelWrapper = FindObjectOfType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += OnChatTextEntered;
    }

    private void ConnectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");

        }
        catch (Exception e)
        {
            Debug.Log($"Could not connect to server: {e.Message}");
        }
    }

    private void OnAvatarAreaClicked(Vector3 pClickPosition)
    {
        Debug.Log($"ChatLobbyClient: you clicked on {pClickPosition}");
        SendMoveRequest(pClickPosition);
    }

    private void SendMoveRequest(Vector3 pClickPosition)
    {
        MoveRequest moveRequest = new MoveRequest
        {
            x = pClickPosition.x,
            y = pClickPosition.y,
            z = pClickPosition.z
        };
        SendObject(moveRequest);
    }

    private void OnChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        SendMessageRequest(pText);
    }

    private void SendMessageRequest(string pText)
    {
        bool isWhisper = pText.StartsWith("/whisper");
        if (isWhisper)
        {
            pText = pText.Substring("/whisper".Length).Trim();
        }

        MessageRequest messageRequest = new MessageRequest
        {
            Message = pText,
            IsWhisper = isWhisper,
        };

        SendObject(messageRequest);
    }

    private void SendObject(ISerializable pOutObject)
    {
        try
        {
            Debug.Log("Sending:" + pOutObject);

            Packet outPacket = new Packet();
            outPacket.Write(pOutObject);

            StreamUtil.Write(_client.GetStream(), outPacket.GetBytes());
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            ConnectToServer();
        }
    }

    // RECEIVING CODE

    private void Update()
    {
        try
        {
            if (_client.Available > 0)
            {
                byte[] inBytes = StreamUtil.Read(_client.GetStream());
                Packet inPacket = new Packet(inBytes);
                ISerializable inObject = inPacket.ReadObject();

                if (inObject is ClientJoin clientJoin)
                {
                    HandleClientJoin(clientJoin);
                }
                else if (inObject is ClientLeave leave)
                {
                    HandleClientLeave(leave);
                }
                else if (inObject is ChatMessage chatMessage)
                {
                    ShowMessageThroughAvatar(chatMessage);
                }else if (inObject is MoveCommand moveCommand)
                {
                    HandleMoveCommand(moveCommand);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            _client.Close();
            ConnectToServer();
        }
    }

    private void HandleMoveCommand(MoveCommand moveCommand)
    {
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(moveCommand.clientId);
        if (avatarView != null)
        {
            avatarView.Move(new Vector3(moveCommand.x, moveCommand.y, moveCommand.z));
        }
        else
        {
            Debug.LogWarning($"Avatar with ID {moveCommand.clientId} not found.");
        }
    }

    private void HandleClientJoin(ClientJoin clientJoin)
    {
        foreach (ClientData clientData in clientJoin.data)
        {
            AvatarView avatarView;
            if (_avatarAreaManager.HasAvatarView(clientData.clientID))
            {
                avatarView = _avatarAreaManager.GetAvatarView(clientData.clientID);
                avatarView.Move(new Vector3(clientData.x, clientData.y, clientData.z));
            }
            else
            {
                avatarView = _avatarAreaManager.AddAvatarView(clientData.clientID);
                avatarView.SetSkin(clientData.skinID);
                Vector3 newPosition = new Vector3(clientData.x, clientData.y, clientData.z);
                avatarView.Move(newPosition);
            }
        }
    }

    private void ShowMessageThroughAvatar(ChatMessage chatMessage)
    {
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(chatMessage.ClientId);
        if (avatarView != null)
        {
            avatarView.Say(chatMessage.Message);
        }
        else
        {
            Debug.LogWarning($"Avatar with ID {chatMessage.ClientId} not found.");
        }
    }

    private void HandleClientLeave(ClientLeave leave)
    {
        Debug.Log($"Client {leave.ClientId} left.");
        _avatarAreaManager.RemoveAvatarView(leave.ClientId);
    }
}
