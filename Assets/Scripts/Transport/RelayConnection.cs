using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AndroidXR.Protocol;
using UnityEngine;

namespace AndroidXR.Transport
{
    public sealed class RelayConnection
    {
        private readonly IWebSocketTransport transport;
        private readonly Queue<string> pendingMessages = new();
        private int sequence = 1;
        private string url;

        public bool IsOpen => transport.IsOpen;

        public RelayConnection(IWebSocketTransport transport)
        {
            this.transport = transport;
            this.transport.Opened += HandleOpened;
            this.transport.Error += message => Debug.LogWarning($"Relay WebSocket error: {message}");
            this.transport.Closed += () => Debug.Log("Relay WebSocket closed.");
        }

        public async Task Connect(string relayUrl)
        {
            url = relayUrl;
            await transport.Connect(relayUrl);
        }

        public void Tick()
        {
            transport.Tick();
        }

        public async Task Close()
        {
            await transport.Close();
        }

        public void SendClientHello()
        {
            var payload = new ClientHelloPayload
            {
                role = XrProtocolConstants.SourceAndroid,
                clientName = SystemInfo.deviceName,
                capabilities = new[] { XrProtocolConstants.TrackpadTouch, XrProtocolConstants.KeyboardCommit },
            };

            Send(XrProtocolConstants.ClientHello, XrProtocolConstants.TargetRelay, payload);
        }

        public void Send<TPayload>(string type, string target, TPayload payload)
        {
            var json = XrMessageFactory.ToJson(type, target, sequence++, payload);
            if (!transport.IsOpen)
            {
                pendingMessages.Enqueue(json);
                return;
            }

            _ = transport.SendText(json);
        }

        private void HandleOpened()
        {
            Debug.Log($"Relay WebSocket connected: {url}");
            SendClientHello();

            while (pendingMessages.Count > 0 && transport.IsOpen)
            {
                _ = transport.SendText(pendingMessages.Dequeue());
            }
        }
    }
}

