using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;
using UnityEngine.Events;
using System.Threading.Tasks;

public class WebsocketClientReceiver : MonoBehaviour
{
    public string m_websocketServer = "ws://localhost:7070";
    WebSocket websocket;

    public string m_lastReceived;



    public BytesEvent m_onReceivedAsBytes;
    public bool m_useUTF8 = false;
    public StringEvent m_onReceivedMessageUTF8;
    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }
    [System.Serializable]
    public class BytesEvent : UnityEvent<byte[]> { }

    public bool m_isConnected;
    public bool m_autoConnectAtStart = true;
    public bool m_autoReconnect=true;
    public float m_autoReconnectCooldown=1;
    public UnityEvent m_connectionLost;


    public void AutoReconnect()
    {
        if (m_isConnected) return;
        Task.Run(CreateConnection);

    }
     void Start()
    {
        InvokeRepeating("AutoReconnect", 0, m_autoReconnectCooldown);
    }

    private async Task CreateConnection()
    {
        websocket = new WebSocket(m_websocketServer);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!" + m_websocketServer);
            m_isConnected = true;
        };

        websocket.OnError += (e) =>
        {
            Debug.Log($"Error on {m_websocketServer}! " + e);
            m_isConnected = false;
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed! "+ m_websocketServer);
            m_isConnected = false;
            m_connectionLost.Invoke();
        };

        websocket.OnMessage += (bytes) =>
        {
            try
            {
                m_byteQueue.Enqueue(bytes);
                //getting the message as a string
                if (m_useUTF8) { 
                    var message = System.Text.Encoding.UTF8.GetString(bytes);
                    m_lastReceived = message;
                    m_stringQueue.Enqueue(message);
                }
            }
            catch (Exception e) { Debug.Log("Exception happened:" + e.StackTrace); }
        };
        await websocket.Connect();
    }

    public Queue<byte[]> m_byteQueue = new Queue<byte[]>();
    public Queue<string> m_stringQueue = new Queue<string>();
    void Update()
    {
        while (m_byteQueue.Count > 0)
            m_onReceivedAsBytes.Invoke(m_byteQueue.Dequeue());
        while (m_stringQueue.Count > 0)
            m_onReceivedMessageUTF8.Invoke(m_stringQueue.Dequeue());

#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket!=null)
        websocket.DispatchMessageQueue();
#endif
    }

  
    private async void OnApplicationQuit()
    {
        await websocket.Close();
        m_isConnected = false;
    }

}