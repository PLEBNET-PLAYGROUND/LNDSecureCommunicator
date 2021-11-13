using System;
using System.Threading.Tasks;
using CommandLine;
using ServiceStack;
using ServiceStack.Text;
using LNDSecureCommunicator.ServiceModel;
using System.Net.Http.Json;
using System.Net.WebSockets;
using LNDSecureCommunicator.ServiceInterface;

namespace LNDSecureCommunicator.CLI
{
    class Program
    {
        private static CancellationTokenSource cts;
        private static ClientWebSocket ws;

        public static string URL { get; private set; } = "http://localhost:5001";

        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(async (CommandLineOptions opts) =>
                   {
                       if (!opts.URL.IsNullOrEmpty())
                       {
                           URL = opts.URL;
                       }
                       if (opts.EchoTest)
                       {
                           await EchoTest();
                       }
                       if (opts.TalkMode)
                       {
                           await TalkMode(opts.RemotePubkey);
                       }
                       if (opts.ListenMode)
                       {
                           await ListenMode();
                       }
                       if (opts.Connect)
                       {
                           var result = await ConnectNode(opts.RemotePubkey);
                       }
                       else if (opts.Disconnect)
                       {
                           var result = await DisconnectNode(opts.RemotePubkey);

                       }
                       else if (!opts.Message.IsNullOrEmpty())
                       {
                           var result = await SendMessage(opts.RemotePubkey, opts.Message);
                       }
                       return 0;
                   },
                   errs => Task.FromResult(-1)); // Invalid arguments
        }

        private static async Task ListenMode()
        {
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            var endpoint = URL.Replace("http:", "ws:") + "/ws/messages";
            await ws.ConnectAsync(new Uri(endpoint), cts.Token);
            var task = Task.Factory.StartNew(TalkLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            while (true)
            {
                await Task.Delay(100);
                while (q.Count > 0)
                {
                    var message = q.Dequeue();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{message.NodePubkey}: {message.Message}");
                    Console.ResetColor();
                }
            }
        }
        private static async Task TalkMode(string remotePubkey)
        {


            Console.WriteLine("Type: !quit to exit the loop");
            while (true)
            {
                Console.Write("Local: ");
                var line = Console.ReadLine();
                if (line == "!quit")
                {
                    cts.Cancel();
                    return;
                }
                var send = await SendMessage(remotePubkey, line);
                if (!send)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("TRANSMISSION ERROR");
                    Console.ResetColor();
                }
                while (q.Count > 0)
                {
                    var message = q.Dequeue();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{message.NodePubkey}: {message.Message}");
                    Console.ResetColor();
                }
            }
        }
        private static Queue<DecodedMessage> q = new Queue<DecodedMessage>();
        private static async Task TalkLoop()
        {
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[4096];
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(4096);
                    do
                    {
                        receiveResult = await ws.ReceiveAsync(buffer, cts.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    var message = outputStream.ToArray().FromUtf8Bytes().FromJson<DecodedMessage>();
                  
                    q.Enqueue(message);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        private static async Task EchoTest()
        {
            var endpoint = URL.Replace("http:", "ws:") + "/ws/echo";

            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(endpoint), cts.Token);
            var task = Task.Factory.StartNew(ReceiveLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Console.WriteLine("Type: !quit to exit the loop");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "!quit")
                {
                    cts.Cancel();
                    task.Wait();
                    return;
                }
                await ws.SendAsync(line.ToAsciiBytes(), WebSocketMessageType.Text, true, cts.Token);
            }

        }

        private static async Task ReceiveLoop()
        {
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[4096];
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(4096);
                    do
                    {
                        receiveResult = await ws.ReceiveAsync(buffer, cts.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    outputStream.ReadToEnd().Print();
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        private static async Task<bool> SendMessage(string remotePubkey, string message)
        {
            var endpoint = URL + "/SendMessage";
            var result = (await endpoint.PostJsonToUrlAsync(new SendMessage
            {
                Pubkey = remotePubkey,
                UTF8TextMessage = message,

            })).FromJson<SendMessageResponse>();
            return result.Success;
        }

        private static async Task<bool> ConnectNode(string remotePubkey)
        {
            var endpoint = URL + "/connect";
            var result = (await endpoint.PostJsonToUrlAsync(new ConnectToPeer
            {
                Pubkey = remotePubkey
            })).FromJson<ConnectToPeerResponse>();
            return result.Success;
        }

        private static async Task<bool> DisconnectNode(string remotePubkey)
        {
            var endpoint = URL + "/disconnect";
            var result = (await endpoint.PostJsonToUrlAsync(new DisconnectFromPeer { Pubkey = remotePubkey })).FromJson<DisconnectFromPeerResponse>();
            return result.Success;
        }
    }
}
