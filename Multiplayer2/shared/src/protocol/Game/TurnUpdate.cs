namespace shared
{
    public class TurnUpdate : ASerializable
    {
        public int currentPlayerId;
        
        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(currentPlayerId);
        }
        
        public override void Deserialize(Packet pPacket)
        {
            currentPlayerId = pPacket.ReadInt();
        }
    }
}