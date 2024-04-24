using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : MonoBehaviour
{
    public  GameObject ObjectPrefab;
    public static GameObject GoalObject;
    private static GameObject goal;
    //private GlobalConfiguration globalconfiguration;
    
    // Start is called before the first frame update
    void Awake()
    {
        Random.seed = System.DateTime.Now.Minute;
        GoalObject = ObjectPrefab;
        //generateObject();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void destroyObject()
    {
        Destroy(goal);
    }

    public static void generateObject()
    {
        List<List<Vector2>> vePolygons = getVEPolygons();
        Vector2 position = new Vector2(0, 0);
        for (int i = 0; i < 1000; i++)
        {


            position = GetRandomPositionWithinBounds(-30, 30, 5, 60);
            if (!isInPolygons(vePolygons, position))
            {
                break;
            }
        }

        goal = GameObject.Instantiate(GoalObject);
        goal.transform.position = Utilities.UnFlatten(position);
        goal.tag = "Coin";
    }

    public static void generateObject(Vector2 userPosition)
    {
        List<List<Vector2>> vePolygons = getVEPolygons();
        Vector2 position = new Vector2(0, 0);
        Vector2 objectGlobalPos = new Vector2(0, 0);
        for (int i = 0; i < 1000; i++)
        {
            Vector2 objectPos = new Vector2(0,Random.Range(6.0f, 15.0f));
            float rotAngle = Random.Range(0, 360);
            objectPos = Utilities.RotateVector(objectPos, rotAngle);
            objectGlobalPos = userPosition + objectPos;
            if(objectGlobalPos.x > -25 && objectGlobalPos.x < 25)
            {
                if (objectGlobalPos.y > -25 && objectGlobalPos.y < 25)
                {
                    if (!isInPolygons(vePolygons, position))
                    {
                        break;
                    }
                }
            }
            //position = GetRandomPositionWithinBounds(-, 30, 5, 60);

        }

        goal = GameObject.Instantiate(GoalObject);
        goal.transform.position = Utilities.UnFlatten(objectGlobalPos);
        goal.tag = "Coin";
    }

    public static Vector2 GetRandomPositionWithinBounds(float minX, float maxX, float minZ, float maxZ)
    {
        return new Vector2(SampleUniform(minX, maxX), SampleUniform(minZ, maxZ));
    }

    public static float SampleUniform(float min, float max)
    {
        return Random.Range(min, max);
    }

    public static bool isInPolygons(List<List<Vector2>> vePolygons, Vector2 position)
    {
        // return true if position is in vePolygons
        bool result = false;
        foreach (var p in vePolygons)
        {
            if (vePolygons.Count <= 1)
            {
                return result;
            }
            bool flag = true;
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 x = p[i];
                Vector2 y = p[(i + 1) % p.Count];

                Vector2 pos = position - x;
                Vector2 polygonLine = y - x;

                if(Utilities.CalculateAngle(polygonLine , pos) > 0)
                {
                    // if positive , it means is is out of Polygon
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                // if flag is true, it means flag didn't change becuase position is in Polygons
                result = true;
                break;
            }
        }
        return result;
    }

    public static List<List<Vector2>> getVEPolygons()
    {
        List<List<Vector2>> polygons = new List<List<Vector2>>();
        GameObject mazeSpawner = GameObject.Find("Park3");

        var temp = mazeSpawner.gameObject.GetComponentsInChildren<Transform>();
        int layerMask = 1 << LayerMask.NameToLayer("VirtualWall");


        for (int i = 1; i < temp.Length; i++)
        {
            if (temp[i].gameObject.layer == LayerMask.NameToLayer("VirtualWall"))
            {

                    // add polygon

                    Vector2 objectPosition = Utilities.FlattenedPos2D(temp[i].gameObject.transform.position);
                    Vector2 objectForward = Utilities.FlattenedDir2D(temp[i].gameObject.transform.forward);
                    Vector2 objectRight = Utilities.FlattenedDir2D(temp[i].gameObject.transform.right);

                    float longSideLength = 5.0f;
                    float shortSideLength = 5.0f;

                    Vector2 widthVector = objectRight * longSideLength;
                    Vector2 depthVector = objectForward * shortSideLength;

                    Vector2 quadrant1 = objectPosition + widthVector + depthVector;
                    Vector2 quadrant2 = objectPosition - widthVector + depthVector;
                    Vector2 quadrant3 = objectPosition - widthVector - depthVector;
                    Vector2 quadrant4 = objectPosition + widthVector - depthVector;

                    polygons.Add(new List<Vector2> {
                        new Vector2(quadrant1.x, quadrant1.y),
                        new Vector2(quadrant2.x, quadrant2.y),
                        new Vector2(quadrant3.x, quadrant3.y),
                        new Vector2(quadrant4.x, quadrant4.y),
                    });


            }
        }


        return polygons;
    }
}
