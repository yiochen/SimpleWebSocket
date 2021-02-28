using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Net.WebSockets;
using SimpleWebSocket;

public class Demo : MonoBehaviour
{
    private ListView m_DialogListView;
    private VisualTreeAsset m_MessageItemAsset;
    private List<string> m_DialogList = new List<string>();
    private WebSocketClient m_Client;

    private System.Threading.Thread m_MainThread;

    private void OnEnable()
    {
        m_MessageItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Demo/MessageItem.uxml");

        var root = GetComponent<UIDocument>().rootVisualElement;
        var connectButton = root.Q<Button>("ConnectButton");
        var disconnectButton = root.Q<Button>("DisconnectButton");
        var sendButton = root.Q<Button>("SendButton");
        var doubleSendButton = root.Q<Button>("DoubleSendButton");
        var endpointInput = root.Q<TextField>("EndpointInput");
        var messageInput = root.Q<TextField>("MessageInput");
        m_DialogListView = root.Q<ListView>("DialogList");

        connectButton.RegisterCallback<ClickEvent>(e =>
        {
            Connect(endpointInput.text);
        });
        disconnectButton.RegisterCallback<ClickEvent>(e =>
        {
            Disconnect();
        });
        sendButton.RegisterCallback<ClickEvent>(e =>
        {
            Send(messageInput.text);
        });
        doubleSendButton.RegisterCallback<ClickEvent>(e =>
        {
            Send($"{messageInput.text} -- 1");
            Send($"{messageInput.text} -- 2");
        });
    }

    private void Send(string message)
    {
        Debug.Log($"start sending text {message}");
        m_Client?.SendText(message);
    }
    void Start()
    {
        m_MainThread = System.Threading.Thread.CurrentThread;
        m_DialogListView.itemsSource = m_DialogList;
        m_DialogListView.makeItem = () => m_MessageItemAsset.CloneTree();
        m_DialogListView.bindItem = (element, i) =>
        {
            element.Q<Label>("Message").text = m_DialogList[i];
        };

        EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Disconnect();
            }
        };
    }

    private async void Connect(string endpoint)
    {
        Debug.Log("Start connecting");
        if (m_Client != null)
        {
            Debug.LogError("Already has a client, cannot connect");
            return;
        }
        m_Client = new WebSocketClient(endpoint);
        m_Client.OnOpen += () =>
        {
            Debug.Log($"OnOpen called on {m_Thread}");
            AddToDialogList($"Connected to {endpoint}");
        };
        m_Client.OnClose += (WebSocketCloseStatus closeStatus) =>
        {
            Debug.Log($"OnClose called with closeStatus {closeStatus} on {m_Thread}");
            AddToDialogList($"Disconnected from {endpoint}");
        };
        m_Client.OnMessage += (byte[] message) =>
        {
            Debug.Log($"OnMessage called on {m_Thread}");
            AddToDialogList(System.Text.Encoding.UTF8.GetString(message));
        };
        await m_Client.Connect();
        Debug.Log($"Connected! on {m_Thread}");
    }

    private void AddToDialogList(string text)
    {
        m_DialogList.Add(text);
        m_DialogListView.Refresh();
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
