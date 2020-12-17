/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using System;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Xml.Linq;
using Unity.Mathematics;

public class GenTiles
{
    Tilemap pathingMap;
    Tilemap collisionMap;
    int randomSeed;

    public GenTiles(Tilemap pathing, Tilemap collision, int seed)
    {
        pathingMap = pathing;
        collisionMap = collision;
        randomSeed = seed;
    }

    public bool Generate(
        Dictionary<Vector2Int, string> authoredTiles,
        Dictionary<string, double> authoredWeights)
    {
        if (pathingMap == null || collisionMap == null)
        {
            return false;
        }

        authoredTiles.Add(new Vector2Int(0, 5), "road 0");
        
        authoredWeights.Add("water_a 0", 0.01f);
        authoredWeights.Add("water_b 0", 0.01f);
        authoredWeights.Add("water_c 0", 0.01f);
        authoredWeights.Add("waterside 0", 0.01f);
        authoredWeights.Add("waterside 1", 0.01f);
        authoredWeights.Add("waterside 2", 0.01f);
        authoredWeights.Add("waterside 3", 0.01f);
        authoredWeights.Add("waterturn 0", 0.01f);
        authoredWeights.Add("waterturn 1", 0.01f);
        authoredWeights.Add("waterturn 2", 0.01f);
        authoredWeights.Add("waterturn 3", 0.01f);
        authoredWeights.Add("watercorner 0", 0.01f);
        authoredWeights.Add("watercorner 1", 0.01f);
        authoredWeights.Add("watercorner 2", 0.01f);
        authoredWeights.Add("watercorner 3", 0.01f);

        SimpleTiledModel model = new SimpleTiledModel("data", 20, 12, false);
        bool finished = false;

        int retries = 10;

        while (!finished && --retries > 0)
        {
            finished = model.Run(randomSeed, 300, authoredTiles, authoredWeights);
        }

        if (finished)
        {
            model.Graphics(pathingMap, collisionMap);
            return true;
        }

        return false;
    }

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {

    }
}


[System.Serializable]
public class WFCTile
{
    public Tile tile = null;
    public string name;

    public bool collides = false;
    public int rotation = 0;
    public int reflection = 0;

    public WFCTile(string name)
    {
        this.name = name;
        tile = Resources.Load<Tile>(name);
    }
    public WFCTile(WFCTile source, int rotation, int reflection)
    {
        tile = source.tile;
        name = source.name;
        this.rotation = source.rotation + rotation;
        this.reflection = source.reflection + reflection;
        Debug.Log($"set rotation for {name} to {this.rotation}");
    }
}


static class Stuff
{
    public static int Random(this double[] a, double r)
    {
        double sum = a.Sum();
        for (int j = 0; j < a.Length; j++) a[j] /= sum;

        int i = 0;
        double x = 0;

        while (i < a.Length)
        {
            x += a[i];
            if (r <= x) return i;
            i++;
        }

        return 0;
    }

    public static long ToPower(this int a, int n)
    {
        long product = 1;
        for (int i = 0; i < n; i++) product *= a;
        return product;
    }

    public static T Get<T>(this XElement xelem, string attribute, T defaultT = default)
    {
        XAttribute a = xelem.Attribute(attribute);
        return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
    }

    public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names) => xelement.Elements().Where(e => names.Any(n => n == e.Name));
}


abstract class Model
{
    protected bool[][] wave;

    protected int[][][] propagator;
    int[][][] compatible;
    protected int[] observed;

    (int, int)[] stack;
    int stacksize;

    protected System.Random random;
    protected int FMX, FMY, T;
    protected bool periodic;

    protected double[] weights;
    double[] weightLogWeights;

    int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    protected Model(int width, int height)
    {
        FMX = width;
        FMY = height;
    }


