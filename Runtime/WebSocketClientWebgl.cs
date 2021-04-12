using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using AOT;
using System.Runtime.InteropServices;

namespace SimpleWebSocket
{
    internal class WebSocketClientWebgl : WebSocketClient
    {

        private int instanceId;

        private TaskCompletionSource<bool> ConnectingTask = null;
        private TaskCompletionSource<bool> ClosingTask = null;

        public override State State
        {
            get
            {
                return State.Connecting;
            }
        }

        /* WebSocket JSLIB functions */
        [DllImport("__Internal")] public static extern int WebSocketConnect(int instanceId);

        [DllImport("__Internal")] public static extern int WebSocketClose(int instanceId, int code, string reason);

        [DllImport("__Internal")] public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")] public static extern int WebSocketSendText(int instanceId, string message);

        [DllImport("__Internal")] public static extern int WebSocketGetState(int instanceId);

        internal WebSocketClientWebgl(string url, Dictionary<string, string> headers = null) : this(url, new List<string>(), headers)
        { }

        internal WebSocketClientWebgl(string url, string subprotocol, Dictionary<string, string> headers = null) : this(url, new List<string> { subprotocol }, headers)
        { }

        internal WebSocketClientWebgl(string url, List<string> subprotocols, Dictionary<string, string> headers = null) : base(url, subprotocols, headers)
        {
            if (!WebSocketFactory.isInitialized)
            {
                WebSocketFactory.Initialize();
            }

            int instanceId = WebSocketFactory.WebSocketAllocate(url);
            WebSocketFactory.instances.Add(instanceId, this);

            foreach (string subprotocol in subprotocols)
            {
                WebSocketFactory.WebSocketAddSubProtocol(instanceId, subprotocol);
            }

            this.instanceId = instanceId;
        }

        ~WebSocketClientWebgl()
        {
            WebSocketFactory.HandleInstanceDestroy(this.instanceId);
        }

        public override Task Connect()
        {
            int ret = WebSocketConnect(this.instanceId);
            if (ret < 0)
            {
                throw GetErrorMessageFromCode(ret, null);
            }
            ConnectingTask = new TaskCompletionSource<bool>();
            return ConnectingTask.Task;
        }

        public override Task Close(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            int ret = WebSocketClose(this.instanceId, (int)closeStatus, "");

            if (ret < 0)
            {
                throw GetErrorMessageFromCode(ret, null);
            }

            ClosingTask = new TaskCompletionSource<bool>();

            return ClosingTask.Task;
        }

        internal void TrySetCloseResult()
        {
            if (ClosingTask != null)
            {
                ClosingTask.TrySetResult(true);
                ClosingTask = null;
            }
        }

        internal void TrySetConnectResult()
        {
            if (ConnectingTask != null)
            {
                ConnectingTask.TrySetResult(true);
                ConnectingTask = null;
            }
        }

        public override void Send(byte[] bytes)
        {
            int ret = WebSocketSend(this.instanceId, bytes, bytes.Length);

            if (ret < 0)
            {
                throw GetErrorMessageFromCode(ret, null);
            }
        }

        public override void SendText(string text)
        {
            int ret = WebSocketSendText(this.instanceId, text);

            if (ret < 0)
            {
                throw GetErrorMessageFromCode(ret, null);
            }
        }

        private static WebSocketException GetErrorMessageFromCode(int errorCode, Exception inner)
        {
            switch (errorCode)
            {
                case -1:
                    return new WebSocketUnexpectedException("WebSocket instance not found.", inner);
                case -2:
                    return new WebSocketInvalidStateException("WebSocket is already connected or in connecting state.", inner);
                case -3:
                    return new WebSocketInvalidStateException("WebSocket is not connected.", inner);
                case -4:
                    return new WebSocketInvalidStateException("WebSocket is already closing.", inner);
                case -5:
                    return new WebSocketInvalidStateException("WebSocket is already closed.", inner);
                case -6:
                    return new WebSocketInvalidStateException("WebSocket is not in open state.", inner);
                case -7:
                    return new WebSocketInvalidArgumentException("Cannot close WebSocket. An invalid code was specified or reason is too long.", inner);
                default:
                    return new WebSocketUnexpectedException("Unknown error.", inner);
            }
        }
    }

    static class WebSocketFactory
    {
        /* Map of websocket instances */
        internal static Dictionary<Int32, WebSocketClientWebgl> instances = new Dictionary<Int32, WebSocketClientWebgl>();

