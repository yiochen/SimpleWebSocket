using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SimpleWebSocket
{
    internal class WebSocketClientNative : WebSocketClient
    {
        private ClientWebSocket m_Socket = new ClientWebSocket();

        private SynchronizationContext synchronizationContext;

        private CancellationTokenSource m_TokenSource;
        private CancellationToken m_CancellationToken;

        /// <summary>
        /// State of the connection.
        /// </summary>
        public override State State
        {
            get
            {
                switch (m_Socket.State)
                {
                    case WebSocketState.Connecting:
                        return State.Connecting;
                    case WebSocketState.Open:
                        return State.Open;
                    case WebSocketState.CloseSent:
                    case WebSocketState.CloseReceived:
                        return State.Closing;
                    case WebSocketState.Closed:
                        return State.Closed;
                    default:
                        return State.Closed;

                }
            }
        }

        internal WebSocketClientNative(string url, Dictionary<string, string> headers = null) : base(url, headers) { }

        internal WebSocketClientNative(string url, string subprotocol, Dictionary<string, string> headers = null) : base(url, subprotocol, headers) { }

        internal WebSocketClientNative(string url, List<string> subprotocols, Dictionary<string, string> headers = null) : base(url, subprotocols, headers) { }

        /// <summary>
        /// Connect to a WebSocket server as an asynchronous operation.
        /// </summary>
        /// <returns><c>Task</c> to await on.</returns>
        /// <remarks>
        /// The <see cref="Connect" /> methods will not block. The returned <c>Task</c>
        /// object will complete after connection is established and
        /// <c>OnOpen</c> has been called.
        /// <see cref="Connect" /> will capture the
        /// <c>SynchronizationContext</c> when called. Callbacks will be called
        /// using the <c>SynchronizationContext</c>.
        /// </remarks>
        public override async Task Connect()
        {
            synchronizationContext = SynchronizationContext.Current;
            m_Socket ??= new ClientWebSocket();
            m_TokenSource = new CancellationTokenSource();
            m_CancellationToken = m_TokenSource.Token;
            foreach (var header in headers)
            {
                m_Socket.Options.SetRequestHeader(header.Key, header.Value);
            }

            foreach (string subprotocol in subprotocols)
            {
                m_Socket.Options.AddSubProtocol(subprotocol);
            }

            try
            {
                await m_Socket.ConnectAsync(uri, m_CancellationToken);
            }
            catch (Exception e)
            {
                // connection error
                InvokeOnError(e.Message);
                await Close(WebSocketCloseStatus.Empty);
                return;
            }
            InvokeOnOpen();
            WaitForMessages();
        }
        /// <summary>
        /// Cancel any pending tasks and close the connection.
        /// </summary>
        /// <param name="closeStatus">Status to communicate to server, default
        /// to NormalClosure</param>
        /// <returns><c>Task</c> to await on.</returns>
        public override async Task Close(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            if (m_CancellationToken.IsCancellationRequested)
            {
                // cancellation already requested, no need to do anything
                return;
            }
            m_TokenSource?.Cancel();
            if (State == State.Open || State == State.Connecting)
            {
                await m_Socket.CloseAsync(closeStatus, string.Empty, CancellationToken.None);
            }
            m_Socket?.Dispose();
            // Close might be called on pooled thread
            await synchronizationContext.RunInContext(() =>
            {
                InvokeOnClose(closeStatus);
            });

        }

        private async Task<(WebSocketMessageType, byte[], WebSocketCloseStatus?)> ReceiveNext(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            WebSocketReceiveResult result = null;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await m_Socket.ReceiveAsync(buffer, m_CancellationToken);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                return (result.MessageType, ms.ToArray(), result.CloseStatus);
            }
        }

        private async void WaitForMessages()
        {
            await new SwitchToBackgroundThread();
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);
            try
            {
                while (m_Socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    if (m_CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var (messageType, message, closeStatus) = await ReceiveNext(buffer, m_CancellationToken);
                    if (messageType == WebSocketMessageType.Text || messageType == WebSocketMessageType.Binary)
                    {
                        await synchronizationContext.RunInContext(() =>
                        {
                            if (!m_CancellationToken.IsCancellationRequested)
                            {
                                InvokeOnMessage(message);
                            }
                        });
                    }
                    else if (messageType == WebSocketMessageType.Close)
                    {
                        await Close(closeStatus ?? WebSocketCloseStatus.Empty);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                if (!m_CancellationToken.IsCancellationRequested)
                {
                    await synchronizationContext.RunInContext(() =>
                    {
                        InvokeOnError(e.Message);
                    });
                }
                await Close(WebSocketCloseStatus.Empty);
            }

        }

        /// <summary>
        /// Send string data to server.
        /// </summary>
        /// <param name="text"></param>
        public override void SendText(string text)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(text);

            SendMessage(WebSocketMessageType.Text, new ArraySegment<byte>(encoded, 0, encoded.Length));
        }

        /// <summary>
        /// Send binary data to server.
        /// </summary>
        /// <param name="bytes"></param>
        public override void Send(byte[] bytes)
        {
            SendMessage(WebSocketMessageType.Binary, new ArraySegment<byte>(bytes, 0, bytes.Length));
        }

        private async void SendMessage(WebSocketMessageType messageType, ArraySegment<byte> buffer)
        {
            await new SwitchToBackgroundThread();
            // Make sure we have data.
            if (buffer.Count == 0)
            {
                return;
            }
            await m_Socket.SendAsync(buffer, messageType, true, m_CancellationToken);
        }
    }
}