namespace shared
{
    public class GameResults : ASerializable
    {
        public string gameResutlsText;
        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(gameResutlsText);
        }

        public override void Deserialize(Packet pPacket)
        {
            gameResutlsText = pPacket.ReadString();
        }
    }
}