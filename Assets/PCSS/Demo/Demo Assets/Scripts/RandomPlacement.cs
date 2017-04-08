using System.Collections;
using System.Collections.Generic;
using TMX.Common;
using UnityEngine;

public class RandomPlacement : MonoBehaviour
{
    public Vector2 placementArea;
    public float averageDistance = 5f;
    public NormalCurve weightRange;
    public float relaxMovement;
    public int iterations = 10;

    public Prefab[] prefabs;

    private List<SamplePoint> samplePoints;
    private int width;
    private int height;

    public void CreateGrid ()
    {
        width = Mathf.CeilToInt(placementArea.x / averageDistance);
        height = Mathf.CeilToInt(placementArea.y / averageDistance);
        samplePoints = new List<SamplePoint>(width * height);

        Vector2 offsetX = 1f / (width - 1f) * Vector2.right;
        Vector2 offsetY = 1f / (height - 1f) * Vector2.up;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = offsetX * x + offsetY * y;
                float weight = weightRange.RandomValue();

                SamplePoint newPoint = new SamplePoint(position, weight);

                if(x > 0)
                {
                    newPoint.AddNeighbor(GetSample(x - 1, y));

                    if(x == width - 1)
                    {
                        newPoint.AddNeighbor(new SamplePoint(position + offsetX, weightRange.RandomValue(), true));
                    }
                }
                else
                {
                    newPoint.AddNeighbor(new SamplePoint(position - offsetX, weightRange.RandomValue(), true));
                }

                if (y > 0)
                {
                    newPoint.AddNeighbor(GetSample(x, y - 1));

                    if (x > 0)
                    {
                        if(Random.value > .5f)
                            newPoint.AddNeighbor(GetSample(x - 1, y - 1));
                        else
                            GetSample(x - 1, y).AddNeighbor(GetSample(x, y - 1));
                    }

                    if (y == height - 1)
                    {
                        newPoint.AddNeighbor(new SamplePoint(position + offsetY, weightRange.RandomValue(), true));
                    }
                }
                else
                {
                    newPoint.AddNeighbor(new SamplePoint(position - offsetY, weightRange.RandomValue(), true));
                }

                samplePoints.Add(newPoint);
            }
        }

        //account for the corners
        samplePoints[0].AddNeighbor(new SamplePoint(samplePoints[0].position - offsetX - offsetY, weightRange.RandomValue(), true));
        samplePoints[width - 1].AddNeighbor(new SamplePoint(samplePoints[width - 1].position + offsetX - offsetY, weightRange.RandomValue(), true));

        samplePoints[samplePoints.Count - 1].AddNeighbor(new SamplePoint(samplePoints[samplePoints.Count - 1].position + offsetX + offsetY, weightRange.RandomValue(), true));
        samplePoints[samplePoints.Count - width].AddNeighbor(new SamplePoint(samplePoints[samplePoints.Count - width].position - offsetX + offsetY, weightRange.RandomValue(), true));
    }

    public void RelaxGrid ()
    {
        for (int i = 0; i < iterations; i++)
        {
            for (int sampleIndex = 0; sampleIndex < samplePoints.Count; sampleIndex++)
            {
                samplePoints[sampleIndex].ApplyWeights(relaxMovement);
                if (i == iterations - 1)
                {
                    Debug.DrawRay(GetPosition(samplePoints[sampleIndex]), Vector3.up * 2f, Color.red, .25f);
                }
            }
            samplePoints.Shuffle();
        }
    }

    [ContextMenu("Place Objects")]
    public void PlaceObjects ()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        Transform container = new GameObject("Container").transform;
        container.position = transform.position;

        CreateGrid();
        RelaxGrid();

        int totalCount = 0;
        for (int i = 0; i < prefabs.Length; i++)
        {
            totalCount += prefabs[i].count;
            prefabs[i].calcCount = prefabs[i].count;
        }

        if (totalCount > samplePoints.Count)
        {
            int numberToRemove = totalCount - samplePoints.Count;

            for (int i = 0; i < numberToRemove; i++)
            {
                float rand = Random.value;
                float total = 0f;

                for (int j = 0; j < prefabs.Length; j++)
                {
                    total += prefabs[j].calcCount / (float)totalCount;
                    if(rand <= total)
                    {
                        prefabs[j].calcCount--;
                        break;
                    }
                }
            }
        }
        int gaps = samplePoints.Count - totalCount;

        samplePoints.Shuffle();
        samplePoints.RemoveRange(0, gaps);

        for (int i = 0; i < prefabs.Length; i++)
        {
            for (int j = 0; j < prefabs[i].calcCount; j++)
            {
                Vector3 position = GetPosition(samplePoints[0]);
                samplePoints.RemoveAt(0);

                GameObject obj = Instantiate(prefabs[i].prefab, position, Quaternion.Euler(0f, Random.value * 360f, 0f));
                obj.transform.localScale *= prefabs[i].scaleRange.RandomValue();
                obj.transform.SetParent(container, true);
            }
        }
        container.SetParent(transform, true);
    }

    private SamplePoint GetSample (int x, int y)
    {
        int index = y * width + x;
        return samplePoints[index];
    }

    private Vector3 GetPosition (SamplePoint sample)
    {
        return transform.position + new Vector3((sample.position.x - .5f) * placementArea.x, 0f, (sample.position.y - .5f) * placementArea.y);
    }
}

[System.Serializable]
public class Prefab
{
    public string name;

    public GameObject prefab;
    public int count = 10;
    public NormalCurve scaleRange;

    [HideInInspector]
    public int calcCount = 10;
}

public class SamplePoint
{
    public Vector2 position;
    public float weight;

    public List<SamplePoint> neighbors;
    public bool isEdge;

    public SamplePoint(Vector2 position, float weight, bool isEdge = false)
    {
        this.position = position;
        this.weight = weight;
        this.isEdge = isEdge;

        neighbors = new List<SamplePoint>(8);
    }

    public void AddNeighbor (SamplePoint newNeighbor)
    {
        neighbors.Add(newNeighbor);
        newNeighbor.neighbors.Add(this);
    }

    public void ApplyWeights (float relaxMovement)
    {
        Vector2 dir = Vector2.zero;

        for (int i = 0; i < neighbors.Count; i++)
        {
            dir += (neighbors[i].position - position) * neighbors[i].weight;
        }

        position += dir * relaxMovement;
    }
}
