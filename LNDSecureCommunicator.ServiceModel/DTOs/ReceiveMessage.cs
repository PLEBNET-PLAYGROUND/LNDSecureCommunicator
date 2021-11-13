using ServiceStack;

namespace LNDSecureCommunicator.ServiceModel
{
    public class ReceiveMessage 
    {
        public string Pubkey { get; set; }
        public SecureMessageType MessageType { get; set; }
        public byte[] MessageData { get; set; }
        public byte[]? FileData { get; set; }
        public byte[] IV { get; set; }
    }
}
