using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace LNDSecureCommunicator.CLI
{
    public class CommandLineOptions
    {
        [Option(shortName: 'n', longName: "node", Required = false, HelpText = "Node RemotePubkey")]
        public string RemotePubkey { get; set; }
        [Option(shortName: 'c', longName: "connect", Required = false, HelpText = "Connect to peer")]
        public bool Connect { get; set; }
        [Option(shortName: 'd', longName: "disconnect", Required = false, HelpText = "Disconnect to peer")]
        public bool Disconnect { get; set; }
        [Option(shortName: 'm', longName: "message", Required = false, HelpText = "Send short text message")]
        public string Message { get; set; }
        [Option(shortName: 'f', longName: "file", Required = false, HelpText = "Send file")]
        public string File { get; set; }
        [Option(shortName: 'u', longName: "url", Required = false, HelpText = @"Daemon URL (default: http://localhost:5001")]
        public string URL { get; set; }

        [Option(shortName: 'e', longName: "echotest", Required = false, HelpText = "Echotest")]
        public bool EchoTest { get; set; }
        [Option(shortName: 't', longName: "talk", Required = false, HelpText = "Chat Mode")]
        public bool TalkMode { get; set; }
        [Option(shortName: 'l', longName: "listen", Required = false, HelpText = "Listen Mode")]
        public bool ListenMode { get; set; }
        [Usage(ApplicationAlias = "lndscli")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Connect To Node", new CommandLineOptions { RemotePubkey = "03degsdfgsdfg", Connect = true});
                yield return new Example("Disconnect To Node", new CommandLineOptions { RemotePubkey = "03degsdfgsdfg", Disconnect = true});
                yield return new Example("Send Message To Node", new CommandLineOptions { RemotePubkey = "03degsdfgsdfg", Message = "Hello World!" });
                yield return new Example("Send File To Node", new CommandLineOptions { RemotePubkey = "03degsdfgsdfg", File = "./file.tar" });
                yield return new Example("Custom Daemon endpoint", new CommandLineOptions { RemotePubkey = "03degsdfgsdfg", File = "./file.tar", URL = "http://myserver:5001" });
                yield return new Example("Echo Test", new CommandLineOptions { EchoTest = true, URL = "http://myserver:5001" });
                yield return new Example("Talk Mode", new CommandLineOptions { TalkMode = true, URL = "http://myserver:5001" });
                yield return new Example("Listen Mode", new CommandLineOptions { ListenMode = true, URL = "http://myserver:5001" });
            }
        }

    }
}
