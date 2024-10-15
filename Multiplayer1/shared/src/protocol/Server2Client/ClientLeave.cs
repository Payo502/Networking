namespace shared
{
    public class ClientLeave : ISerializable
    {
        public int ClientId { get; set; }
        public void Serialize(Packet pPacket)
        {
            pPacket.Write(ClientId);
        }

        public void Deserialize(Packet pPacket)
        {
            ClientId = pPacket.ReadInt();
        }

    }
}
