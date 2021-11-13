namespace LNDSecureCommunicator.ServiceModel
{
    public class LNDEncryptedDataFormat
    {
        public string NodePubkey { get; set; }
        public byte[] IV { get; set; }
        public byte[] Data { get; set; }
    }
}
