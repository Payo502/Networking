using System.Collections.Generic;

namespace shared
{
    public class ClientJoin : ISerializable
    {
        public int ClientId { get; set; }
        public int SkinId { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public List<ClientData> data = new List<ClientData>();
        public void Serialize(Packet pPacket)
        {
            pPacket.Write(ClientId);
            pPacket.Write(SkinId);
            pPacket.Write(x);
            pPacket.Write(y);
            pPacket.Write(z);

            int count = data.Count;
            pPacket.Write(count);
            for (int i = 0; i < count; i++)
                pPacket.Write(data[i]);
        }
        public void Deserialize(Packet pPacket)
        {
            ClientId = pPacket.ReadInt();
            SkinId = pPacket.ReadInt();
            x = pPacket.ReadFloat();
            y = pPacket.ReadFloat();
            z = pPacket.ReadFloat();

            int count = pPacket.ReadInt();
            for (int i = 0; i < count; i++)
                data.Add(pPacket.Read<ClientData>());

        }

    }
}