        /* Delegates */
        public delegate void OnOpenCallback(int instanceId);
        public delegate void OnMessageCallback(int instanceId, System.IntPtr msgPtr, int msgSize);
        public delegate void OnErrorCallback(int instanceId, System.IntPtr errorPtr);
        public delegate void OnCloseCallback(int instanceId, int closeCode);

        /* WebSocket JSLIB callback setters and other functions */
        [DllImport("__Internal")]
        public static extern int WebSocketAllocate(string url);

        [DllImport("__Internal")]
        public static extern int WebSocketAddSubProtocol(int instanceId, string subprotocol);

        [DllImport("__Internal")]
        public static extern void WebSocketFree(int instanceId);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnOpen(OnOpenCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnMessage(OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnError(OnErrorCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnClose(OnCloseCallback callback);

        /* If callbacks was initialized and set */
        public static bool isInitialized = false;

        /// <summary>
        /// Initialize WebSocket callbacks to JSLIB. This serves the function as
        /// a constructor for this static class.
        /// </summary>
        public static void Initialize()
        {

            WebSocketSetOnOpen(RouteOnOpenEvent);
            WebSocketSetOnMessage(RouteOnMessageEvent);
            WebSocketSetOnError(RouteOnErrorEvent);
            WebSocketSetOnClose(RouteOnCloseEvent);

            isInitialized = true;
        }

        /// <summary>
        /// Called when instance is destroyed (by destructor)
        /// Method removes instance from map and free it in JSLIB implementation
        /// </summary>
        /// <param name="instanceId">Instance identifier.</param>
        public static void HandleInstanceDestroy(int instanceId)
        {

            instances.Remove(instanceId);
            WebSocketFree(instanceId);

        }

        [MonoPInvokeCallback(typeof(OnOpenCallback))]
        public static void RouteOnOpenEvent(int instanceId)
        {

            if (instances.TryGetValue(instanceId, out var instance))
            {
                instance.InvokeOnOpen();
                instance.TrySetConnectResult();
            }

        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        public static void RouteOnMessageEvent(int instanceId, System.IntPtr msgPtr, int msgSize)
        {

            if (instances.TryGetValue(instanceId, out var instanceRef))
            {
                byte[] msg = new byte[msgSize];
                Marshal.Copy(msgPtr, msg, 0, msgSize);
                instanceRef.InvokeOnMessage(msg);
            }

        }

        [MonoPInvokeCallback(typeof(OnErrorCallback))]
        public static void RouteOnErrorEvent(int instanceId, System.IntPtr errorPtr)
        {


            if (instances.TryGetValue(instanceId, out var instanceRef))
            {
                string errorMsg = Marshal.PtrToStringAuto(errorPtr);
                instanceRef.InvokeOnError(errorMsg);

            }

        }

        [MonoPInvokeCallback(typeof(OnCloseCallback))]
        public static void RouteOnCloseEvent(int instanceId, int closeCode)
        {

            if (instances.TryGetValue(instanceId, out var instanceRef))
            {
                // todo convert from closeCode to close status
                instanceRef.InvokeOnClose(WebSocketCloseStatus.Empty);
                instanceRef.TrySetConnectResult();
                instanceRef.TrySetCloseResult();
            }
        }
    }

    public class WebSocketException : Exception
    {
        public WebSocketException() { }
        public WebSocketException(string message) : base(message) { }
        public WebSocketException(string message, Exception inner) : base(message, inner) { }
    }

    public class WebSocketUnexpectedException : WebSocketException
    {
        public WebSocketUnexpectedException() { }
        public WebSocketUnexpectedException(string message) : base(message) { }
        public WebSocketUnexpectedException(string message, Exception inner) : base(message, inner) { }
    }

    public class WebSocketInvalidArgumentException : WebSocketException
    {
        public WebSocketInvalidArgumentException() { }
        public WebSocketInvalidArgumentException(string message) : base(message) { }
        public WebSocketInvalidArgumentException(string message, Exception inner) : base(message, inner) { }
    }

    public class WebSocketInvalidStateException : WebSocketException
    {
        public WebSocketInvalidStateException() { }
        public WebSocketInvalidStateException(string message) : base(message) { }
        public WebSocketInvalidStateException(string message, Exception inner) : base(message, inner) { }
    }
}