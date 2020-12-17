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
    delegate void genfntype(Dictionary<string, double> weights);

    string[] waterNames =
    {
        "water_a 0",
        "water_b 0",
        "water_c 0",
        "waterside 0",
        "waterside 1",
        "waterside 2",
        "waterside 3",
        "waterturn 0",
        "waterturn 1",
        "waterturn 2",
        "waterturn 3",
        "watercorner 0",
        "watercorner 1",
        "watercorner 2",
        "watercorner 3"
    };

    string[] cliffNames = { 
        "cliff 0",
        "cliff 1",
        "cliff 2",
        "cliff 3",
        "cliffcorner 0",
        "cliffcorner 1",
        "cliffcorner 2",
        "cliffcorner 3",
        "cliffturn 0",
        "cliffturn 1",
        "cliffturn 2",
        "cliffturn 3"
    };


    string[] treesNames = {
        "trees 0"
    };

    string[] grassNames = {
        "grass 0"
    };

    string[] roadNames = {
        "road 0",
        "road 1",
        "road 2",
        "road 3",
        "roadturn 0",
        "roadturn 1",
        "roadturn 2",
        "roadturn 3",
        "grasscorner 0",
        "grasscorner 1",
        "grasscorner 2",
        "grasscorner 3"
    };

    string[] locales = { "forest", "plains" };

    Dictionary<string, genfntype> genFnMap;

    Dictionary<Vector2Int, string> authoredTiles;
    Dictionary<string, double> authoredWeights;

    void onSceneGPT(GPTNetResponse res)
    {


        authoredTiles.Add(new Vector2Int(0, 5), "road 0");


        tileGenerator.Generate(authoredTiles, authoredWeights);
    }

    // Start is called before the first frame update
    void Start()
    {
        authoredTiles = new Dictionary<Vector2Int, string>();
        authoredWeights = new Dictionary<string, double>();
        genFnMap = new Dictionary<string, genfntype>();

        genFnMap.Add("forest", setForestWeights);
        genFnMap.Add("plains", setPlainsWeights);

        string locale = locales[Random.Range(0, locales.Length)];

        if (genFnMap.ContainsKey(locale))
        {
            genFnMap[locale](authoredWeights);
        }

        tileGenerator = new GenTiles(pathingMap, collisionMap, Random.Range(0, 10000000));

        GPTNetRequest startRequest = new GPTNetRequest("start");

        startRequest.prompt = "You are on a quest to defeat the evil dragon of Larion. " +
                "You've heard he lives up at the north of the kingdom. " +
                "You set on the path to defeat him and walk into a " + locale + ". " +
                "You go into the " + locale + ".";
        startRequest.context = "You are a knight living in the kingdom of Larion.";

        gpt.Generate(null, startRequest);

        GPTNetRequest observeRequest = new GPTNetRequest("observe_scene");

        gpt.Generate(onSceneGPT, observeRequest);
        
    }

    void setForestWeights(Dictionary<string, double> authoredWeights)
    {
        foreach (string name in grassNames)
        {
            authoredWeights.Add(name, 0.7);
        }
        foreach (string name in cliffNames)
        {
            authoredWeights.Add(name, 0.1);
        }
        foreach (string name in waterNames)
        {
            authoredWeights.Add(name, 0.2);
        }
        foreach (string name in treesNames)
        {
            authoredWeights.Add(name, 1.0);
        }
        foreach (string name in roadNames)
        {
            authoredWeights.Add(name, 0.1);
        }
    }

    void setPlainsWeights(Dictionary<string, double> authoredWeights)
    {
        foreach (string name in grassNames)
        {
            authoredWeights.Add(name, 1.0);
        }
        foreach (string name in cliffNames) {
            authoredWeights.Add(name, 0.1);
        }
        foreach (string name in waterNames)
        {
            authoredWeights.Add(name, 0.1);
        }
        foreach (string name in treesNames)
        {
            authoredWeights.Add(name, 0.5);
        }
        foreach (string name in roadNames)
        {
            authoredWeights.Add(name, 0.3);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
