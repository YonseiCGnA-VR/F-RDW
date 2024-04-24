using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using graph;
using PathClass;
public class PathExtractorScript : MonoBehaviour
{
    // Extract Path from current position and give it to MPCRed
    private GameObject body = null;
    public GameObject futurePosition;
    private GameObject graph;
    public float redirectionFreq = 2.0f;
    private float time = 0.0f;
    private float secondtime = 0.0f;
    Vector3 position;
    private float velocity = 1.0f;
    public int Depth = 4;
    // first index = depth of path, second index = number of path in same depth
    private Node<PathArray> pathList = new Node<PathArray>();
    private GameObject redirectedAvater;
    private bool listReady = false;
    private GlobalConfiguration globalConf = null;
    private RedirectionManager redirectionManager;
    public float delayTime = 0;
    // Start is called before the first frame update
    void Start()
    {
        delayTime = Time.deltaTime * 20;
        globalConf = GetComponentInParent<GlobalConfiguration>();
        redirectionManager = GameObject.Find("Redirected Avatar").GetComponent<RedirectionManager>();

        body = GameObject.Find("Body");
        if (body == null)
        {
            Debug.LogError("body null error ...");
        }
        redirectedAvater = GameObject.Find("Redirected Avater");
        time = redirectionFreq - 0.5f;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        // reset need to be fixed
        if (!redirectionManager.inReset)
        {
            this.time += Time.deltaTime;
            this.secondtime += Time.deltaTime;
            if (secondtime >= delayTime + redirectionFreq)
            {

                setListReady(true);
                nodeTraversals(pathList);
                secondtime = time;

            }
            if (time >= redirectionFreq)
            {

                position = body.transform.position;
                Debug.Log("Position : " + position);
                Quaternion direction = body.transform.rotation;
                pathList = new Node<PathArray>();

                StartCoroutine(getPath(position, direction, Depth, pathList));



                time = 0;


            }
        }

    }

    public void setTimeToFreq()
    {
        this.time = redirectionFreq;
    }

    public bool accessPath()
    {
        bool result = false;
        if (listReady)
        {
            listReady = false;
            result = true ;
        }
        return result;
    }
    

    public void setListReady(bool state)
    {
        listReady = state;
    }
    private Vector3[] nodeTraversals(Node<PathArray> node)
    {
        int pathNum = 0;
        Vector3[] pos = new Vector3[3];
        Vector3[] nextPos = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            pos[i] = new Vector3(-1, -1, -1);
        }
        if (node.value != null)
        {
            for (pathNum = 0; pathNum < 3; pathNum++)
            {
                if (node.value.getPath(pathNum).graph == null)
                {
                    break;
                }
                pos[pathNum] = node.value.getPath(pathNum).pos;
            }
        }

