namespace LNDSecureCommunicator.ServiceModel
{
    public class CountMessages
    {
        public string NodePubkey { get; set; }

    }

    public class CountMessagesResponse
    {
        public int Count { get; set; }
    }
}
