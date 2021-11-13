using ServiceStack;
using System;
using System.Collections.Generic;
using System.Text;

namespace LNDSecureCommunicator.ServiceModel
{
    public class SendMessage 
    {
        public string Pubkey { get; set; }
        public string? UTF8TextMessage { get; set; }
        public byte[]? Data { get; set; }
    }
}
