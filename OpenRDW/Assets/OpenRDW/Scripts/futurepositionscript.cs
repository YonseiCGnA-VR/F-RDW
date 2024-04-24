using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using graph;
using PathClass;


public class futurepositionscript : MonoBehaviour {
    private GameObject Graph;
    private PathElement[] paths = new PathElement[3];
    private GraphObject.Type graphType;
    private int pathNum = 0;
    private int totalPathNum = 0;
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
    }

    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.CompareTag("Graph"))
        {
            //Debug.Log("Graph name :" + coll.gameObject.name);
            Graph = GameObject.Find(coll.gameObject.name);
            if (!Graph.GetComponent<GraphScript>().graph[0].isInGraph(gameObject.transform.position, coll.gameObject.transform.position, coll.gameObject.transform.lossyScale))
            {
                // if Position is not in Graph
                return;
            }
            for (int j = 0; j < 3; j++)
            {
                paths[j] = new PathElement();
            }
            pathNum = Graph.GetComponent<GraphScript>().getPathNum();

            //Debug.Log("Graph name :" + coll.gameObject.name);
            Graph = GameObject.Find(coll.gameObject.name);
            GraphObject.Type graphType = Graph.GetComponent<GraphScript>().type;

            pathNum = Graph.GetComponent<GraphScript>().getPathNum();
            totalPathNum = Graph.GetComponent<GraphScript>().getTotalPathNum();
            //Debug.Log("futureposition's pathNum :" + pathNum);
            int k = 0;
            for (int i = 0; i < totalPathNum; i++)
            {

                if (Graph.GetComponent<GraphScript>().graph[i].isInGraph(gameObject.transform.position, coll.gameObject.transform.position, coll.gameObject.transform.lossyScale))
                {

                    GraphObject.Type graphComponentType = Graph.GetComponent<GraphScript>().graph[i].getType();
                    if (selectGraph(graphType, graphComponentType, gameObject.transform))
                    {
                        paths[k].pos = Graph.GetComponent<GraphScript>().graph[i].mapping(gameObject.transform.position, coll.gameObject.transform.position, coll.gameObject.transform.lossyScale);
                        paths[k].type = graphComponentType;
                        paths[k].prob = Graph.GetComponent<GraphScript>().graph[i].getProb();
                        paths[k].graph = Graph;
                        k = k + 1;
                    }
                    //Debug.Log("Pos :" + pos[i] + "type :" + type[i] + "prob :" + prob[i]);
                }
            }

        }
    }

    public bool selectGraph(GraphObject.Type type, GraphObject.Type componentType, Transform transform)
    {
        bool result = false;
        // 0 < transform.rotation.eulerAngels.y < 360 , 0 angle  means Z-axis
        switch (type)
        {
            case GraphObject.Type.Straight_X:
            case GraphObject.Type.Straight_Z:
            case GraphObject.Type.quadrant_1_turn:
            case GraphObject.Type.quadrant_2_turn:
            case GraphObject.Type.quadrant_3_turn:
            case GraphObject.Type.quadrant_4_turn:
                result = true;
                break;
            case GraphObject.Type.down_T_maze:
                if (45 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 180)
                {
                    if (componentType == GraphObject.Type.Straight_X) result = true;
                    else if (componentType == GraphObject.Type.quadrant_3_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_4_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else if (180 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 315)
                {
                    if (componentType == GraphObject.Type.Straight_X) result = true;
                    else if (componentType == GraphObject.Type.quadrant_4_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_3_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else
                {
                    if (componentType == GraphObject.Type.quadrant_3_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_4_turn) result = true;
                    else if (componentType == GraphObject.Type.Straight_X) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                break;
            case GraphObject.Type.left_T_maze:
                if (45 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 135)
                {
                    if (componentType == GraphObject.Type.quadrant_2_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_3_turn) result = true;
                    else if (componentType == GraphObject.Type.Straight_Z) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else if (135 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 270)
                {
                    if (componentType == GraphObject.Type.Straight_Z) result = true;
                    else if (componentType == GraphObject.Type.quadrant_2_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_3_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else
                {
                    if (componentType == GraphObject.Type.Straight_Z) result = true;
                    else if (componentType == GraphObject.Type.quadrant_3_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_2_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }

                break;
            case GraphObject.Type.right_T_maze:
                if (90 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 225)
                {
                    if (componentType == GraphObject.Type.quadrant_1_turn) result = true;
                    else if (componentType == GraphObject.Type.Straight_Z) result = true;
                    else if (componentType == GraphObject.Type.quadrant_4_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else if (225 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 315)
                {
                    if (componentType == GraphObject.Type.quadrant_1_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_4_turn) result = true;
                    else if (componentType == GraphObject.Type.Straight_Z) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else
                {
                    if (componentType == GraphObject.Type.Straight_Z) result = true;
                    else if (componentType == GraphObject.Type.quadrant_4_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_1_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                break;
            case GraphObject.Type.up_T_maze:
                if (0 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 135)
                {
                    if (componentType == GraphObject.Type.quadrant_2_turn) result = true;
                    else if (componentType == GraphObject.Type.Straight_X) result = true;
                    else if (componentType == GraphObject.Type.quadrant_1_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else if (135 < transform.rotation.eulerAngles.y && transform.rotation.eulerAngles.y < 225)
                {
                    if (componentType == GraphObject.Type.quadrant_1_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_2_turn) result = true;
                    else if (componentType == GraphObject.Type.Straight_X) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                else
                {
                    if (componentType == GraphObject.Type.Straight_X) result = true;
                    else if (componentType == GraphObject.Type.quadrant_2_turn) result = true;
                    else if (componentType == GraphObject.Type.quadrant_1_turn) result = false;
                    else
                    {
                        Debug.LogError("Something is wrong in selectGraph ...");
                        result = false;
                    }
                }
                break;
            default:
                Debug.LogError("type Default in selectGraph ...");
                result = false;
                break;
        }
        return result;
    }

    public Vector3 getPos(int i)
    {
        //Debug.Log("GetPos value :" + this.pos[i]);
        return paths[i].pos;
    }

    public GraphObject.Type getType(int i) {
        return paths[i].type;
    }

    public double getProb(int i) {
        return paths[i].prob;
    }

    public GameObject getGraph() {
        return Graph;
    }
    public int getPathNum() {
        return this.pathNum;
    }
}





