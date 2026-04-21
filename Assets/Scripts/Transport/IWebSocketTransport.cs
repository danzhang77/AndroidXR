using System;
using System.Threading.Tasks;

namespace AndroidXR.Transport
{
    public interface IWebSocketTransport
    {
        bool IsOpen { get; }
        event Action Opened;
        event Action<string> Error;
        event Action Closed;

        Task Connect(string url);
        Task SendText(string message);
        Task Close();
        void Tick();
    }
}

