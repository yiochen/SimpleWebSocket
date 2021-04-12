using System.Collections.Generic;
using UnityEngine;
// using UnityWebSocket;
using UnityEngine.UI;
using SimpleWebSocket;

public class Demo : MonoBehaviour
{
    [SerializeField] private RectTransform DialogListView;
    [SerializeField] private Text MessageItemAsset;
    [SerializeField] private Button ConnectButton;
    [SerializeField] private Button DisconnectButton;
    [SerializeField] private Button SendButton;
    [SerializeField] private Button DoubleSendButton;
    [SerializeField] private InputField HostInput;
    [SerializeField] private InputField MessageInput;
    [SerializeField] private float NextTop = 0;

    private List<string> m_DialogList = new List<string>();
    private WebSocketClient m_Client;

    private System.Threading.Thread m_MainThread;

    public void OnConnectClicked()
    {
        Connect(HostInput.text);
    }

    public void OnDisconnectClicked()
    {
        Disconnect();
    }

    public void OnSendClicked()
    {
        Send(MessageInput.text);
    }

    public void OnDoubleSendClicked()
    {
        Send($"{MessageInput.text} -- 1");
        Send($"{MessageInput.text} -- 2");
    }

    private void Send(string message)
    {
        Debug.Log($"start sending text {message}");
        m_Client?.SendText(message);
    }
    void Start()
    {
        m_MainThread = System.Threading.Thread.CurrentThread;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += (UnityEditor.PlayModeStateChange state) =>
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Disconnect();
            }
        };
#endif
    }

    private async void Connect(string endpoint)
    {
        Debug.Log("Start connecting");
        if (m_Client != null)
        {
            Debug.LogError("Already has a client, cannot connect");
            return;
        }
        m_Client = WebSocketClient.Create(endpoint);
        m_Client.OnOpen += () =>
        {
            Debug.Log($"OnOpen called on {m_Thread}");
            AddToDialogList($"Connected to {endpoint}");
        };
        m_Client.OnClose += (System.Net.WebSockets.WebSocketCloseStatus closeStatus) =>
        {
            Debug.Log($"OnClose called with closeStatus {closeStatus} on {m_Thread}");
            AddToDialogList($"Disconnected from {endpoint}");
        };
        m_Client.OnMessage += (byte[] message) =>
        {
            Debug.Log($"OnMessage called on {m_Thread}");
            AddToDialogList(System.Text.Encoding.UTF8.GetString(message));
        };
        m_Client.OnError += (string error) =>
        {
            Debug.Log($"OnError called on {m_Thread}");
            AddToDialogList(error);
        };
        await m_Client.Connect();
        Debug.Log($"Connected! on {m_Thread}");
    }

    private void AddToDialogList(string text)
    {
        Text message = Instantiate(MessageItemAsset, Vector3.zero, Quaternion.identity);
        message.transform.parent = DialogListView;
        RectTransform rect = message.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, NextTop);
        rect.SetRight(0);
        message.text = text;

        NextTop -= 50;
    }

    private async void Disconnect()
    {
        if (m_Client != null)
        {
            Debug.Log("Start disconnecting");
            await m_Client.Close();
            m_Client = null;
        }
    }

    private string m_Thread
    {
        get
        {
            UnityEngine.Assertions.Assert.IsNotNull(m_MainThread);
            if (m_MainThread.Equals(System.Threading.Thread.CurrentThread))
            {
                return "Unity thread";
            }

            return "pooled thread";
        }
    }
}
public static class RectTransformExtensions
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
}
