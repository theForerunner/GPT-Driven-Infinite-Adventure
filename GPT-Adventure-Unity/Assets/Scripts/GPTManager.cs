using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GPTManager : MonoBehaviour
{

    public delegate void action(GPTNetResponse response);
    private action onResponse;
    public GameObject GPTObject;

    List<GPTNetRequest> requestQueue;

    void generateNext()
    {
        if (requestQueue.Count > 0)
        {
            StartCoroutine(PostRequest("http://127.0.0.1:8800", JsonUtility.ToJson(requestQueue[0]), onResponse));
        }
    }

    IEnumerator PostRequest(string url, string json, action responseCallback)
    {
        Debug.Log("url: " + url);
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        GPTNetResponse resData = new GPTNetResponse();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        } else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);

            JsonUtility.FromJsonOverwrite(uwr.downloadHandler.text, resData);

            if (responseCallback != null)
            {
                responseCallback(resData);
            }
        }

        requestQueue.RemoveAt(0);
        generateNext();
    }

    public void Generate(action callback, GPTNetRequest reqData)
    {
        onResponse = callback;

        requestQueue.Add(reqData);
        Debug.Log("Sending data");
        if (requestQueue.Count == 1)
        {
            StartCoroutine(PostRequest("http://127.0.0.1:8800", JsonUtility.ToJson(reqData), onResponse));
        }

    }

    void Start()
    {
        requestQueue = new List<GPTNetRequest>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}



[System.Serializable]
public class GPTNetRequest
{
    public string action;
    public string prompt;
    public string context;

    public GPTNetRequest(string action) { this.action = action; }
    public GPTNetRequest() { }
}

[System.Serializable]
public class GPTNetResponse
{
    public string error;
    public string textResponse;
    public string key1;
    public float val1;
    public string key2;
    public float val2;
    public string key3;
    public float val3;
}