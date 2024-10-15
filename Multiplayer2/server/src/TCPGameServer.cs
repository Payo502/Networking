using System;
using System.Net.Sockets;
using System.Net;
using shared;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace server
{

	/**
	 * Basic TCPGameServer that runs our game.
	 * 
	 * Server is made up out of different rooms that can hold different members.
	 * Each member is identified by a TcpMessageChannel, which can also be used for communication.
	 * In this setup each client is only member of ONE room, but you could change that of course.
	 * 
	 * Each room is responsible for cleaning up faulty clients (since it might involve gameplay, status changes etc).
	 * 
	 * As you can see this setup is limited/lacking:
	 * - only 1 game can be played at a time
	 */
	class TCPGameServer
	{

		public static void Main(string[] args)
		{
			TCPGameServer tcpGameServer = new TCPGameServer();
			tcpGameServer.run();
		}

		//we have 3 different rooms at the moment (aka simple but limited)

		private LoginRoom _loginRoom;   //this is the room every new user joins
		private LobbyRoom _lobbyRoom;   //this is the room a user moves to after a successful 'login'
		private List<GameRoom> _gameRooms = new List<GameRoom>();
		private GameOverRoom _gameOverRoom;

		//stores additional info for a player
		private Dictionary<TcpMessageChannel, PlayerInfo> _playerInfo = new Dictionary<TcpMessageChannel, PlayerInfo>();

		private TCPGameServer()
		{
			_loginRoom = new LoginRoom(this);
			_lobbyRoom = new LobbyRoom(this);
			_gameOverRoom = new GameOverRoom(this);

			SetupGameRoomListeners();
		}

        private void SetupGameRoomListeners()
        {
            foreach (GameRoom gameRoom in _gameRooms)
            {
                gameRoom.OnGameFinished += HandleGameFinished;
            }
        }

        private void HandleGameFinished(GameRoom room)
        {
			_gameRooms.Remove(room);
            Log.LogInfo($"Active game rooms count: {_gameRooms.Count}", this);

        }

        private void run()
		{
			Log.LogInfo("Starting server on port 55555", this, ConsoleColor.Gray);

			//start listening for incoming connections (with max 50 in the queue)
			//we allow for a lot of incoming connections, so we can handle them
			//and tell them whether we will accept them or not instead of bluntly declining them
			TcpListener listener = new TcpListener(IPAddress.Any, 55555);
			listener.Start(50);

			while (true)
			{
				//check for new members	
				if (listener.Pending())
				{
					//get the waiting client
					Log.LogInfo("Accepting new client...", this, ConsoleColor.White);
					TcpClient client = listener.AcceptTcpClient();
					//and wrap the client in an easier to use communication channel
					TcpMessageChannel channel = new TcpMessageChannel(client);
					//and add it to the login room for further 'processing'
					_loginRoom.AddMember(channel);
				}

				//now update every single room
				_loginRoom.Update();
				_lobbyRoom.Update();
				_gameOverRoom.Update();
				
				foreach (GameRoom _gameRoom in _gameRooms.ToArray())
				{
					_gameRoom.Update();
				}

				Thread.Sleep(100);
			}

		}

		//provide access to the different rooms on the server 
		public LoginRoom GetLoginRoom() { return _loginRoom; }
		public LobbyRoom GetLobbyRoom() { return _lobbyRoom; }
		public GameOverRoom GetGameOverRoom() { return _gameOverRoom; }
		//public GameRoom GetGameRoom() { return _gameRoom; }

		/**
		 * Returns a handle to the player info for the given client 
		 * (will create new player info if there was no info for the given client yet)
		 */
		public PlayerInfo GetPlayerInfo(TcpMessageChannel pClient)
		{
			if (!_playerInfo.ContainsKey(pClient))
			{
				_playerInfo[pClient] = new PlayerInfo();
			}

			return _playerInfo[pClient];
		}

		/**
		 * Returns a list of all players that match the predicate, e.g. to get a list of 
		 * all players named bob, you would do:
		 *	GetPlayerInfo((playerInfo) => playerInfo.name == "bob");
		 */
		public List<PlayerInfo> GetPlayerInfo(Predicate<PlayerInfo> pPredicate)
		{
			return _playerInfo.Values.ToList<PlayerInfo>().FindAll(pPredicate);
		}

		/**
		 * Should be called by a room when a member is closed and removed.
		 */
		public void RemovePlayerInfo(TcpMessageChannel pClient)
		{
			_playerInfo.Remove(pClient);
		}

		public GameRoom CreateNewGameRoom()
		{
			GameRoom newGameRoom = new GameRoom(this);
			_gameRooms.Add(newGameRoom);
            Log.LogInfo($"Created a new GameRoom. Total active game rooms: {_gameRooms.Count}", this);
            return newGameRoom;
		}

		public void RemoveGameRoom(GameRoom gameRoom)
		{
            _gameRooms.Remove(gameRoom);
            Log.LogInfo($"Total active game rooms after removal: {_gameRooms.Count}.", this);

        }
    }
}




