using System;
using System.Text;
using System.Net;
using System.Threading;
using NLog;
using System.Threading.Tasks;
using vtortola.WebSockets;
using System.IO;

namespace cor64.WebService
{
    public class WebSocketServer
    {
        private WebSocketListener m_WebSocketListener;
        private readonly CancellationToken m_SendCt = new CancellationToken();
        private readonly CancellationToken m_RevCt = new CancellationToken();
        private readonly CancellationToken m_AcceptCt = new CancellationToken();
        private WebSocket m_Socket;

        public event EventHandler ClientConnected;
        public event EventHandler ClientDisconnected;
        public event EventHandler<String> MessageRecieved;

        public WebSocketServer(IPAddress address, int port)
        {
            m_WebSocketListener = new WebSocketListener(new IPEndPoint(address, port));
        }

        public void Open()
        {
            m_WebSocketListener.Standards.RegisterStandard(new WebSocketFactoryRfc6455());

            Task.Factory.StartNew(ListenForClientAsync);
        }

        private async Task ListenForClientAsync()
        {
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            m_WebSocketListener.StartAsync();
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            while (true)
            {
                var ws = await m_WebSocketListener.AcceptWebSocketAsync(m_AcceptCt);

                OnClientConnected();

                if (ws != null)
                {
                    m_Socket = ws;

                    while (m_Socket.IsConnected)
                    {
                        var recievedMessage = await m_Socket.ReadMessageAsync(m_RevCt);

                        if (recievedMessage != null && recievedMessage.MessageType == WebSocketMessageType.Text) {
                            StreamReader reader = new StreamReader(recievedMessage);
                            OnMessageRecieved(reader.ReadToEnd());
                        }
                    }

                    OnClientDisconnected();
                    m_Socket.Dispose();
                    m_Socket = null;
                }
            }
        }

        public void Close()
        {
            m_WebSocketListener.Stop();
        }

        public void SendMessage(String message)
        {
            if (m_Socket == null)
                throw new NullReferenceException();

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (m_Socket.IsConnected)
            {
                m_Socket.WriteStringAsync(message, m_SendCt);
            }
        }

        protected virtual void OnClientConnected()
        {
            ClientConnected?.Invoke(this, new EventArgs());
        }

        protected virtual void OnMessageRecieved(String message)
        {
            MessageRecieved?.Invoke(this, message);
        }

        protected virtual void OnClientDisconnected()
        {
            ClientDisconnected?.Invoke(this, new EventArgs());
        }
    }
}
