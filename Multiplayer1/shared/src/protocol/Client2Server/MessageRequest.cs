namespace shared
{
    public class MessageRequest : ISerializable
    {
        public string Message { get; set; }
        public bool IsWhisper { get; set; }
        public void Serialize(Packet pPacket)
        {
            pPacket.Write(Message);
            pPacket.Write(IsWhisper);
        }

        public void Deserialize(Packet pPacket)
        {
            Message = pPacket.ReadString();
            IsWhisper = pPacket.ReadBool();
        }

    }
}
