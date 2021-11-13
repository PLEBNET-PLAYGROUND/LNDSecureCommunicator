namespace LNDSecureCommunicator.ServiceModel
{
    public class ListMessages
    {
        public string NodePubkey { get; set; }
        public int? Take { get; set; }
        public int? Skip { get; set; }
    }
}
