namespace shared
{
    public class ClientData : ISerializable
    {
        public int clientID;
        public int skinID;
        public float x;
        public float y;
        public float z;

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(clientID);
            pPacket.Write(skinID);
            pPacket.Write(x);
            pPacket.Write(y);
            pPacket.Write(z);
        }

        public void Deserialize(Packet pPacket)
        {
            clientID = pPacket.ReadInt();
            skinID = pPacket.ReadInt();
            x = pPacket.ReadFloat();
            y = pPacket.ReadFloat();
            z = pPacket.ReadFloat();
        }

    }
}
