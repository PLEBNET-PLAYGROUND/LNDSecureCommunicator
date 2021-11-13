using LNDroneController.LND;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TorHiddenServiceHelper;
using Microsoft.Extensions.Options;
using LNDSecureCommunicator.ServiceInterface;
using Microsoft.Extensions.DependencyInjection;

namespace LNDSecureCommunicator
{
    public class LightningBackgroundService : IHostedService, IDisposable, ILightningBackgroundService
    {
        public static ulong EstablishCommunicationKeysendType = 343493346969L;
        public static ulong EstablishCommunicationKeysendACKType = 343493356969L;
        public LNDNodeConnection NodeConnection { get; }
        private LNDSecureCommunicatorSetting Settings { get; set; } = null;
        private CommunicatorSettings CommunicatorSettings { get; set; }

        private IServiceProvider _serviceProvider;

        public LNDSecureContext Db { get { return _serviceProvider.GetRequiredService<LNDSecureContext>(); } }

        private Timer Timer;
        private ulong? InvoiceLastIndexOffset;

        private ILogger<LightningBackgroundService> _logger;

        private TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);


        public LightningBackgroundService(LNDroneController.LND.LNDNodeConnection nodeConnection,
            ILogger<LightningBackgroundService> logger,
            Microsoft.Extensions.Options.IOptions<CommunicatorSettings> commSettings,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            NodeConnection = nodeConnection;
            CommunicatorSettings = commSettings.Value;
        }
        public void Dispose()
        {
            if (Timer != null)
                Timer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Timer = new Timer(PrimaryLoop, null, TimeSpan.FromSeconds(5), RefreshInterval); //check invoices
            return Task.CompletedTask;
        }

        private void PrimaryLoop(object state)
        {
            _logger.LogDebug("Enter Primary Loop");
            Timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (Settings == null || Settings.NodePubkey.IsNullOrEmpty())
            {
                _logger.LogDebug("PL: TryUpdatingNodeIdForSettings");
                TryUpdatingNodeIdForSettings();
            }
            else if (!InvoiceLastIndexOffset.HasValue)
            {
                _logger.LogDebug("PL: LoadLastIndexFromDb");
                LoadLastIndexFromDb();
            }
            else
            {
                _logger.LogDebug("PL: CheckForNewNodePeerRequests");
                CheckForNewNodePeerRequests().Wait();
            }

            Timer.Change(RefreshInterval, RefreshInterval);
            _logger.LogDebug("Exit Primary Loop");
        }

        private void LoadLastIndexFromDb()
        {
            using (var db = Db)
            {
                Settings = db.LNDSecureCommunicatorSettings.Single();
                InvoiceLastIndexOffset = (CommunicatorSettings.InvoiceOffsetStartPoint < Settings.InvoiceLastIndexOffset) ? Settings.InvoiceLastIndexOffset : CommunicatorSettings.InvoiceOffsetStartPoint;
                _logger.LogInformation("Last Invoice Offset: {InvoiceLastIndexOffset}", InvoiceLastIndexOffset.Value);
            }
        }

