using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class GPTMessageThread : RunnableThread
{

    public delegate void action(string response);

    private action promptCallback;

    private GPTNetData data;

    public GPTMessageThread(action callback, string message, string context)
    {
        data = new GPTNetData();
        data.prompt = message;
        data.context = context;
        promptCallback = callback;
    }

    protected override void Run()
    {
        ForceDotNet.Force();
        using (RequestSocket client = new RequestSocket())
        {
            client.Connect("tcp://localhost:5555");

            for (int i = 0; i < 1 && Running; i++)
            {
                Debug.Log("Sending data");
                client.SendFrame(JsonUtility.ToJson(data));
                
                string message = null;
                bool gotMessage = false;
                while (Running)
                {
                    gotMessage = client.TryReceiveFrameString(out message);
                    if (gotMessage) break;
                }

                if (gotMessage)
                {
                    Debug.Log("Received " + message);
                    promptCallback(message);
                }
            }
        }

        NetMQConfig.Cleanup();
    }
}

[System.Serializable]
public class GPTNetData
{
    public string prompt;
    public string context;
}