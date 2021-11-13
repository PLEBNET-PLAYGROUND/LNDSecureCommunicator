using LNDSecureCommunicator.ServiceInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceStack;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace LNDSecureCommunicator.Controllers
{
    public class WebSocketController : ControllerBase
    {
        public static ConcurrentQueue<DecodedMessage> NewMessages { get; set; } = new ConcurrentQueue<DecodedMessage>();

        [HttpGet("/ws/echo")]
        public async Task EchoWS()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await Echo(HttpContext, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        [HttpGet("/ws/messages")]

        public async Task MessagesWS()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await StreamMessages(HttpContext, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        private async Task StreamMessages(HttpContext httpContext, WebSocket webSocket)
        {
            while (true)
            {
                if (NewMessages.Count > 0)
                {
                    NewMessages.TryDequeue(out var message);
                    await webSocket.SendAsync(message.ToJson().ToUtf8Bytes(), WebSocketMessageType.Text, true, CancellationToken.None);
                    if (webSocket.State != WebSocketState.Open)
                        break;
                }
                await Task.Delay(250);
            }
        }

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None); 
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

    }
}
