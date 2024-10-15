using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using TcpMessageChannel = shared.TcpMessageChannel;

namespace server
{
    /**
     * This room runs a single Game (at a time).
     *
     * The 'Game' is very simple at the moment:
     *	- all client moves are broadcast to all clients
     *
     * The game has no end yet (that is up to you), in other words:
     * all players that are added to this room, stay in here indefinitely.
     */
    class GameRoom : Room
    {
        private bool IsGameInPlay { get; set; }

        private readonly List<TcpMessageChannel> _membersInGame = new List<TcpMessageChannel>();

        //wraps the board to play on...
        private TicTacToeBoard _board = new TicTacToeBoard();

        private int CurrentTurn { get; set; } = 0;

        public event Action<GameRoom> OnGameFinished;

        public GameRoom(TCPGameServer pOwner) : base(pOwner)
        {
        }

        public void StartGame(TcpMessageChannel pPlayer1, TcpMessageChannel pPlayer2)
        {
            if (IsGameInPlay) throw new Exception("A game is already in progress");

            IsGameInPlay = true;
            CurrentTurn = 0;

            addMember(pPlayer1);
            addMember(pPlayer2);

            PlayerInfo player1 = _server.GetPlayerInfo(pPlayer1);
            PlayerInfo player2 = _server.GetPlayerInfo(pPlayer2);

            GameNameInit gameNameInit = new GameNameInit
            {
                player1Name = player1.playerNickname,
                player2Name = player2.playerNickname,
            };

            pPlayer1.SendMessage(gameNameInit);
            pPlayer2.SendMessage(gameNameInit);

            SendTurnUpdate();
        }

        protected override void addMember(TcpMessageChannel pMember)
        {
            base.addMember(pMember);

            //notify client he has joined a game room 
            RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent
            {
                room = RoomJoinedEvent.Room.GAME_ROOM
            };
            pMember.SendMessage(roomJoinedEvent);

            _membersInGame.Add(pMember);
        }

        protected override void removeMember(TcpMessageChannel pMember)
        {
            base.removeMember(pMember);
            if (IsGameInPlay && _membersInGame.Contains(pMember))
            {
                _membersInGame.Remove(pMember);
                if (_membersInGame.Count == 1)
                {
                    ConcludeGame(_membersInGame.First(), false, false, true);
                }
            }
        }

        public override void Update()
        {
            //demo of how we can tell people have left the game...
            int oldMemberCount = memberCount;
            base.Update();
            int newMemberCount = memberCount;

            if (oldMemberCount != newMemberCount)
            {
                Log.LogInfo("People left the game...", this);
            }
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            if (pMessage is MakeMoveRequest)
            {
                if (IsPlayerTurn(pSender))
                {
                    handleMakeMoveRequest(pMessage as MakeMoveRequest, pSender);
                }
            }
            else if (pMessage is ConcedeMessage)
            {
                HandleConcedeRequest(pSender);
            }
            else if (pMessage is Heartbeat)
            {
                _lastHeartbeatReceived[pSender] = DateTime.Now;
            }
        }

        private bool IsPlayerTurn(TcpMessageChannel pSender)
        {
            return _membersInGame.IndexOf(pSender) == CurrentTurn;
        }


        private void HandleConcedeRequest(TcpMessageChannel concedingPlayer)
        {
            Log.LogInfo($"Player {indexOfMember(concedingPlayer) + 1} has conceded.", this);
            TcpMessageChannel winningPlayer = _membersInGame.FirstOrDefault(player => player != concedingPlayer);
            ConcludeGame(winningPlayer, true);
        }

        private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
        {
            //int playerID = indexOfMember(pSender) + 1;
            _board.MakeMove(pMessage.move, CurrentTurn + 1);

            int winner = _board.GetBoardData().WhoHasWon();
            TcpMessageChannel winningPlayer = winner == 1 ? _membersInGame[0] : _membersInGame[1];
            if (winner != 0)
            {
                if (winner == -1)
                {
                    ConcludeGame(winningPlayer, false, true);
                }
                else
                {
                    ConcludeGame(winningPlayer);
                }
            }
            else
            {
                ToggleTurn();
                MakeMoveResult makeMoveResult = new MakeMoveResult
                {
                    whoMadeTheMove = CurrentTurn + 1,
                    boardData = _board.GetBoardData()
                };
                sendToAll(makeMoveResult);
            }
        }

        private void ToggleTurn()
        {
            CurrentTurn = (CurrentTurn + 1) % _membersInGame.Count;
            SendTurnUpdate();
        }

        private void SendTurnUpdate()
        {
            TurnUpdate turnUpdate = new TurnUpdate
            {
                currentPlayerId = CurrentTurn + 1
            };
            sendToAll(turnUpdate);
        }

        private void ConcludeGame(TcpMessageChannel winningPlayer, bool isConceding = false, bool isTie = false, bool isDisconnect = false)
        {
            IsGameInPlay = false;

            TcpMessageChannel losingPlayer = _membersInGame.FirstOrDefault(player => player != winningPlayer);

            MovePlayerToEndScreen(winningPlayer);
            MovePlayerToEndScreen(losingPlayer);

            RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent
            {
                room = RoomJoinedEvent.Room.GAME_OVER_ROOM
            };
            sendToAll(roomJoinedEvent);

            if (isConceding)
            {
                SendMessageToPlayer(winningPlayer, new ChatMessage { message = "You have won by concession." });
                SendMessageToPlayer(losingPlayer, new ChatMessage { message = "You have lost by concession." });
            }
            else if (isTie)
            {
                SendMessageToPlayer(winningPlayer, new ChatMessage { message = "It was a tie!" });
                SendMessageToPlayer(losingPlayer, new ChatMessage { message = "It was a tie!" });
            }
            else if (isDisconnect)
            {
                SendMessageToPlayer(winningPlayer, new ChatMessage { message = "You have won due to disconnection." });
            }
            else
            {
                SendMessageToPlayer(winningPlayer, new ChatMessage { message = "You have won." });
                SendMessageToPlayer(losingPlayer, new ChatMessage { message = "You have lost." });
            }


            _board = new TicTacToeBoard();
            _membersInGame.Clear();
            _server.RemoveGameRoom(this);

            OnGameFinished?.Invoke(this);
        }

        private void MovePlayerToEndScreen(TcpMessageChannel playerToMove)
        {
            if (playerToMove != null)
            {
                _server.GetGameOverRoom().AddMember(playerToMove);
                removeMember(playerToMove);
            }
        }

        private void SendMessageToPlayer(TcpMessageChannel winningPlayer, ChatMessage chatMessage)
        {
            if (winningPlayer != null)
            {
                winningPlayer.SendMessage(chatMessage);
            }
        }
    }
}