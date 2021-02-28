# SimpleWebSocket

A WebSocket client implementation for Unity3d.

## Usage

```c#
using System.Collections;
using UnityEngine;
using System.Net.WebSockets;
using SimpleWebSocket;

public class MyScript : MonoBehavior
{
    async void Start()
    {
        // Create a webSocket client
        WebSocketClient client = new WebSocketClient("ws://echo.websocket.org");

        client.OnOpen += () => {
            Debug.Log("Connection open!");
        };

        client.OnMessage += (byte[] message) => {
            Debug.Log($"Received text {System.Text.Encoding.UTF8.GetString(message)}");
        };

        client.OnClose += (WebSocketCloseStatus closeStatus) => {
            Debug.Log($"Connection closed due to {closeStatus}");
        }

        // Connect to the server
        await client.Connect();

        // Disconnect
        await client.Close();
    }
}
```
