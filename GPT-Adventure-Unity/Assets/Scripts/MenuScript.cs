using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuScript : MonoBehaviour
{
    // Start is called before the first frame update

    public Button startButton;

    void startButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("WorldScene");
    }

    void Start()
    {
        startButton.onClick.AddListener(startButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
