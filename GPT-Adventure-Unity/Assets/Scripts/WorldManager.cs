using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    public Tilemap pathingMap;
    public Tilemap collisionMap;

    public GameObject creaturePrefab;
    public GameObject NPCPrefab;

    public GPTManager gpt;

    GenTiles tileGenerator;

    void onSceneGPT(GPTNetResponse res)
    {

        Dictionary<Vector2Int, string> authoredTiles = new Dictionary<Vector2Int, string>();
        Dictionary<string, double> authoredWeights = new Dictionary<string, double>();

        authoredTiles.Add(new Vector2Int(0, 5), "road 0");

        // authoredWeights.Add("water_a 0", 0.01f);
        // authoredWeights.Add("water_b 0", 0.01f);
        // authoredWeights.Add("water_c 0", 0.01f);
        // authoredWeights.Add("waterside 0", 0.01f);
        // authoredWeights.Add("waterside 1", 0.01f);
        // authoredWeights.Add("waterside 2", 0.01f);
        // authoredWeights.Add("waterside 3", 0.01f);
        // authoredWeights.Add("waterturn 0", 0.01f);
        // authoredWeights.Add("waterturn 1", 0.01f);
        // authoredWeights.Add("waterturn 2", 0.01f);
        // authoredWeights.Add("waterturn 3", 0.01f);
        // authoredWeights.Add("watercorner 0", 0.01f);
        // authoredWeights.Add("watercorner 1", 0.01f);
        // authoredWeights.Add("watercorner 2", 0.01f);
        // authoredWeights.Add("watercorner 3", 0.01f);

        tileGenerator.Generate(authoredTiles, authoredWeights);
    }

    // Start is called before the first frame update
    void Start()
    {
        tileGenerator = new GenTiles(pathingMap, collisionMap, Random.Range(0, 10000000));

        GPTNetRequest startRequest = new GPTNetRequest("start");

        startRequest.prompt = "You are on a quest to defeat the evil dragon of Larion. " +
                "You've heard he lives up at the north of the kingdom. " +
                "You set on the path to defeat him and walk into a dark forest. " +
                "You go into the forest.";
        startRequest.context = "You are a knight living in the kingdom of Larion.";

        gpt.Generate(null, startRequest);

        GPTNetRequest observeRequest = new GPTNetRequest("observe_scene");

        gpt.Generate(onSceneGPT, observeRequest);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
