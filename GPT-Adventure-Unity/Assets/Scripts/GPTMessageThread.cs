using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class GPTMessageThread : RunnableThread
{
    protected override void Run()
    {
        ForceDotNet.Force();
        using (RequestSocket client = new RequestSocket())
        {
            client.Connect("tcp://localhost:5555");

            for (int i = 0; i < 1 && Running; i++)
            {
                Debug.Log("Sending Hello");
                client.SendFrame("Hello");
                
                string message = null;
                bool gotMessage = false;
                while (Running)
                {
                    gotMessage = client.TryReceiveFrameString(out message);
                    if (gotMessage) break;
                }

                if (gotMessage) Debug.Log("Received " + message);
            }
        }

        NetMQConfig.Cleanup();
    }
}