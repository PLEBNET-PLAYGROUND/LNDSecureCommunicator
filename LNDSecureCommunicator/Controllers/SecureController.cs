using LNDroneController.Extentions;
using LNDroneController.LND;
using LNDSecureCommunicator.ServiceInterface;
using LNDSecureCommunicator.ServiceModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
namespace LNDSecureCommunicator.Controllers
{
    [ApiController]
    //[ApiKey]
    public class SecureController : ControllerBase
    {
        private ILogger<SecureController> logger;
        private LNDNodeConnection node;
        private LNDSecureContext Db { get { return Provider.GetRequiredService<LNDSecureContext>(); } }

        public IServiceProvider Provider { get; }

        public SecureController(ILogger<SecureController> logger,
            IServiceProvider provider, LNDSecureContext context, LNDNodeConnection lnd)
        {
            this.logger = logger;
            node = lnd;
            Provider = provider;
        }

        [HttpPost]
        [Route("/SendMessage")]
        public async Task<SendMessageResponse> SendMessage(SendMessage request)
        {
            using (var db = Db)
            {
                var conf = db.LNDSecureCommunicatorSettings.FirstOrDefault(x => x.Id == 1);
                if (conf == null)
                {
                    logger.LogError("LNDSecureCommunicatorSettings is not ready.");
                    throw new WebServiceException("LNDSecureCommunicatorSettings is not ready.");
                }
                var remoteNode = db.RemoteNodes.SingleOrDefault(x => x.NodePubkey == request.Pubkey);
                if (remoteNode == null)
                {
                    throw new WebServiceException($"Cannot find RemoteNode @ {request.Pubkey}");
                }

                var message = request.UTF8TextMessage.ToUtf8Bytes();
                var (encryptedValue, iv) = message.EncryptStringToAesBytes(Convert.FromBase64String(remoteNode.SharedBase64PrivateKey), null);

                var packet = new ReceiveMessage
                {
                    Pubkey = conf.NodePubkey,
                    IV = iv,
                    MessageData = encryptedValue,
                    MessageType = SecureMessageType.Message,
                };

                var proxy = new WebProxy();
                proxy.Address = new Uri("socks5://127.0.0.1:9050");
                using (var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true,
                    
                })
                {
                    using (var client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(90);
                        try
                        {
                            var response = await client.PostAsJsonAsync($"http://{remoteNode.OnionAddress}/ReceiveMessage", packet);
                            var content = await response.Content.ReadAsStringAsync();
                            var sendMessageResponse = content.FromJson<SendMessageResponse>();
                            return new SendMessageResponse { Success = sendMessageResponse.Success };
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "SendMessage");
                        }
                    }
                }
                return new SendMessageResponse { Success = false };
            }

        }

        [HttpPost]
        [Route("/ReceiveMessage")]
        public async Task<ReceiveMessageResponse> ReceiveMessage(ReceiveMessage request)
        {
            using (var db = Db)
            {
                var remoteNode = db.RemoteNodes.SingleOrDefault(x => x.NodePubkey == request.Pubkey);
                if (remoteNode == null)
                {
                    throw new WebServiceException($"Cannot find RemoteNode @ {request.Pubkey}");
                }

                if (request.MessageType == SecureMessageType.Message)
                {
                    var decrypted = request.MessageData.DecryptStringFromBytesAes(Convert.FromBase64String(remoteNode.SharedBase64PrivateKey), request.IV).FromUtf8Bytes();
                    logger.LogInformation("Message from {Pubkey}: {Message}", request.Pubkey, decrypted);
                    var record = new DecodedMessage
                    {
                        CreatedDate = DateTime.UtcNow,
                        Message = decrypted,
                        NodePubkey = remoteNode.NodePubkey,
                    };
                    db.DecodedMessages.Add(record);
                    db.SaveChanges();
                    WebSocketController.NewMessages.Enqueue(record);
                }
                else
                {
                    var filename = request.MessageData.DecryptStringFromBytesAes(Convert.FromBase64String(remoteNode.SharedBase64PrivateKey), request.IV).FromUtf8Bytes();
                    var fileData = request.MessageData.DecryptStringFromBytesAes(Convert.FromBase64String(remoteNode.SharedBase64PrivateKey), request.IV);
                    logger.LogInformation("Message from {Pubkey}: {Filename} of {Size} bytes", request.Pubkey, filename, fileData.Length);
                    var record = new DecodedMessage
                    {
                        CreatedDate = DateTime.UtcNow,
                        Message = filename,
                        FileData = fileData,
                        NodePubkey = remoteNode.NodePubkey,
                    };
                    db.DecodedMessages.Add(record);
                    db.SaveChanges();
                }
            }

            return new ReceiveMessageResponse { Success = true };
        }


        [HttpPost]
        [Route("/connect")]
        public async Task<ConnectToPeerResponse> Connect(ConnectToPeer req)
        {
            var lightning = Provider.GetRequiredService<ILightningBackgroundService>();
            using (var db = Db)
            {
                var conf = db.LNDSecureCommunicatorSettings.FirstOrDefault(x => x.Id == 1);
                if (conf == null)
                {
                    logger.LogError("LNDSecureCommunicatorSettings is not ready.");
                    throw new WebServiceException("LNDSecureCommunicatorSettings is not ready.");
                }
                if (db.RemoteNodes.Any(x => x.NodePubkey == req.Pubkey))
                {
                    logger.LogError("Node {Pubkey} already exists.", req.Pubkey);
                    throw new WebServiceException($"Node {req.Pubkey} already exists.");
                }
                var success = await lightning.EstablishCommunication(req.Pubkey);

                return new ConnectToPeerResponse { Success = success };
            }
        }

        [HttpPost]
        [Route("/disconnect")]
        public async Task<DisconnectFromPeerResponse> Disconnect(DisconnectFromPeer req)
        {
            using (var db = Db)
            {
                var lightning = Provider.GetRequiredService<ILightningBackgroundService>();

                var conf = db.LNDSecureCommunicatorSettings.FirstOrDefault(x => x.Id == 1);
                if (conf == null)
                {
                    logger.LogError("LNDSecureCommunicatorSettings is not ready.");
                    throw new WebServiceException("LNDSecureCommunicatorSettings is not ready.");
                }
                var node = db.RemoteNodes.SingleOrDefault(x => x.NodePubkey == req.Pubkey);
                if (node == null)
                {
                    logger.LogError("Node {Pubkey} does not exist.", req.Pubkey);
                    throw new WebServiceException($"Node {req.Pubkey} does not exist.");
                }
                else
                {
                    var result = await lightning.DisableCommunication(req.Pubkey);
                    return new DisconnectFromPeerResponse { Success = result };
                }
            }
        }

        [HttpPost]
        [Route("/Message/List")]
        public async Task<List<DecodedMessage>> ListMessages(ListMessages req)
        {
            using (var db = Db)
            {
                var query = db.DecodedMessages.Where(x => x.NodePubkey == req.NodePubkey);
                if (req.Take.HasValue)
                {
                    query = query.Take(req.Take.Value);
                }
                if (req.Skip.HasValue)
                {
                    query = query.Skip(req.Skip.Value);
                }
                return query.ToList();
            }
        }

        [HttpPost]
        [Route("/Message/Count")]
        public async Task<CountMessagesResponse> CountMessages(CountMessages req)
        {
            using (var db = Db)
            {
                return new CountMessagesResponse { Count = db.DecodedMessages.Count() } ;
            }
        }
    }
     
}
  