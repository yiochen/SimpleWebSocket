using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebSocket
{
    public delegate void OnOpenHandler();
    public delegate void OnMessageHandler(byte[] data);
    public delegate void OnErrorHandler(string errorMsg);
    public delegate void OnCloseHandler(WebSocketCloseStatus closeCode);
    public abstract class AbstractWebSocketClient
    {
        public event OnOpenHandler OnOpen;
        public event OnMessageHandler OnMessage;
        public event OnErrorHandler OnError;
        public event OnCloseHandler OnClose;
        protected Uri uri;
        protected Dictionary<string, string> headers;
        protected List<string> subprotocols;

        public abstract State State { get; }

        protected AbstractWebSocketClient(string url, Dictionary<string, string> headers = null) : this(url, new List<string>(), headers) { }

        protected AbstractWebSocketClient(string url, string subprotocol, Dictionary<string, string> headers = null) : this(url, new List<string> { subprotocol }, headers) { }

        public AbstractWebSocketClient(string url, List<string> subprotocols, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);

            this.headers = headers ?? new Dictionary<string, string>();

            this.subprotocols = subprotocols;

            string protocol = uri.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }

        protected void InvokeOnOpen()
        {
            OnOpen?.Invoke();
        }

        protected void InvokeOnMessage(byte[] data)
        {
            OnMessage?.Invoke(data);
        }

        protected void InvokeOnError(string errorMsg)
        {
            OnError?.Invoke(errorMsg);
        }

        protected void InvokeOnClose(WebSocketCloseStatus code)
        {
            OnClose?.Invoke(code);
        }
        public abstract Task Connect();

        public abstract Task Close(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure);

        public abstract void Send(byte[] bytes);

        public abstract void SendText(string text);

    }

    /// <summary>C# WebSocketState has 6 types
    /// (https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocketstate?view=net-5.0)
    /// while JavaScript only has 4.
    /// (https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/readyState)
    /// This converges the two implementations</summary>
    public enum State
    {
        Connecting,
        Open,
        Closing,
        Closed
    }

}