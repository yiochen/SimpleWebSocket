using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace SimpleWebSocket
{
    public delegate void OnOpenHandler();
    public delegate void OnMessageHandler(byte[] data);
    public delegate void OnErrorHandler(string errorMsg);
    public delegate void OnCloseHandler(WebSocketCloseStatus closeCode);
    public abstract class WebSocketClient
    {
        /// <summary>
        /// Callback when the connection is open but before <c>Connect</c>
        /// resolves.
        /// </summary>
        public event OnOpenHandler OnOpen;
        /// <summary>
        /// Callback when receiving a message from server.
        /// </summary>
        public event OnMessageHandler OnMessage;
        /// <summary>
        /// Callback when encountering an error during connection or when socket
        /// is open.
        /// </summary>
        public event OnErrorHandler OnError;
        /// <summary>
        /// Callback when the socket is closed, either initiated by server or by
        /// client.
        /// </summary>
        public event OnCloseHandler OnClose;
        protected Uri uri;
        protected Dictionary<string, string> headers;
        protected List<string> subprotocols;

        public abstract State State { get; }

        protected WebSocketClient(string url, Dictionary<string, string> headers = null) : this(url, new List<string>(), headers) { }

        protected WebSocketClient(string url, string subprotocol, Dictionary<string, string> headers = null) : this(url, new List<string> { subprotocol }, headers) { }

        public WebSocketClient(string url, List<string> subprotocols, Dictionary<string, string> headers = null)
        {
            uri = new Uri(url);

            this.headers = headers ?? new Dictionary<string, string>();

            this.subprotocols = subprotocols;

            string protocol = uri.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
                throw new ArgumentException("Unsupported protocol: " + protocol);
        }

        public void InvokeOnOpen()
        {
            OnOpen?.Invoke();
        }

        public void InvokeOnMessage(byte[] data)
        {
            OnMessage?.Invoke(data);
        }

        public void InvokeOnError(string errorMsg)
        {
            OnError?.Invoke(errorMsg);
        }

        public void InvokeOnClose(WebSocketCloseStatus code)
        {
            OnClose?.Invoke(code);
        }
        public abstract Task Connect();

        public abstract Task Close(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure);

        public abstract void Send(byte[] bytes);

        public abstract void SendText(string text);

        public static WebSocketClient Create(string url, Dictionary<string, string> headers = null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new WebSocketClientNative(url, headers);
#else
            return new WebSocketClientWebgl(url, headers);
#endif

        }

        public static WebSocketClient Create(string url, string subprotocol, Dictionary<string, string> headers = null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new WebSocketClientNative(url, subprotocol, headers);
#else
            return new WebSocketClientWebgl(url, subprotocol, headers);
#endif
        }

        public static WebSocketClient Create(string url, List<string> subprotocols, Dictionary<string, string> headers = null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new WebSocketClientNative(url, subprotocols, headers);
#else
            return new WebSocketClientWebgl(url, subprotocols, headers);
#endif
        }
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