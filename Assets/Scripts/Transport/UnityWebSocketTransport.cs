using System;
using System.Threading.Tasks;
using NativeWebSocket;

namespace AndroidXR.Transport
{
    public sealed class UnityWebSocketTransport : IWebSocketTransport
    {
        private WebSocket socket;

        public bool IsOpen => socket != null && socket.State == WebSocketState.Open;

        public event Action Opened;
        public event Action<string> Error;
        public event Action Closed;

        public async Task Connect(string url)
        {
            await Close();

            socket = new WebSocket(url);
            socket.OnOpen += () => Opened?.Invoke();
            socket.OnError += message => Error?.Invoke(message);
            socket.OnClose += _ => Closed?.Invoke();

            await socket.Connect();
        }

        public async Task SendText(string message)
        {
            if (!IsOpen)
            {
                return;
            }

            await socket.SendText(message);
        }

        public async Task Close()
        {
            if (socket == null)
            {
                return;
            }

            var closingSocket = socket;
            socket = null;
            await closingSocket.Close();
        }

        public void Tick()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            socket?.DispatchMessageQueue();
#endif
        }
    }
}

