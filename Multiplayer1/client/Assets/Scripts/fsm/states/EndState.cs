using System;
using System.Collections;
using System.Collections.Generic;
using shared;
using UnityEngine;

public class EndState : ApplicationStateWithView<EndView>
{
    public override void EnterState()
    {
        base.EnterState();
    }
    
    public override void ExitState()
    {
        base.ExitState();
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }
    
    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is ChatMessage)
        {
            handleChatMessage(pMessage as ChatMessage);
        }
    }

    private void handleChatMessage(ChatMessage pMessage)
    {
        view.gameResultText.text = pMessage.message;
    }

    public void GoToLobby()
    {
        fsm.channel.SendMessage(new GoToLobbyRequest());
        Log.LogInfo("Go to lobby request sent to server", this);
        fsm.ChangeState<LobbyState>();
    }
}
