using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using graph;

public class GraphScript : MonoBehaviour
{
    // Graph Object's rotation must be 0,0,0
    public GraphObject.Type type;
    public GraphObject[] graph = new GraphObject[3];

    private int pathNum = 0;
    private int totalPathNum = 0;
    // Start is called before the first frame update
    void Start()
    {
        switch (type)
        {
            case GraphObject.Type.Straight_X:
            case GraphObject.Type.Straight_Z:
            case GraphObject.Type.quadrant_1_turn:
            case GraphObject.Type.quadrant_2_turn:
            case GraphObject.Type.quadrant_3_turn:
            case GraphObject.Type.quadrant_4_turn:
                graph[0] = new GraphObject(type, 1);
                pathNum = 1;
                totalPathNum = 1;
                break;
            case GraphObject.Type.left_T_maze:
                graph[0] = new GraphObject(GraphObject.Type.quadrant_2_turn, 0.5);
                graph[1] = new GraphObject(GraphObject.Type.quadrant_3_turn, 0.5);
                graph[2] = new GraphObject(GraphObject.Type.Straight_Z, 0.5);
                pathNum = 2;
                totalPathNum = 3;
                break;
            case GraphObject.Type.down_T_maze:
                graph[0] = new GraphObject(GraphObject.Type.quadrant_3_turn, 0.5);
                graph[1] = new GraphObject(GraphObject.Type.quadrant_4_turn, 0.5);
                graph[2] = new GraphObject(GraphObject.Type.Straight_X, 0.5);
                pathNum = 2;
                totalPathNum = 3;
                break;
            case GraphObject.Type.right_T_maze:
                graph[0] = new GraphObject(GraphObject.Type.quadrant_1_turn, 0.5);
                graph[1] = new GraphObject(GraphObject.Type.quadrant_4_turn, 0.5);
                graph[2] = new GraphObject(GraphObject.Type.Straight_Z, 0.5);
                pathNum = 2;
                totalPathNum = 3;
                break;
            case GraphObject.Type.up_T_maze:
                graph[0] = new GraphObject(GraphObject.Type.quadrant_1_turn, 0.5);
                graph[1] = new GraphObject(GraphObject.Type.quadrant_2_turn, 0.5);
                graph[2] = new GraphObject(GraphObject.Type.Straight_X, 0.5);
                pathNum = 2;
                totalPathNum = 3;
                break;
            default:
                Debug.LogError("[" + gameObject.name + "] Graph Initiate Error ...");
                pathNum = 0;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    public int getPathNum()
    {
        return this.pathNum;
    }

    public GraphObject.Type getGraphType()
    {
        return this.type;
    }

    public int getTotalPathNum()
    {
        return this.totalPathNum;
    }

}

namespace graph
{
    public class GraphObject
    {
        public enum Type
        {
            Null = -1,
            Straight_X = 0,
            Straight_Z,
            quadrant_1_turn,
            quadrant_2_turn,
            quadrant_3_turn,
            quadrant_4_turn,
            left_T_maze,
            down_T_maze,
            right_T_maze,
            up_T_maze
        }

        private Type type;
        private double prob;
        private Map mapFunction;

        public GraphObject(Type type, double prob)
        {
            this.type = type;
            this.prob = prob;
            switch (type)
            {
                case Type.Straight_X:
                    mapFunction = new Straight_X_Map();
                    break;
                case Type.Straight_Z:
                    mapFunction = new Straight_Z_Map();
                    break;
                case Type.quadrant_1_turn:
                    mapFunction = new quadrant_1_Map();
                    break;
                case Type.quadrant_2_turn:
                    mapFunction = new quadrant_2_Map();
                    break;
                case Type.quadrant_3_turn:
                    mapFunction = new quadrant_3_Map();
                    break;
                case Type.quadrant_4_turn:
                    mapFunction = new quadrant_4_Map();
                    break;
                default:
                    Debug.Log("Wrong type Error in initiating Graph ...");
                    break;
            }
        }

        public Vector3 mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
        {
            return mapFunction.Mapping(point, graphPoint, graphScale);
        }

        public bool isInGraph(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
        {
            return mapFunction.isInGraph(point, graphPoint, graphScale);
        }


        public Type getType()
        {
            return type;
        }

        public double getProb()
        {
            return this.prob;
        }


        public abstract class Map
        {
            public abstract Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale);

            public virtual bool isInGraph(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {
                double minX = graphPoint.x - (graphScale.x / 2);
                double maxX = graphPoint.x + (graphScale.x / 2);
                double minZ = graphPoint.z - (graphScale.z / 2);
                double maxZ = graphPoint.z + (graphScale.z / 2);
                if (minX <= point.x && point.x <= maxX)
                {
                    if (minZ <= point.z && point.z <= maxZ)
                    {
                        return true;
                    }
                }
                Debug.LogError("Something is wrong in Mapping");
                return false;
            }

            public virtual float GetAngle(Vector3 vectorStartPoint, Vector3 vectorEndPoint)
            {
                Vector2 fromV = new Vector2(vectorStartPoint.x, vectorStartPoint.z);
                Vector2 toV = new Vector2(vectorEndPoint.x, vectorEndPoint.z);
                Vector2 v = toV - fromV;
                //Debug.Log("v" + v);
                Vector2 refer = new Vector2(1, 0);
                float resultAngle = Mathf.Atan2(v.y, v.x);
                //Debug.Log("ResultAngle" + resultAngle);
                return resultAngle;
            }

        }

        public class Straight_X_Map : Map
        {
            public override Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {
                if (isInGraph(point, graphPoint, graphScale))
                {
                    return new Vector3(point.x, point.y, graphPoint.z);
                }
                else
                {

                    return new Vector3(0, 0, 0);
                }
            }
        }

        public class Straight_Z_Map : Map
        {
            public override Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {
                if (isInGraph(point, graphPoint, graphScale))
                {
                    return new Vector3(graphPoint.x, point.y, point.z);
                }
                else
                {

                    return new Vector3(0, 0, 0);
                }
            }
        }



        public class quadrant_1_Map : Map
        {
            // Circle path which Center is in quadrant 1 if we assume that graphpoint is (0,0)
            public override Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {

                if (isInGraph(point, graphPoint, graphScale))
                {
                    Vector3 centerPoint = new Vector3(graphPoint.x + (graphScale.x / 2), graphPoint.y, graphPoint.z + (graphScale.z / 2));
                    float angleRad = GetAngle(centerPoint, point);
                    float cos = Mathf.Cos(angleRad);
                    float sin = Mathf.Sin(angleRad);
                    Vector3 result = new Vector3(centerPoint.x + (graphScale.x / 2) * cos, centerPoint.y, centerPoint.z + (graphScale.z / 2) * sin);
                    return result;

                }
                else
                {

                    return new Vector3(0, 0, 0);
                }
            }
        }

        public class quadrant_2_Map : Map
        {
            public override Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {

                if (isInGraph(point, graphPoint, graphScale))
                {
                    Vector3 centerPoint = new Vector3(graphPoint.x - (graphScale.x / 2), graphPoint.y, graphPoint.z + (graphScale.z / 2));
                    float angleRad = GetAngle(centerPoint, point);
                    float cos = Mathf.Cos(angleRad);
                    float sin = Mathf.Sin(angleRad);
                    Vector3 result = new Vector3(centerPoint.x + (graphScale.x / 2) * cos, centerPoint.y, centerPoint.z + (graphScale.z / 2) * sin);
                    return result;

                }
                else
                {

                    return new Vector3(0, 0, 0);
                }
            }
        }

        public class quadrant_3_Map : Map
        {
            public override Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {

                if (isInGraph(point, graphPoint, graphScale))
                {
                    Vector3 centerPoint = new Vector3(graphPoint.x - (graphScale.x / 2), graphPoint.y, graphPoint.z - (graphScale.z / 2));
                    float angleRad = GetAngle(centerPoint, point);
                    float cos = Mathf.Cos(angleRad);
                    float sin = Mathf.Sin(angleRad);
                    Vector3 result = new Vector3(centerPoint.x + (graphScale.x / 2) * cos, centerPoint.y, centerPoint.z + (graphScale.z / 2) * sin);
                    return result;

                }
                else
                {

                    return new Vector3(0, 0, 0);
                }
            }
        }

        public class quadrant_4_Map : Map
        {
            public override Vector3 Mapping(Vector3 point, Vector3 graphPoint, Vector3 graphScale)
            {

                if (isInGraph(point, graphPoint, graphScale))
                {
                    Vector3 centerPoint = new Vector3(graphPoint.x + (graphScale.x / 2), graphPoint.y, graphPoint.z - (graphScale.z / 2));
                    float angleRad = GetAngle(centerPoint, point);
                    float cos = Mathf.Cos(angleRad);
                    float sin = Mathf.Sin(angleRad);
                    Vector3 result = new Vector3(centerPoint.x + (graphScale.x / 2) * cos, centerPoint.y, centerPoint.z + (graphScale.z / 2) * sin);
                    return result;

                }
                else
                {

                    return new Vector3(0, 0, 0);
                }
            }
        }
    }
}