    void Init()
    {
        wave = new bool[FMX * FMY][];
        compatible = new int[wave.Length][][];
        for (int i = 0; i < wave.Length; i++)
        {
            wave[i] = new bool[T];
            compatible[i] = new int[T][];
            for (int t = 0; t < T; t++) compatible[i][t] = new int[4];
        }

        weightLogWeights = new double[T];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < T; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        sumsOfOnes = new int[FMX * FMY];
        sumsOfWeights = new double[FMX * FMY];
        sumsOfWeightLogWeights = new double[FMX * FMY];
        entropies = new double[FMX * FMY];

        stack = new (int, int)[wave.Length * T];
        stacksize = 0;
    }

    void InitAuthored(Dictionary<Vector2Int, int> authoredPreset)
    {
        foreach (Vector2Int key in authoredPreset.Keys)
        {
            int wave_index = key.x + FMX * key.y;
            int value = authoredPreset[key];

            for (int t = 0; t < T; ++t)
            {
                if (t != value)
                {
                    Ban(wave_index, t);
                }
            }

        }
    }

    bool? Observe()
    {
        double min = 1E+3;
        int argmin = -1;

        for (int i = 0; i < wave.Length; i++)
        {
            if (OnBoundary(i % FMX, i / FMX)) continue;

            int amount = sumsOfOnes[i];
            if (amount == 0) return false;

            double entropy = entropies[i];
            if (amount > 1 && entropy <= min)
            {
                double noise = 1E-6 * random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }

        if (argmin == -1)
        {
            observed = new int[FMX * FMY];
            for (int i = 0; i < wave.Length; i++) for (int t = 0; t < T; t++) if (wave[i][t]) { observed[i] = t; break; }
            return true;
        }

        double[] distribution = new double[T];
        for (int t = 0; t < T; t++) distribution[t] = wave[argmin][t] ? weights[t] : 0;
        int r = distribution.Random(random.NextDouble());

        bool[] w = wave[argmin];
        for (int t = 0; t < T; t++) if (w[t] != (t == r)) Ban(argmin, t);

        return null;
    }

    protected void Propagate()
    {
        while (stacksize > 0)
        {
            var e1 = stack[stacksize - 1];
            stacksize--;

            int i1 = e1.Item1;
            int x1 = i1 % FMX, y1 = i1 / FMX;

            for (int d = 0; d < 4; d++)
            {
                int dx = DX[d], dy = DY[d];
                int x2 = x1 + dx, y2 = y1 + dy;
                if (OnBoundary(x2, y2)) continue;

                if (x2 < 0) x2 += FMX;
                else if (x2 >= FMX) x2 -= FMX;
                if (y2 < 0) y2 += FMY;
                else if (y2 >= FMY) y2 -= FMY;

                int i2 = x2 + y2 * FMX;
                int[] p = propagator[d][e1.Item2];
                int[][] compat = compatible[i2];

                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }
        }
    }

    public bool Run(int seed, int limit, Dictionary<Vector2Int, int> authoredPreset)
    {
        if (wave == null) Init();

        Clear();
        random = new System.Random(seed);

        if (authoredPreset != null)
        {
            InitAuthored(authoredPreset);
        }

        for (int l = 0; l < limit || limit == 0; l++)
        {
            bool? result = Observe();
            if (result != null) return (bool)result;
            Propagate();
        }

        return true;
    }

    protected void Ban(int i, int t)
    {
        wave[i][t] = false;

        int[] comp = compatible[i][t];
        for (int d = 0; d < 4; d++) comp[d] = 0;
        stack[stacksize] = (i, t);
        stacksize++;

        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        double sum = sumsOfWeights[i];
        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    protected virtual void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            for (int t = 0; t < T; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
        }
    }

    protected abstract bool OnBoundary(int x, int y);
    // public abstract System.Drawing.Bitmap Graphics();

    protected static int[] DX = { -1, 0, 1, 0 };
    protected static int[] DY = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };
}


class SimpleTiledModel : Model
{
    List<WFCTile> tiles;
    List<string> tilenames;
    public Dictionary<string, int> tileIndices = new Dictionary<string, int>();
    Dictionary<string, int> firstOccurrence = new Dictionary<string, int>();
    int tilesize = 16;
    bool unique = false;