        /// <summary>
        /// Used to setup a remote peer
        /// </summary>
        /// <param name="remotePubkey">pubkey of the remote peer</param>
        /// <returns></returns>
        public async Task<bool> EstablishCommunication(string remotePubkey)
        {
            using (var db = Db)
            {
                if (db.RemoteNodes.Any(x => x.NodePubkey == remotePubkey))
                {
                    _logger.LogWarning("Remote node {RemotePubkey} already exists", remotePubkey);
                    return false;
                }

                //Format: node pubkey, node client auth pubkey, onion endpoint
                var msg = $"LNDSecureCommunicator:CONNECT:{Settings.NodePubkey}:{Settings.ClientAuthBase32PublicKey}:{Settings.OnionPublicAddress}";
                var custom = new Dictionary<ulong, byte[]>();
                custom.Add(EstablishCommunicationKeysendType, msg.ToUtf8Bytes());
                var sharedKey = (await NodeConnection.DeriveSharedKey(remotePubkey)).SharedKey;
                try
                {
                    var response = await NodeConnection.KeysendPayment(remotePubkey, 10, 10, null, 60, custom);
                    if (response.Status == Lnrpc.Payment.Types.PaymentStatus.Succeeded)
                    {

                        var timeStamp = DateTime.UtcNow;
                        db.RemoteNodes.Add(new RemoteNode
                        {
                            CreatedDate = timeStamp,
                            UpdateDate = timeStamp,
                            NodePubkey = remotePubkey,
                            SharedBase64PrivateKey = sharedKey.ToBase64(),
                            RemoteNodeACK = false,
                        });
                        db.SaveChanges();
                        _logger.LogInformation("Sent CONNECT keysend to {RemotePubkey} was {Status}: {CustomData}", remotePubkey, response.Status, msg);
                    }
                    return response.Status == Lnrpc.Payment.Types.PaymentStatus.Succeeded;
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "EstablishCommunication");
                }

                return false;
            }
        }

        public async Task<bool> DisableCommunication(string remotePubkey)
        {
            using (var db = Db)
            {
                var node = db.RemoteNodes.SingleOrDefault(x => x.NodePubkey == remotePubkey);
                if (node == null)
                {
                    _logger.LogError("DisableCommunication failed because record for {RemotePubkey} was not found", remotePubkey);
                    return false;
                }
                else
                {
                    node.Disabled = true;
                    node.UpdateDate = DateTime.UtcNow;
                    db.SaveChanges();
                    //TODO: Implement onion rebinding
                    _logger.LogInformation("DisableCommunication on {RemotePubkey}", remotePubkey);
                }
                return true;
            }

        }

        /// <summary>
        /// Used as a response to CONNECT message, giving originating server enough info to setup the client auth for remote API access
        /// </summary>
        /// <param name="remotePubkey"></param>
        /// <returns></returns>
        public async Task<bool> EstablishCommunicationACK(string remotePubkey)
        {
            //Format: node pubkey, node client auth pubkey, onion endpoint
            var msg = $"LNDSecureCommunicator:ACK:{Settings.NodePubkey}:{Settings.ClientAuthBase32PublicKey}:{Settings.OnionPublicAddress}";
            var custom = new Dictionary<ulong, byte[]>();
            custom.Add(EstablishCommunicationKeysendACKType, msg.ToUtf8Bytes());
            try
            {
                var sharedKey = await NodeConnection.DeriveSharedKey(remotePubkey);
                var response = await NodeConnection.KeysendPayment(remotePubkey, 10, 10, null, 60, custom);
                _logger.LogInformation("Sent ACK keysend to {RemotePubkey} was {Status}: {CustomData}", remotePubkey, response.Status, msg);
                return response.Status == Lnrpc.Payment.Types.PaymentStatus.Succeeded;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "EstablishCommunicationACK");
            }
            return false;
        }

        private void SaveLastIndexFromDb()
        {
            using (var db = Db)
            {
                var record = db.LNDSecureCommunicatorSettings.Single();
                record.InvoiceLastIndexOffset = InvoiceLastIndexOffset.Value;
                record.UpdateDate = DateTime.UtcNow;
                db.SaveChanges();

                _logger.LogInformation("Updated Invoice Offset: {InvoiceLastIndexOffset}", InvoiceLastIndexOffset.Value);
            }
        }

        private void TryUpdatingNodeIdForSettings()
        {
            using (var db = Db)
            {
                Settings = db.LNDSecureCommunicatorSettings.SingleOrDefault(x => x.Id == 1);
                if (Settings == null)
                {
                    _logger.LogInformation("Settings don't exist yet, will try next cycle.");
                    return;
                }
                if (Settings.NodePubkey.IsNullOrEmpty())
                {
                    Settings.NodePubkey = NodeConnection.LocalNodePubKey;
                    Settings.UpdateDate = DateTime.UtcNow;
                    db.SaveChanges();
                    _logger.LogInformation("Updated Node Pubkey in DB to {Pubkey}", Settings.NodePubkey);
                }
            }

        }

        private async Task CheckForNewNodePeerRequests()
        {
            using (var db = Db)
            {
                _logger.LogDebug("Check for new node peer requests");
                var lastIndex = InvoiceLastIndexOffset.Value;
                var invoices = NodeConnection.LightningClient.ListInvoices(new Lnrpc.ListInvoiceRequest { IndexOffset = InvoiceLastIndexOffset.Value, NumMaxInvoices = 100 });
                if (invoices.Invoices != null)
                {
                    _logger.LogDebug("{Count} invoices found.", invoices.Invoices.Count);
                    var messagesInKeysend = invoices.Invoices.Where(x => x.IsKeysend == true && x.Htlcs.Last().CustomRecords.Any(x => x.Key == EstablishCommunicationKeysendType || x.Key == EstablishCommunicationKeysendACKType));
                    _logger.LogDebug("{Count} matching keysends found.", messagesInKeysend.Count());
                    foreach (var keysendMessage in messagesInKeysend)
                    {
                        var connectMessages = keysendMessage.Htlcs.Last().CustomRecords.Where(x => x.Key == EstablishCommunicationKeysendType);
                        var ackMessages = keysendMessage.Htlcs.Last().CustomRecords.Where(x => x.Key == EstablishCommunicationKeysendACKType);
                        _logger.LogDebug("{Count} connect keysends found.", connectMessages.Count());
                        _logger.LogDebug("{Count} ack keysends found.", ackMessages.Count());

                        foreach (var connect in connectMessages) //Phase 1
                        {
                            //Format: node pubkey, node client auth pubkey, onion endpoint
                            var protocol = connect.Value.ToStringUtf8().Split(":");
                            var messageType = protocol[1];
                            if (messageType != "CONNECT")
                            {
                                _logger.LogError("Received CONNECT without valid construction");
                            }
                            else
                            {
                                var remotePubkey = protocol[2];
                                var clientAuthPubkey = protocol[3];
                                var onionEndpoint = protocol[4];
                                var sharedKey = (await NodeConnection.DeriveSharedKey(remotePubkey)).SharedKey;
                                var timeStamp = DateTime.UtcNow;
                                var remoteNode = new RemoteNode
                                {
                                    NodePubkey = remotePubkey,
                                    ClientAuthBase32PublicKey = clientAuthPubkey,
                                    CreatedDate = timeStamp,
                                    OnionAddress = onionEndpoint,
                                    SharedBase64PrivateKey = sharedKey.ToBase64(),
                                    UpdateDate = timeStamp,
                                    RemoteNodeACK = false
                                };
                                _logger.LogInformation("Received CONNECT from: {RemotePubkey} @ {OnionAddress}", remotePubkey, onionEndpoint);
                                var result = await EstablishCommunicationACK(remotePubkey);
                                remoteNode.RemoteNodeACK = true;
                                db.RemoteNodes.Add(remoteNode);
                                db.SaveChanges();
                            }
                        }
                        foreach (var ack in ackMessages) //Phase 2
                        {
                            //Format: node pubkey, node client auth pubkey, onion endpoint
                            var protocol = ack.Value.ToStringUtf8().Split(":");
                            var messageType = protocol[1];
                            if (messageType != "ACK")
                            {
                                _logger.LogError("Received ACK without valid construction");
                            }
                            else
                            {
                                var remotePubkey = protocol[2];
                                var clientAuthPubkey = protocol[3];
                                var onionEndpoint = protocol[4];
                                var sharedKey = (await NodeConnection.DeriveSharedKey(remotePubkey)).SharedKey;
                                var record = db.RemoteNodes.SingleOrDefault(x => x.NodePubkey == remotePubkey);
                                if (record == null)
                                {
                                    //Error we got an ACK without RemoteNode Record
                                    _logger.LogError("Received ACK without RemoteNode record: {RemotePubkey} @ {OnionAddress}", remotePubkey, onionEndpoint);

                                }
                                else
                                {
                                    var timeStamp = DateTime.UtcNow;
                                    record.RemoteNodeACK = true;
                                    record.ClientAuthBase32PublicKey = clientAuthPubkey;
                                    record.OnionAddress = onionEndpoint;
                                    db.SaveChanges();
                                    _logger.LogInformation("Received ACK from: {RemotePubkey} @ {OnionAddress}", remotePubkey, onionEndpoint);
                                }
                            }
                        }
                        //if (keysendMessage.AddIndex > InvoiceLastIndexOffset)
                        //{
                        //    InvoiceLastIndexOffset = keysendMessage.AddIndex;
                        //}
                    }
                    if (invoices.Invoices.Count > 0)
                    {
                        if (invoices.Invoices.Last().AddIndex > InvoiceLastIndexOffset)
                        {
                            InvoiceLastIndexOffset = invoices.Invoices.Last().AddIndex;
                        }
                    }
                    if (lastIndex != InvoiceLastIndexOffset)
                    {

                        SaveLastIndexFromDb();
                    }

                }
                else
                {
                    //No invoices?
                    _logger.LogDebug("No invoices found.");
                }
            }

        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
