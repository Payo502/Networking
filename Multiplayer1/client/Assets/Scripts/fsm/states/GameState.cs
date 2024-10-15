using shared;
using System;
using UnityEngine;

/**
 * This is where we 'play' a game.
 */
public class GameState : ApplicationStateWithView<GameView>
{
    //just for fun we keep track of how many times a player clicked the board
    //note that in the current application you have no idea whether you are player 1 or 2
    //normally it would be better to maintain this sort of info on the server if it is actually important information
    private int player1MoveCount = 0;
    private int player2MoveCount = 0;

    private int currentPlayerId = 0;

    public override void EnterState()
    {
        base.EnterState();

        view.gameBoard.OnCellClicked += _onCellClicked;
    }

    private void _onCellClicked(int pCellIndex)
    {
        if (currentPlayerId == 1 || currentPlayerId == 2)
        {
            MakeMoveRequest makeMoveRequest = new MakeMoveRequest();
            makeMoveRequest.move = pCellIndex;
            fsm.channel.SendMessage(makeMoveRequest);   
        }
        else
        {
            Debug.LogWarning("Not your turn!");
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        view.gameBoard.OnCellClicked -= _onCellClicked;
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is MakeMoveResult)
        {
            handleMakeMoveResult(pMessage as MakeMoveResult);
        }
        else if (pMessage is GameNameInit)
        {
            handleNameGameInit(pMessage as GameNameInit);
        }else if(pMessage is RoomJoinedEvent)
        {
            handleRoomJoinedEvent(pMessage as RoomJoinedEvent);
        }
        else if (pMessage is TurnUpdate)
        {
            handleTurnUpdate(pMessage as TurnUpdate);
        }
    }

    private void handleTurnUpdate(TurnUpdate pMessage)
    {
        currentPlayerId = pMessage.currentPlayerId;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        view.playerLabel1.text = (currentPlayerId == 1) ? "Your turn!" : "Waiting for Player 1...";
        view.playerLabel2.text = (currentPlayerId == 2) ? "Your turn!" : "Waiting for Player 2...";
    }

    private void handleRoomJoinedEvent(RoomJoinedEvent pMessage)
    {
        if (pMessage.room == RoomJoinedEvent.Room.LOBBY_ROOM)
        {
            view.gameBoard.ClearBoard();
            fsm.ChangeState<LobbyState>();
            Debug.LogWarning($"Moved to lobby state... {pMessage.room}");
        }
        else if (pMessage.room == RoomJoinedEvent.Room.GAME_OVER_ROOM)
        {
            view.gameBoard.ClearBoard();
            fsm.ChangeState<EndState>();
        }
    }

    private void handleNameGameInit(GameNameInit gameNameInit)
    {
        view.playerName1.text = $"Player 1 {gameNameInit.player1Name}";
        view.playerName2.text = $"Player 2 {gameNameInit.player2Name}";
    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        view.gameBoard.SetBoardData(pMakeMoveResult.boardData);

        //some label display
        /*if (pMakeMoveResult.whoMadeTheMove == 1)
        {
            player1MoveCount++;
            view.playerLabel1.text = $"Player 1 (Movecount: {player1MoveCount})";
        }
        if (pMakeMoveResult.whoMadeTheMove == 2)
        {
            player2MoveCount++;
            view.playerLabel2.text = $"Player 2 (Movecount: {player2MoveCount})";
        }*/
        
        UpdateLabel();
    }

    public void ConcedeGame()
    {
        fsm.channel.SendMessage(new ConcedeMessage());
        Debug.Log("Conceding game...");
    }
}