    public SimpleTiledModel(string name, int width, int height, bool periodic) : base(width, height)
    {

        this.periodic = periodic;

        XElement xroot = XDocument.Load($"Assets/Tilemap/{name}.xml").Root;

        tilesize = xroot.Get("size", tilesize);
        unique = xroot.Get("unique", unique);

        tiles = new List<WFCTile>();
        tilenames = new List<string>();
        var tempStationary = new List<double>();

        List<int[]> action = new List<int[]>();


        foreach (XElement xtile in xroot.Element("tiles").Elements("tile"))
        {
            string tilename = xtile.Get<string>("name");
            bool tileCollision = xtile.Get<bool>("collide");

            Func<int, int> a, b;
            int cardinality;

            char sym = xtile.Get("symmetry", 'X');
            if (sym == 'L')
            {
                cardinality = 4;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i + 1 : i - 1;
            } else if (sym == 'T')
            {
                cardinality = 4;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i : 4 - i;
            } else if (sym == 'I')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => i;
            } else if (sym == '\\')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => 1 - i;
            } else if (sym == 'F')
            {
                cardinality = 8;
                a = i => i < 4 ? (i + 1) % 4 : 4 + (i - 1) % 4;
                b = i => i < 4 ? i + 4 : i - 4;
            } else
            {
                cardinality = 1;
                a = i => i;
                b = i => i;
            }

            T = action.Count;
            firstOccurrence.Add(tilename, T);

            int[][] map = new int[cardinality][];
            for (int t = 0; t < cardinality; t++)
            {
                map[t] = new int[8];

                map[t][0] = t;
                map[t][1] = a(t);
                map[t][2] = a(a(t));
                map[t][3] = a(a(a(t)));
                map[t][4] = b(t);
                map[t][5] = b(a(t));
                map[t][6] = b(a(a(t)));
                map[t][7] = b(a(a(a(t))));

                for (int s = 0; s < 8; s++) map[t][s] += T;

                action.Add(map[t]);
            }

            if (unique)
            {
                for (int t = 0; t < cardinality; t++)
                {

                    // UnityEngine.Debug.Log($"adding {tilename} {t} , {tiles.Count} (actions {action.Count})");
                    tileIndices.Add($"{tilename} {t}", tiles.Count);
                    tiles.Add(new WFCTile($"{tilename} {t}"));
                    tiles.Last().collides = tileCollision;
                    tilenames.Add($"{tilename} {t}");
                }
            } else
            {
                tiles.Add(new WFCTile($"{tilename} 0"));
                tiles.Last().collides = tileCollision;
                tilenames.Add($"{tilename} 0");

                for (int t = 1; t < cardinality; t++)
                {
                    if (t <= 3) tiles.Add(new WFCTile(tiles[T + t - 1], 90, 0));
                    if (t >= 4) tiles.Add(new WFCTile(tiles[T + t - 4], 0, 180));
                    tilenames.Add($"{tilename} {t}");
                }
            }

