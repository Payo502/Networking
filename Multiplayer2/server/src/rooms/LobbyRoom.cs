using shared;
using System;
using System.Collections.Generic;

namespace server
{
	/**
	 * The LobbyRoom is a little bit more extensive than the LoginRoom.
	 * In this room clients change their 'ready status'.
	 * If enough people are ready, they are automatically moved to the GameRoom to play a Game (assuming a game is not already in play).
	 */ 
	class LobbyRoom : SimpleRoom
	{
		//this list keeps tracks of which players are ready to play a game, this is a subset of the people in this room
		private List<TcpMessageChannel> _readyMembers = new List<TcpMessageChannel>();

		public LobbyRoom(TCPGameServer pOwner) : base(pOwner)
		{
		}

		protected override void addMember(TcpMessageChannel pMember)
		{
			base.addMember(pMember);

			PlayerInfo playerInfo = _server.GetPlayerInfo(pMember);

			//tell the member it has joined the lobby
			RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
			roomJoinedEvent.room = RoomJoinedEvent.Room.LOBBY_ROOM;
			pMember.SendMessage(roomJoinedEvent);

			//print some info in the lobby (can be made more applicable to the current member that joined)
			ChatMessage simpleMessage = new ChatMessage();
			// how can i send the clients nickname here?
			simpleMessage.message = $"Client '{playerInfo.playerNickname}' has joined the lobby!";
			sendToAll(simpleMessage);

			//send information to all clients that the lobby count has changed
			sendLobbyUpdateCount();
		}

		/**
		 * Override removeMember so that our ready count and lobby count is updated (and sent to all clients)
		 * anytime we remove a member.
		 */
		protected override void removeMember(TcpMessageChannel pMember)
		{
			base.removeMember(pMember);
			_readyMembers.Remove(pMember);

			sendLobbyUpdateCount();
		}

		protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
		{
			if (pMessage is ChangeReadyStatusRequest) handleReadyNotification(pMessage as ChangeReadyStatusRequest, pSender);
			else if (pMessage is ChatMessage) handleChatMessage(pMessage as ChatMessage, pSender);
			else if (pMessage is Heartbeat)
			{
				_lastHeartbeatReceived[pSender] = DateTime.Now;
			}
		}

        private void handleChatMessage(ChatMessage chatMessage, TcpMessageChannel pSender)
        {
			PlayerInfo senderInfo = _server.GetPlayerInfo(pSender);

			string timestamp = DateTime.Now.ToString("HH:MM:ss");
			string formattedMessage = $"[{timestamp}] {senderInfo.playerNickname}: {chatMessage.message}";
            chatMessage.message = formattedMessage;

            sendToAll(chatMessage);
        }

        private void handleReadyNotification(ChangeReadyStatusRequest pReadyNotification, TcpMessageChannel pSender)
		{
			//if the given client was not marked as ready yet, mark the client as ready
			if (pReadyNotification.ready)
			{
				if (!_readyMembers.Contains(pSender)) _readyMembers.Add(pSender);
			}
			else //if the client is no longer ready, unmark it as ready
			{
				_readyMembers.Remove(pSender);
			}

			//do we have enough people for a game and is there no game running yet?
			if (_readyMembers.Count >= 2)
			{
				TcpMessageChannel player1 = _readyMembers[0];
				TcpMessageChannel player2 = _readyMembers[1];
				removeMember(player1);
				removeMember(player2);

				GameRoom newGameRoom = _server.CreateNewGameRoom();
				newGameRoom.StartGame(player1, player2);
			}

			//(un)ready-ing / starting a game changes the lobby/ready count so send out an update
			//to all clients still in the lobby
			sendLobbyUpdateCount();
		}
			
		private void sendLobbyUpdateCount()
		{
			LobbyInfoUpdate lobbyInfoMessage = new LobbyInfoUpdate();
			lobbyInfoMessage.memberCount = memberCount;
			lobbyInfoMessage.readyCount = _readyMembers.Count;
			sendToAll(lobbyInfoMessage);
		}

        public void BroadcastMessageToLobby(string message)
        {
            ChatMessage gameEndMessage = new ChatMessage
            {
                message = message
            };
            sendToAll(gameEndMessage);
        }
    }
}
