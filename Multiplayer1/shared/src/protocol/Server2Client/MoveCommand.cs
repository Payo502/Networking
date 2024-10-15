namespace shared
{
    public class MoveCommand : ISerializable
    {
        public float x, y, z;
        public int clientId;
        public void Serialize(Packet pPacket)
        {
            pPacket.Write(x);
            pPacket.Write(y);
            pPacket.Write(z);
            pPacket.Write(clientId);
        }

        public void Deserialize(Packet pPacket)
        {
            x = pPacket.ReadFloat();
            y = pPacket.ReadFloat();
            z = pPacket.ReadFloat();
            clientId = pPacket.ReadInt();
        }
    }
}
