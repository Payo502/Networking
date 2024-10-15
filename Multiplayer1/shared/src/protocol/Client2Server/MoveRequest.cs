namespace shared
{
    public class MoveRequest : ISerializable
    {
        public float x, y, z;
        public void Serialize(Packet pPacket)
        {
            pPacket.Write(x);
            pPacket.Write(y);
            pPacket.Write(z);
        }

        public void Deserialize(Packet pPacket)
        {
            x = pPacket.ReadFloat();
            y = pPacket.ReadFloat();
            z = pPacket.ReadFloat();
        }
    }
}
