using NUnit.Framework;
using ServiceStack;
using ServiceStack.Testing;
using LNDSecureCommunicator.ServiceInterface;
using LNDSecureCommunicator.ServiceModel;
using LNDroneController.LND;
using System.Threading.Tasks;
using ServiceStack.Text;
using LNDroneController.Extentions;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Net;

namespace LNDSecureCommunicator.Tests
{
    public class UnitTest
    {
        private readonly ServiceStackHost appHost;

        public LNDNodeConnection Bob { get; }
        public LNDNodeConnection Alice { get; }

        public UnitTest()
        {
            appHost = new BasicAppHost().Init();


            Bob = new LNDNodeConnection(new LNDSettings
            {
                GrpcEndpoint = "https://127.0.0.1:10008",
                TLSCertPath = @"C:\Users\rjs\.polar\networks\1\volumes\lnd\bob\tls.cert",
                MacaroonPath = @"C:\Users\rjs\.polar\networks\1\volumes\lnd\bob\data\chain\bitcoin\regtest\admin.macaroon",
                MaxFeePercentage = 1,
                MinFeeSats = 10,
                MaxFeeSats = 250
            });
            Alice = new LNDNodeConnection(new LNDSettings
            {
                GrpcEndpoint = "https://127.0.0.1:10004",
                TLSCertPath = @"C:\Users\rjs\.polar\networks\1\volumes\lnd\alice\tls.cert",
                MacaroonPath = @"C:\Users\rjs\.polar\networks\1\volumes\lnd\alice\data\chain\bitcoin\regtest\admin.macaroon",
                MaxFeePercentage = 1,
                MinFeeSats = 10,
                MaxFeeSats = 250
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public async Task TestNodes()
        {
            var a = await Alice.GetInfo();
            var b = await Bob.GetInfo();
            a.PrintDump();
            b.PrintDump();
        }

        [Test]
        public async Task SharedKeysAreEqual()
        {

            var aliceSharedKey = await Alice.DeriveSharedKey(Bob.LocalNodePubKey);
            var bobSharedKey = await Bob.DeriveSharedKey(Alice.LocalNodePubKey);
            Assert.That(aliceSharedKey.SharedKey == bobSharedKey.SharedKey);
            aliceSharedKey.SharedKey.ToBase64().Print();
        }

        [Test]
        public async Task EstablishLinkCommunication()
        {
            ulong establishMessageKeysendType = 343493346969L;
            var custom = new Dictionary<ulong, byte[]>();
            custom.Add(establishMessageKeysendType, "LNDSecureCommunicator:MyNodeIDHere:somethingsomething.onion".ToUtf8Bytes());
            var aliceSharedKey = await Alice.DeriveSharedKey(Bob.LocalNodePubKey);
            var bobSharedKey = await Bob.DeriveSharedKey(Alice.LocalNodePubKey);
            Assert.That(aliceSharedKey.SharedKey == bobSharedKey.SharedKey);
            var response = await Alice.KeysendPayment(Bob.LocalNodePubKey, 10, 10,null,60, custom);
            Assert.That(response.Status == Lnrpc.Payment.Types.PaymentStatus.Succeeded);
            var invoices = Bob.LightningClient.ListInvoices(new Lnrpc.ListInvoiceRequest { NumMaxInvoices = 100, Reversed = true }).Invoices;
            var exact = invoices.Where(x => x.IsKeysend == true && x.Htlcs.Last().CustomRecords.Any(x => x.Key == establishMessageKeysendType));
            foreach(var x in exact)
            {
                var z = x.Htlcs.Last().CustomRecords.Where(x => x.Key == establishMessageKeysendType);
                foreach(var z1 in z)
                {
                    $"{z1.Value.ToStringUtf8()}".Print();
                }
            }
        }
        [Test]
        public async Task TestSOCKProxy()
        {
            var proxy = new WebProxy();
            proxy.Address = new Uri("socks5://127.0.0.1:9050");
            //proxy.Credentials = new NetworkCredential(); //Used to set Proxy logins. 
            var handler = new HttpClientHandler
            {
                Proxy = proxy
            };
            var client = new HttpClient(handler);
            var res = await client.GetAsync("http://645z42qwxj2zym23kqi7cwn5gdhnnxu3gxqzfxtyoymtc2lobs7bsmqd.onion/metadata");
            Assert.That(res.StatusCode == System.Net.HttpStatusCode.OK);
        }
        [Test]
        public Task InvoiceCheck()
        {
            var invoices = Bob.LightningClient.ListInvoices(new Lnrpc.ListInvoiceRequest { IndexOffset = 0 });
            invoices.Invoices?.Count().Print();
            invoices.LastIndexOffset.ToString().Print();
            invoices = Bob.LightningClient.ListInvoices(new Lnrpc.ListInvoiceRequest { IndexOffset = invoices.LastIndexOffset, NumMaxInvoices = 100 });
            invoices.Invoices?.Count().Print();
            invoices.LastIndexOffset.ToString().Print();
            return Task.CompletedTask;
        }

        [Test]
        public async Task ECDH_AES_FullCycle()
        { 

            var aliceSharedKey = await Alice.DeriveSharedKey(Bob.LocalNodePubKey);
            var bobSharedKey = await Bob.DeriveSharedKey(Alice.LocalNodePubKey);
            Assert.That(aliceSharedKey.SharedKey == bobSharedKey.SharedKey);
            var message = "Hello World!".ToUtf8Bytes();
            var(encryptedValue, iv) = message.EncryptStringToAesBytes(aliceSharedKey.SharedKey.ToByteArray(),null);
            var dencryptedValue = encryptedValue.DecryptStringFromBytesAes(bobSharedKey.SharedKey.ToByteArray(), iv);
            Assert.That(message.FromUtf8Bytes() == dencryptedValue.FromUtf8Bytes());
        }

  
}
}
