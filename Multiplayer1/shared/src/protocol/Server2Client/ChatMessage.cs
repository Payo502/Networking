namespace shared
{
    public class ChatMessage : ISerializable
    {
        public int ClientId { get; set; }
        public string Message { get; set; }
        public bool IsWhisper { get; set; }

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(ClientId);
            pPacket.Write(Message);
            pPacket.Write(IsWhisper);
        }

        public void Deserialize(Packet pPacket)
        {
            ClientId = pPacket.ReadInt();
            Message = pPacket.ReadString();
            IsWhisper = pPacket.ReadBool();
        }

    }
}
