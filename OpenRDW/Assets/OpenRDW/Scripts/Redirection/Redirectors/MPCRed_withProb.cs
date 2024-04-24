using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathClass;
using graph;
using movement;
using System.Linq;

namespace movement {

    public enum Movement { Null = -1, Left = 0, Front = 1, Right = 2 }

}



public class MPCRed_withProb : MPCRed_Redirector {


    [HideInInspector]


    private DataExtractor dataExtractor;
    private float[] predictProb;
    private const int PREDICTIONAPPLYNUMBER = 1; // how many graphs are affected by prediction
    private int applyCount = PREDICTIONAPPLYNUMBER;
    private int probAppliedLoop = 0;// just Apply probability to immediate Crossing road
    private MontecarloSimulation montecarloSimulation;
    public override Node<PathArray> getPathList()
    {
        return this.pathList;
    }
    //Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        dataExtractor = FindObjectOfType<DataExtractor>();
        predictProb = new float[dataExtractor.predictActionNum];
        if (redirectionManager.globalConfiguration.montecarloSimulation)
        {
            montecarloSimulation = redirectionManager.globalConfiguration.montecarloScript;
        }
        
    }


    // Update is called once per frame
    void Update() {

        applyCount = PREDICTIONAPPLYNUMBER;
        if (pathExtractorScript.accessPath() && !resetMode) {


            pathList = pathExtractorScript.getPathList();
            currAction = null;

            currAction = initPlanning(pathList, redirectionManager.currPosReal, redirectionManager.currDirReal, Depth, false);
            if (currAction == null)
            {
                Debug.LogError("currAction is Null !! ...");
            }

            Debug.Log("curAction cost : " + currAction.cost);
            Debug.Log("curAction type : " + currAction.type);
        }

    }
    public override Action initPlanning(Node<PathArray> node, Vector3 position, Vector3 forward, int loop, bool resetMode)
    {
        bool predictReady = false;
        probAppliedLoop = -1;
        if (redirectionManager.globalConfiguration.montecarloSimulation && redirectionManager.globalConfiguration.movementController == GlobalConfiguration.MovementController.AutoPilot)
        {

            
            predictReady = true;
            Vector3 result = montecarloSimulation.getSimulatedProb(1.0f, 0.9f, 10.0f, 5);
            for(int i = 0; i < 3; i++)
            {
                predictProb[i] = result[i];
            }


        }
        else
        {


            predictReady = dataExtractor.getPrediction(ref predictProb);
        }

        currDirection = currDirection * directionSmoothingFactor + forward * (1 - directionSmoothingFactor);
        Vector3 right = Quaternion.AngleAxis(90, Vector3.up) * currDirection;
        Action action = planning(node, position, forward, right, loop, resetMode, predictProb, predictReady);

        return action;
    }


    public Action planning(Node<PathArray> node, Vector3 position, Vector3 forward, Vector3 right, int loop, bool resetMode, float[] predictProb, bool predictReady) {
        bool breakPathLoop = false;
        int resetCost = Action.RESETCOST;

        float cost = 0;
        Action nextAction = null;

        Action bestAction = new Action(Action.Type.Null, 500000, 0);
        int graphPathNum = node.value.getPath(0).getGraphObject().GetComponent<GraphScript>().getPathNum();
        bool isNextReset = false;

        if (node == null)
        {
            return null;
        }

        for (int i = 0; i < 14; i++) {
            Action act = actionSet[i];

            if (act == null) {
                continue;
            }
            if (!resetMode)
            {
                // reset has not been progressed
                if (!(act.type == Action.Type.noRedirection || act.type == Action.Type.MINRET || act.type == Action.Type.MAXRET))
                {
                    continue;
                }
            }
            else
            {
                if ((act.type == Action.Type.noRedirection || act.type == Action.Type.MINRET || act.type == Action.Type.MAXRET))
                {
                    continue;
                }
            }
            cost = 0;
            if (act.cost > bestAction.cost) {
                break;
            }
            if (loop == 0)
            {
                // leaf Node
                if (IfCollisionHappens(position, forward))
                {
                    bestAction.cost = resetCost;
                }
                else
                {
                    bestAction.cost = cost;
                }


                break;
            }

            for (int pathNum = 0; pathNum < graphPathNum; pathNum++) {

                Movement graphMovementType = Movement.Null;
                if (node.value.getPath(pathNum).type == GraphObject.Type.Null) {
                    break;
                }

                breakPathLoop = false;
                PathElement pathSeg = node.value.getPath(pathNum);
                graphMovementType = getMovement(pathSeg.type, forward, node.value.getPath(pathNum).direction);
                Vector3 nextPos = position;
                Vector3 nextForward = forward;
                Vector3 nextRight = right;
                
                if (pathSeg.type == GraphObject.Type.Straight_X || pathSeg.type == GraphObject.Type.Straight_Z)
                {
                    // Straight path


                    switch (act.type)
                    {
                        case Action.Type.noRedirection:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, 0, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, 0, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, 0, node.value.getPath(pathNum).direction);


                            break;
                        case Action.Type.MINRET:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, -redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, -redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, -redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                            break;
                        case Action.Type.MAXRET:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                            break;
                        case Action.Type.Reset30:
                        case Action.Type.Reset60:
                        case Action.Type.Reset90:
                        case Action.Type.Reset120:
                        case Action.Type.Reset150:
                        case Action.Type.Reset180:
                        case Action.Type.Reset150N:
                        case Action.Type.Reset120N:
                        case Action.Type.Reset90N:
                        case Action.Type.Reset60N:
                        case Action.Type.Reset30N:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, 0, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, 0, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, 0, node.value.getPath(pathNum).direction);
                            break;

                    }
                }
                else
                {
                    switch (act.type)
                    {
                        case Action.Type.noRedirection:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, 0, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, 0, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, 0, node.value.getPath(pathNum).direction);


                            break;
                        case Action.Type.MINRET:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right,- redirectionManager.globalConfiguration.MIN_ROT_GAIN, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, -redirectionManager.globalConfiguration.MIN_ROT_GAIN, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, -redirectionManager.globalConfiguration.MIN_ROT_GAIN, node.value.getPath(pathNum).direction);
                            break;
                        case Action.Type.MAXRET:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, -redirectionManager.globalConfiguration.MAX_ROT_GAIN, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, -redirectionManager.globalConfiguration.MAX_ROT_GAIN, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, -redirectionManager.globalConfiguration.MAX_ROT_GAIN, node.value.getPath(pathNum).direction);
                            break;
                        case Action.Type.Reset30:
                        case Action.Type.Reset60:
                        case Action.Type.Reset90:
                        case Action.Type.Reset120:
                        case Action.Type.Reset150:
                        case Action.Type.Reset180:
                        case Action.Type.Reset150N:
                        case Action.Type.Reset120N:
                        case Action.Type.Reset90N:
                        case Action.Type.Reset60N:
                        case Action.Type.Reset30N:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, 0, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, 0, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, 0, node.value.getPath(pathNum).direction);
                            break;

                    }

                }
                // use predicted prob to Path Segmentation's prob
                // calculate Probability of Path Segmentation
                float pathProb = 0;
                if(graphPathNum > 1)
                {
                    if(probAppliedLoop == -1)
                    {
                        probAppliedLoop = loop;
                    }

                    if (predictReady && loop == probAppliedLoop)
                    {

                        float maxValue = predictProb.Max();
                        int maxIndex = predictProb.ToList().IndexOf(maxValue);
                        float nextMoveProb = predictProb[maxIndex];
                        bool predictUsable = isPredictUsable(maxIndex, node, graphPathNum, forward, node.value.getPath(pathNum).direction);

                        if (predictUsable)
                        {
                            if(maxIndex == (int)graphMovementType)
                            {
                                pathProb = 1;

                            }
                            else
                            {
                                pathProb = 0;

                            }
                        }
                        else
                        {
                            pathProb = (float)pathSeg.prob;
                        }

                    }
                    else
                    {
                        // prediction is not ready
                        pathProb = (float)pathSeg.prob;
                    }
                }
                else
                {
                    // when Path is single path
                    pathProb = (float)pathSeg.prob;
                }
                cost = cost + pathProb * act.cost;
                if (IfCollisionHappens(nextPos, nextForward))
                {
                    // resets
                    isNextReset = true;
                }
                if (cost > bestAction.cost)
                {
                    break;
                }
                if (loop > 0)
                {
                    // next PathSeg cost
                    if (node.nextNodes[pathNum].value.getPath(0).type != GraphObject.Type.Null)
                    {
                        // nextNodes are exist
                        nextAction = planning(node.nextNodes[pathNum], nextPos, nextForward, nextRight, loop - 1, isNextReset, predictProb, predictReady);

                        if (loop == 1)
                        {

                        }

                        if (nextAction != null)
                        {
                            cost = cost + alpha * pathProb * nextAction.cost;

                        }
                    }


                }





            }
            if (!breakPathLoop) {
                if (cost < bestAction.cost) {
                    bestAction.type = act.type;
                    bestAction.cost = cost;
                    bestAction.angle = act.angle;
                }
            }




        }
        predictReady = false;
        return bestAction;
    }

    public bool isPredictUsable(int maxValue, Node<PathArray> node, int pathNum, Vector3 forward, Quaternion direction)
    {
        bool result = false;
        Movement move = Movement.Null;
        for(int i = 0; i < pathNum; i++)
        {

            move = getMovement(node.value.getPath(i).type, forward, direction);
            if(maxValue == (int)move)
            {
                result = true;
            }
        }


        return result;
    }

    

    public Movement getMovement(GraphObject.Type type, Vector3 forward, Quaternion direction) {
        Movement move;
        float angle = direction.eulerAngles.y;
        switch (type) {

            case GraphObject.Type.Straight_X:
            case GraphObject.Type.Straight_Z:
                move = Movement.Front;
                break;
            case GraphObject.Type.quadrant_4_turn:
                if (0 <= Mathf.Sin((angle + 45) * Mathf.PI / 180)) {
                    // down to right (right turn)
                    move = Movement.Right;
                }
                else {
                    // right to down ( left turn )
                    move = Movement.Left;
                }
                break;
            case GraphObject.Type.quadrant_1_turn:
                if (0 <= Mathf.Sin((angle - 45) * Mathf.PI / 180)) {
                    // up to right ( left turn )
                    move = Movement.Left;
                }
                else {

                    // right to up ( right turn )
                    move = Movement.Right;
                }
                break;
            case GraphObject.Type.quadrant_2_turn:
                if (0 <= Mathf.Sin((angle + 45) * Mathf.PI / 180)) {
                    //left to up ( left turn )
                    move = Movement.Left;
                }
                else {
                    // up to left ( right turn )
                    move = Movement.Right;
                }
                break;
            case GraphObject.Type.quadrant_3_turn:
                if (0 <= Mathf.Sin((angle - 45) * Mathf.PI / 180)) {
                    //left to down ( right turn )
                    move = Movement.Right;

                }
                else {
                    // down to left ( left turn )
                    move = Movement.Left;
                }
                break;

            default:
                move = Movement.Null;
                Debug.Log("getMovement Error ... " + type);
                break;

        }
        return move;
    }

   


}