        for (int j = 0; j < pathNum; j++)
        {
            if (node.nextNodes[j] == null)
            {
                break;
            }
            nextPos = nodeTraversals(node.nextNodes[j]);
            for (int m = 0; m < 3; m++)
            {
                if (nextPos[m] == new Vector3(-1, -1, -1))
                {
                    break;
                }
                Debug.DrawLine(pos[j], nextPos[m], Color.green, 2);
            }

        }
        return pos;

    }
    public Node<PathArray> getPathList()
    {
        return pathList;
    }

    public IEnumerator getPath(Vector3 position, Quaternion direction, int loopNum, Node<PathArray> list)
    {
        yield return StartCoroutine(getFuturePosition(position, direction, loopNum, list));

    }

    private IEnumerator initiate(PathElement[] newPathEle, GameObject futurePosObj, Vector3 position, Quaternion direction, System.Action<int> retFunc)
    {
        int pathNum = -1;


        futurePosObj.transform.position = Utilities.FlattenedPos3D(position);
        futurePosObj.transform.rotation = direction;
        yield return new WaitForFixedUpdate();

        pathNum = futurePosObj.GetComponent<futurepositionscript>().getPathNum();

        // get mapped position
        for (int i = 0; i < pathNum; i++)
        {
            newPathEle[i].pos = futurePosObj.GetComponent<futurepositionscript>().getPos(i);
            newPathEle[i].type = futurePosObj.GetComponent<futurepositionscript>().getType(i);
            newPathEle[i].prob = futurePosObj.GetComponent<futurepositionscript>().getProb(i);
            newPathEle[i].graph = futurePosObj.GetComponent<futurepositionscript>().getGraph();
        }
        futurePosObj.transform.position = newPathEle[0].pos;
        retFunc(pathNum);
        yield break;
    }

    private IEnumerator getFuturePosition(Vector3 position, Quaternion direction, int loopNum, Node<PathArray> list)
    {
        int pathNum = 0;
        PathArray newPath = new PathArray();
        GameObject futurePosObj = null;
        futurePosObj = Instantiate(futurePosition) as GameObject;
        PathElement[] newPathEle = new PathElement[3];
        for (int i = 0; i < 3; i++)
        {
            newPathEle[i] = new PathElement();
        }


        yield return StartCoroutine(initiate(newPathEle, futurePosObj, position, direction, x => pathNum = x));

        for (int i = 0; i < 3; i++)
        {
            newPathEle[i].direction = direction;
            newPath.setPath(i, newPathEle[i]);
        }
        // link


        list.setValue(newPath);

        if (loopNum <= 0)
        {
            Destroy(futurePosObj);
            yield return null;
        }
        else
        {
            for (int j = 0; j < pathNum; j++)
            {
                Vector3 nextPos = getNextPos(futurePosObj, newPathEle[j].type, newPathEle[j].pos, velocity, newPathEle[j].graph);

                Quaternion nextDir = getNextDir(futurePosObj, newPathEle[j].type);
                list.nextNodes[j] = new Node<PathArray>();

                yield return StartCoroutine(getFuturePosition(nextPos, nextDir, loopNum - 1, list.nextNodes[j]));

                Destroy(futurePosObj);
            }

        }


    }

    private Vector3 getNextPos(GameObject body, GraphObject.Type type, Vector3 pos, float velocity, GameObject graph)
    {
        // process approximately direction to Predict Path
        GraphObject.Type GraphType = graph.GetComponent<GraphScript>().getGraphType();
        switch (GraphType)
        {
            case GraphObject.Type.up_T_maze:
            case GraphObject.Type.down_T_maze:
            case GraphObject.Type.left_T_maze:
            case GraphObject.Type.right_T_maze:
                switch (type)
                {
                    case GraphObject.Type.Straight_X:
                        if (0 <= Mathf.Sin(body.transform.rotation.eulerAngles.y * Mathf.PI / 180))
                        {
                            return new Vector3(graph.transform.position.x + (graph.transform.lossyScale.x / 2) + 0.2f, pos.y, graph.transform.position.z);
                        }
                        else
                        {
                            return new Vector3(graph.transform.position.x - (graph.transform.lossyScale.x / 2) - 0.2f, pos.y, graph.transform.position.z);
                        }


                    case GraphObject.Type.Straight_Z:
  
                        if (0 <= Mathf.Cos(body.transform.rotation.eulerAngles.y * Mathf.PI / 180))
                        {
                            return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z + (graph.transform.lossyScale.z / 2) + 0.2f);
                        }
                        else
                        {
                            return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z - (graph.transform.lossyScale.z / 2) - 0.2f);
                        }

                    case GraphObject.Type.quadrant_4_turn:
                        if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y + 45) * Mathf.PI / 180))
                        {
                            return new Vector3(graph.transform.position.x + (graph.transform.lossyScale.x / 2) + 0.2f, pos.y, graph.transform.position.z);
                        }
                        else
                        {
                            return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z - (graph.transform.lossyScale.z / 2) - 0.2f);
                        }

                    case GraphObject.Type.quadrant_1_turn:
                        if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y - 45) * Mathf.PI / 180))
                        {
                            return new Vector3(graph.transform.position.x + (graph.transform.lossyScale.x / 2) + 0.2f, pos.y, graph.transform.position.z);

                        }
                        else
                        {
                            return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z + (graph.transform.lossyScale.z / 2) + 0.2f);

                        }

                    case GraphObject.Type.quadrant_2_turn:
                        if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y + 45) * Mathf.PI / 180))
                        {
                            return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z + (graph.transform.lossyScale.z / 2) + 0.2f);

                        }
                        else
                        {
                            return new Vector3(graph.transform.position.x - (graph.transform.lossyScale.x / 2) - 0.2f, pos.y, graph.transform.position.z);
                        }

                    case GraphObject.Type.quadrant_3_turn:
                        if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y - 45) * Mathf.PI / 180))
                        {
                            return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z - (graph.transform.lossyScale.z / 2) - 0.2f);

                        }
                        else
                        {
                            return new Vector3(graph.transform.position.x - (graph.transform.lossyScale.x / 2) - 0.2f, pos.y, graph.transform.position.z);
                        }
 
                }
                break;
            case GraphObject.Type.Straight_X:
                if (0 <= Mathf.Sin(body.transform.rotation.eulerAngles.y * Mathf.PI / 180))
                {
                    return new Vector3(pos.x + velocity * redirectionFreq, pos.y, pos.z);
                }
                else
                {
                    return new Vector3(pos.x - velocity * redirectionFreq, pos.y, pos.z);
                }

            case GraphObject.Type.Straight_Z:
                if (0 <= Mathf.Cos(body.transform.rotation.eulerAngles.y * Mathf.PI / 180))
                {
                    return new Vector3(pos.x, pos.y, pos.z + velocity * redirectionFreq);
                }
                else
                {
                    return new Vector3(pos.x, pos.y, pos.z - velocity * redirectionFreq);
                }

            case GraphObject.Type.quadrant_4_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y + 45) * Mathf.PI / 180))
                {
                    return new Vector3(graph.transform.position.x + (graph.transform.lossyScale.x / 2) + 0.2f, pos.y, graph.transform.position.z);
                }
                else
                {
                    return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z - (graph.transform.lossyScale.z / 2) - 0.2f);
                }

            case GraphObject.Type.quadrant_1_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y - 45) * Mathf.PI / 180))
                {
                    return new Vector3(graph.transform.position.x + (graph.transform.lossyScale.x / 2) + 0.2f, pos.y, graph.transform.position.z);

                }
                else
                {
                    return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z + (graph.transform.lossyScale.z / 2) + 0.2f);

                }

            case GraphObject.Type.quadrant_2_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y + 45) * Mathf.PI / 180))
                {
                    return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z + (graph.transform.lossyScale.z / 2) + 0.2f);

                }
                else
                {
                    return new Vector3(graph.transform.position.x - (graph.transform.lossyScale.x / 2) - 0.2f, pos.y, graph.transform.position.z);
                }

            case GraphObject.Type.quadrant_3_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y - 45) * Mathf.PI / 180))
                {
                    return new Vector3(graph.transform.position.x, pos.y, graph.transform.position.z - (graph.transform.lossyScale.z / 2) - 0.2f);

                }
                else
                {
                    return new Vector3(graph.transform.position.x - (graph.transform.lossyScale.x / 2) - 0.2f, pos.y, graph.transform.position.z);
                }


            default:
                Debug.LogError("getNextPos Function Error ...");
                break;

        }
        return new Vector3(-1, -1, -1);
    }

    private Quaternion getNextDir(GameObject body, GraphObject.Type type)
    {
        switch (type)
        {
            case GraphObject.Type.Straight_X:
                if (0 <= Mathf.Sin(body.transform.rotation.eulerAngles.y * Mathf.PI / 180))
                {
                    return Quaternion.Euler(new Vector3(0, 90, 0));
                }
                else
                {
                    return Quaternion.Euler(new Vector3(0, 270, 0));
                }


            case GraphObject.Type.Straight_Z:
                if (0 <= Mathf.Cos(body.transform.rotation.eulerAngles.y * Mathf.PI / 180))
                {
                    return Quaternion.Euler(new Vector3(0, 0, 0));
                }
                else
                {
                    return Quaternion.Euler(new Vector3(0, 180, 0));
                }

            case GraphObject.Type.quadrant_4_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y + 45) * Mathf.PI / 180))
                {
                    return Quaternion.Euler(new Vector3(0, 90, 0));
                }
                else
                {
                    return Quaternion.Euler(new Vector3(0, 180, 0));
                }

            case GraphObject.Type.quadrant_1_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y - 45) * Mathf.PI / 180))
                {
                    return Quaternion.Euler(new Vector3(0, 90, 0));

                }
                else
                {
                    return Quaternion.Euler(new Vector3(0, 0, 0));
                }

            case GraphObject.Type.quadrant_2_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y + 45) * Mathf.PI / 180))
                {
                    return Quaternion.Euler(new Vector3(0, 0, 0));

                }
                else
                {
                    return Quaternion.Euler(new Vector3(0, 270, 0));
                }

            case GraphObject.Type.quadrant_3_turn:
                if (0 <= Mathf.Sin((body.transform.rotation.eulerAngles.y - 45) * Mathf.PI / 180))
                {
                    return Quaternion.Euler(new Vector3(0, 180, 0));

                }
                else
                {
                    return Quaternion.Euler(new Vector3(0, 270, 0));
                }
            default:
                Debug.LogError("getNextPos Function Error ...");
                return Quaternion.Euler(new Vector3(90, 90, 90));


        }
    }

}

