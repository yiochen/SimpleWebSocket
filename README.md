# SimpleWebSocket

A WebSocket client implementation for Unity3d, supports WebGL.

This package is based on
[NativeWebSocket](https://github.com/endel/NativeWebSocket) with slight
modification of the API. For example, making `Connect` on WebGl wait for the
connection to open before marking the Task as completed.

## Installation

This package can be installed using [Unity Package
Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html) using a git url.

```
https://github.com/yiochen/SimpleWebSocket.git#upm
```

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
        WebSocketClient client = WebSocketClient.Create("ws://echo.websocket.org");

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

        // Send text
        client.SendText("Hello world");

        // send binary
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Hello world");
        client.Send(bytes);

        // Disconnect
        await client.Close();
    }
}
```
