using LNDroneController.LND;
using System.Threading;
using System.Threading.Tasks;

namespace LNDSecureCommunicator
{
    public interface ILightningBackgroundService
    {
        public LNDNodeConnection NodeConnection { get; }
        public Task<bool> EstablishCommunication(string remotePubkey);
        public Task<bool> EstablishCommunicationACK(string remotePubkey);
        public Task<bool> DisableCommunication(string remotePubkey);
    }
}