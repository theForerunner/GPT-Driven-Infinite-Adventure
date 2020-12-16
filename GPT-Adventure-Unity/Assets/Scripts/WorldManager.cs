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

    // Start is called before the first frame update
    void Start()
    {
        tileGenerator = new GenTiles(pathingMap, collisionMap);

        Dictionary<Vector2Int, string> authoredTiles = new Dictionary<Vector2Int, string>();
        Dictionary<string, double> authoredWeights = new Dictionary<string, double>();


        tileGenerator.Generate(authoredTiles, authoredWeights);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