namespace PathClass
{

    public class PathElement
    {
        public Vector3 pos;
        public Quaternion direction;
        public double prob;
        public GraphObject.Type type;
        public GameObject graph;


        public PathElement()
        {
            this.type = GraphObject.Type.Null;
            this.direction = Quaternion.Euler(-90, -90, -90);
            this.prob = 0;
            this.pos = new Vector3(-1, -1, -1);
            this.graph = null;

        }

        public PathElement(GraphObject.Type type, double prob, Vector3 pos, GameObject graph, Quaternion direction)
        {
            this.type = type;
            this.prob = prob;
            this.pos = pos;
            this.graph = graph;
            this.direction = direction;

        }
        public void setPathElement(GraphObject.Type type, double prob, Vector3 pos, GameObject graph, Quaternion direction)
        {
            this.type = type;
            this.prob = prob;
            this.pos = pos;
            this.graph = graph;
            this.direction = direction;
        }

        public GameObject getGraphObject()
        {
            return this.graph;
        }

    }

    public class PathArray
    {
        private PathElement[] paths = new PathElement[3];

        public PathArray()
        {
            for (int i = 0; i < 3; i++)
            {
                this.paths[i] = new PathElement();
            }
        }

        public void setPath(int i, PathElement path)
        {
            this.paths[i] = path;
        }

        public PathElement getPath(int i)
        {
            return paths[i];
        }
    }

    public class Node<T>
    {
        // value.getPath(i)'s next path is nextNodes[i]
        public Node<T>[] nextNodes = new Node<T>[3];

        public T value;

        public void setValue(T value)
        {
            this.value = value;
        }


        public void linkNode(int i, Node<T> newNode)
        {
            newNode.nextNodes[i] = nextNodes[i];
            nextNodes[i] = newNode;
        }

        public Node<T> ShallowCopy()
        {
            return (Node<T>)this.MemberwiseClone();
        }
    }
}




