using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    public Tilemap pathingMap;
    public Tilemap collisionMap;

    public GPTManager gpt;

    GenTiles tileGenerator;

    void onSceneGPT(GPTNetResponse res)
    {

        Dictionary<Vector2Int, string> authoredTiles = new Dictionary<Vector2Int, string>();
        Dictionary<string, double> authoredWeights = new Dictionary<string, double>();


        tileGenerator.Generate(authoredTiles, authoredWeights);
    }

    // Start is called before the first frame update
    void Start()
    {
        gpt.Generate(onSceneGPT, "start", "You wake up in an open area and look around.", "");
        tileGenerator = new GenTiles(pathingMap, collisionMap, Random.Range(0, 10000000));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
