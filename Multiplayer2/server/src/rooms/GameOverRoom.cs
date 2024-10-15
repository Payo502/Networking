using shared;
using System;
using System.Collections.Generic;

namespace server
{ 
    class GameOverRoom : SimpleRoom
    {
        public GameOverRoom(TCPGameServer pOwner) : base(pOwner)
        {
            
        }
        
        protected override void addMember(TcpMessageChannel pMember)
        {
            base.addMember(pMember);
            
            //tell the member it has joined the gameover room
            RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
            roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_OVER_ROOM;
            pMember.SendMessage(roomJoinedEvent);
        }
        
        protected override void removeMember(TcpMessageChannel pMember)
        {
            base.removeMember(pMember);
        }
        
        

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            // receive go to loby request from client
            if (pMessage is GoToLobbyRequest)
            {
                handleGoToLobbyRequest(pSender);
                Log.LogInfo("Go to lobby request received from client", this);
            }
            else if (pMessage is Heartbeat)
            {
                _lastHeartbeatReceived[pSender] = DateTime.Now;
            }
        }

        private void handleGoToLobbyRequest(TcpMessageChannel pSender)
        {
            removeMember(pSender);
            _server.GetLobbyRoom().AddMember(pSender);
        }
    }
}