            for (int t = 0; t < cardinality; t++) tempStationary.Add(xtile.Get("weight", 1.0f));
        }

        T = action.Count;
        weights = tempStationary.ToArray();

        propagator = new int[4][][];
        var tempPropagator = new bool[4][][];
        for (int d = 0; d < 4; d++)
        {
            tempPropagator[d] = new bool[T][];
            propagator[d] = new int[T][];
            for (int t = 0; t < T; t++) tempPropagator[d][t] = new bool[T];
        }

        foreach (XElement xneighbor in xroot.Element("neighbors").Elements("neighbor"))
        {
            string[] left = xneighbor.Get<string>("left").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] right = xneighbor.Get<string>("right").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int L = action[firstOccurrence[left[0]]][left.Length == 1 ? 0 : int.Parse(left[1])], D = action[L][1];
            int R = action[firstOccurrence[right[0]]][right.Length == 1 ? 0 : int.Parse(right[1])], U = action[R][1];

            tempPropagator[0][R][L] = true;
            tempPropagator[0][action[R][6]][action[L][6]] = true;
            tempPropagator[0][action[L][4]][action[R][4]] = true;
            tempPropagator[0][action[L][2]][action[R][2]] = true;

            tempPropagator[1][U][D] = true;
            tempPropagator[1][action[D][6]][action[U][6]] = true;
            tempPropagator[1][action[U][4]][action[D][4]] = true;
            tempPropagator[1][action[D][2]][action[U][2]] = true;
        }

        for (int t2 = 0; t2 < T; t2++) for (int t1 = 0; t1 < T; t1++)
            {
                tempPropagator[2][t2][t1] = tempPropagator[0][t1][t2];
                tempPropagator[3][t2][t1] = tempPropagator[1][t1][t2];
            }

        List<int>[][] sparsePropagator = new List<int>[4][];
        for (int d = 0; d < 4; d++)
        {
            sparsePropagator[d] = new List<int>[T];
            for (int t = 0; t < T; t++) sparsePropagator[d][t] = new List<int>();
        }

        for (int d = 0; d < 4; d++) for (int t1 = 0; t1 < T; t1++)
            {
                List<int> sp = sparsePropagator[d][t1];
                bool[] tp = tempPropagator[d][t1];

                for (int t2 = 0; t2 < T; t2++) if (tp[t2]) sp.Add(t2);

                int ST = sp.Count;
                if (ST == 0) Debug.Log($"ERROR: tile {tilenames[t1]} has no neighbors in direction {d}");
                propagator[d][t1] = new int[ST];
                for (int st = 0; st < ST; st++) propagator[d][t1][st] = sp[st];
            }
    }

    protected override bool OnBoundary(int x, int y) => !periodic && (x < 0 || y < 0 || x >= FMX || y >= FMY);

    public void Graphics(Tilemap pathing, Tilemap collision)
    {
        if (observed != null)
        {
            for (int x = 0; x < FMX; x++) for (int y = 0; y < FMY; y++)
                {
                    WFCTile tile = tiles[observed[x + y * FMX]];
                    if (tile.collides)
                    {
                        collision.SetTile(new Vector3Int(x - FMX / 2, -y - 1 + FMY / 2, 0), tile.tile);
                    } else
                    {
                        pathing.SetTile(new Vector3Int(x - FMX / 2, -y - 1 + FMY / 2, 0), tile.tile);
                    }


                    // UnityEngine.Debug.Log($"set tile for {tile.name}");
                    if (tile.rotation != 0 || tile.reflection != 0)
                    {
                        Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, tile.rotation), Vector3.one);
                        if (tile.collides)
                        {
                            collision.SetTransformMatrix(new Vector3Int(x, -y, 0), matrix);
                        } else
                        {
                            pathing.SetTransformMatrix(new Vector3Int(x, -y, 0), matrix);
                        }
                        Debug.Log($"{tile.name} has rotation {tile.rotation} and reflect {tile.reflection}");
                    }
                }
        }
    }

    public string TextOutput()
    {
        var result = new System.Text.StringBuilder();

        for (int y = 0; y < FMY; y++)
        {
            for (int x = 0; x < FMX; x++) result.Append($"{tilenames[observed[x + y * FMX]]}, ");
            result.Append(Environment.NewLine);
        }

        return result.ToString();
    }

    public bool Run(int seed, int limit, Dictionary<Vector2Int, string> authoredTiles, Dictionary<string, double> authoredWeights)
    {
        Dictionary<Vector2Int, int> authoredPresetBase = null;
        Dictionary<string, double> origWeights = new Dictionary<string, double>();

        if (authoredTiles != null)
        {

            authoredPresetBase = new Dictionary<Vector2Int, int>();

            foreach (Vector2Int key in authoredTiles.Keys)
            {
                authoredPresetBase.Add(key, tileIndices[authoredTiles[key]]);
            }
        }

        if (authoredWeights != null)
        {
            foreach (string key in authoredWeights.Keys)
            {
                origWeights.Add(key, weights[tileIndices[key]]);
                weights[tileIndices[key]] = authoredWeights[key];
            }
        }

        bool rc = Run(seed, limit, authoredPresetBase);

        foreach (string key in origWeights.Keys)
        {
            weights[tileIndices[key]] = origWeights[key];
        }

        return rc;
    }
}
