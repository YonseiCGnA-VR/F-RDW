using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathClass;
using graph;

public class Action {
    public const int RESETCOST = 500;
    public enum Type {
        Null = -1,
        MINRET = 0,
        noRedirection = 1,
        MAXRET,
        Reset30,
        Reset60,
        Reset90,
        Reset120,
        Reset150,
        Reset180,
        Reset150N,
        Reset120N,
        Reset90N,
        Reset60N,
        Reset30N,
    }

    public Type type;
    public float cost;
    public float angle;
    public Action(Type t, float cost, float angle) {
        this.type = t;
        this.cost = cost;
        this.angle = angle;
    }

    
    
}

public class NoRedirection : Action {
    public NoRedirection() : base(Action.Type.noRedirection, 0, 0) {

    }
}


public class MinRedirection : Action {
    public MinRedirection() : base(Action.Type.MINRET, 1, 0) {

    }
}
public class MaxRedirection : Action {
    public MaxRedirection() : base(Action.Type.MAXRET, 1, 0) {

    }
}


public class Reset30 : Action {
    public Reset30() : base(Action.Type.Reset30, RESETCOST,  30) {

    }
       
}

public class Reset60 : Action {
    public Reset60() : base(Action.Type.Reset60, RESETCOST, 60) {

    }
}

public class Reset90 : Action {
    public Reset90() : base (Action.Type.Reset90, RESETCOST, 90){

    }
}
public class Reset120 : Action {
    public Reset120() : base(Action.Type.Reset120, RESETCOST, 120) {

    }
}
public class Reset150 : Action {
    public Reset150() : base(Action.Type.Reset150, RESETCOST,  150) {

    }
}
public class Reset180: Action {
    public Reset180() : base(Action.Type.Reset180, RESETCOST,  179.999f) {

    }
}
public class Reset30N : Action {
    public Reset30N() : base(Action.Type.Reset30N, RESETCOST, -30) {

    }

}

public class Reset60N : Action {
    public Reset60N() : base(Action.Type.Reset60N, RESETCOST,  - 60) {

    }
}

public class Reset90N : Action {
    public Reset90N() : base(Action.Type.Reset90N, RESETCOST, - 90) {

    }
}
public class Reset120N : Action {
    public Reset120N() : base(Action.Type.Reset120N, RESETCOST, - 120) {

    }
}
public class Reset150N : Action {
    public Reset150N() : base(Action.Type.Reset150N, RESETCOST,  - 150) {

    }
}


public class MPCRed_Redirector : Redirector {
    protected const float MOVEMENT_THRESHOLD = 0.2f;
    protected const float ROTATION_THRESHOLD = 1.5f;
    protected static float toleranceAngleError = 1;
    public float redirectionFreq = 2.0f;
    public float userSpeed = 1.0f;
    [HideInInspector]
    public int Depth;
    [HideInInspector]
    protected MovementManager simulationManager;
    [HideInInspector]
    protected RedirectionManager redirectionManager;
    protected double time = 0;
    protected GameObject pathExtractor;
    protected Node<PathArray> pathList = null;
    protected GameObject body = null;
    protected Action[] actionSet = new Action[14];
    [HideInInspector]
    protected Action currAction = null;
    protected float gain = 0.0f;
    public float alpha = 0.8f;
    protected float rotationFromCurvatureGain;
    protected const float CURVATURE_GAIN_CAP_DEGREES_PER_SECOND = 15;
    protected const float ROTATION_GAIN_CAP_DEGREES_PER_SECOND = 30;
    protected bool resetMode = false;
    protected PathExtractorScript pathExtractorScript;
    protected Vector3 currDirection;
    public float directionSmoothingFactor = 0.8f;
    public float collisionangle;
    public bool pointreset;
    public virtual Node<PathArray> getPathList()
    {
        return pathList;
    }
    // Start is called before the first frame update
    public virtual void Start() {
        actionSet[0] = new MinRedirection();
        actionSet[1] = new MaxRedirection();
        actionSet[2] = new NoRedirection();
        actionSet[3] = new Reset180();
        actionSet[4] = new Reset150();
        actionSet[5] = new Reset150N();
        actionSet[6] = new Reset120();
        actionSet[7] = new Reset120N();
        actionSet[8] = new Reset90();
        actionSet[9] = new Reset90N();
        actionSet[10] = new Reset60N();
        actionSet[11] = new Reset60();
        actionSet[12] = new Reset30();
        actionSet[13] = new Reset30N();
        body = GameObject.Find("Body");
        currAction = new Action(Action.Type.noRedirection, 0, 0);
        pathExtractor = GameObject.Find("Path Extractor");
        pathExtractor.SetActive(true);
        pathExtractorScript = pathExtractor.GetComponent<PathExtractorScript>();

        redirectionFreq = pathExtractor.GetComponent<PathExtractorScript>().redirectionFreq;
        simulationManager = GetComponent<MovementManager>();
        redirectionManager = GetComponent<RedirectionManager>();
        currDirection = redirectionManager.currDirReal;
        if (pathExtractor == null) {
            Debug.Log("pathExtractor is null ...");
        }
        Depth = pathExtractor.GetComponent<PathExtractorScript>().Depth;

    }

    public override void InjectRedirection() {

        float g_c = 0;//curvature
        float g_r = 0;//rotation

        redirectionManager = GetComponent<RedirectionManager>();

        Vector3 deltaPos = redirectionManager.deltaPos;

        var deltaTime = redirectionManager.GetDeltaTime();
        var maxRotationFromCurvatureGain = CURVATURE_GAIN_CAP_DEGREES_PER_SECOND * deltaTime;
        var maxRotationFromRotationGain = ROTATION_GAIN_CAP_DEGREES_PER_SECOND * deltaTime;
        int desiredSteeringDirection = 0;

        //calculate rotation by curvature gain
        var rotationFromCurvatureGain = Mathf.Rad2Deg * (redirectionManager.deltaPos.magnitude / redirectionManager.globalConfiguration.CURVATURE_RADIUS);

        var deltaDir = redirectionManager.deltaDir;
        int rotationDir = 0;
        if(Mathf.Sign(deltaDir) > 0)
        {
            rotationDir = 1;
        }
        else
        {
            rotationDir = -1;
        }
        if (currAction.type == Action.Type.MINRET)
        {//rotate away from negtive gradient
            desiredSteeringDirection = 1; // world axis is opposite so positive value means world rotate against User's rotation
            g_r = desiredSteeringDirection * rotationDir * Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.globalConfiguration.MIN_ROT_GAIN), maxRotationFromRotationGain); // 
        }
        else if(currAction.type == Action.Type.MAXRET)
        {//rotate towards negtive gradient
            desiredSteeringDirection = -1;
            g_r = desiredSteeringDirection * rotationDir * Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.globalConfiguration.MAX_ROT_GAIN), maxRotationFromRotationGain);

        }
        else
        {
            g_r = 0;
            g_c = 0;
        }

        g_c = desiredSteeringDirection * Mathf.Min(rotationFromCurvatureGain, maxRotationFromCurvatureGain);

        if (Mathf.Abs(g_r) > Mathf.Abs(g_c))
        {
            // Rotation Gain
            InjectRotation(g_r);
            g_c = 0;
        }
        else
        {
            // Curvature Gain
            InjectCurvature(g_c);
            g_r = 0;
        }

       

    }




    // Update is called once per frame
    void Update() {



        if (pathExtractor.GetComponent<PathExtractorScript>().accessPath() && !resetMode) {


            
            
            Debug.Log("User Speed (per second)  : " + userSpeed);
            pathList = pathExtractor.GetComponent<PathExtractorScript>().getPathList();
            currAction = new Action(Action.Type.Null, 0, 0);

            currAction = initPlanning(pathList, redirectionManager.currPosReal, redirectionManager.currDirReal, Depth, resetMode);

            Debug.Log("curAction cost : " + currAction.cost);
            Debug.Log("curAction type : " + currAction.type);
        }

    }

    public virtual Action initPlanning(Node<PathArray> node, Vector3 position, Vector3 forward, int loop, bool resetMode)
    {
        currDirection = currDirection * directionSmoothingFactor + forward * (1 - directionSmoothingFactor);
        Vector3 right = Quaternion.AngleAxis(90, Vector3.up) * currDirection;

        Action action = planning(node, position, forward, right, loop, resetMode);

        return action;
    }


    public virtual Action planning(Node<PathArray> node, Vector3 position, Vector3 forward, Vector3 right, int loop, bool resetMode) {
        bool breakPathLoop = false;
        int resetCost = Action.RESETCOST;
        float cost = 0;
        Action nextAction = null;

        Action bestAction = new Action(Action.Type.Null, 500000, 0);
        bool isNextReset = false;
        for (int i = 0; i < actionSet.Length; i++) {
            Action act = actionSet[i];

            if(act == null) {
                continue;
            }
            if (!resetMode)
            {
                // reset has not been progressed
                switch (act.type)
                {
                    case Action.Type.noRedirection:
                    case Action.Type.MINRET:
                    case Action.Type.MAXRET:
                        break; 
                    default:
                        continue;
                }
            }
            else
            {
                switch (act.type)
                {
                    case Action.Type.noRedirection:
                    case Action.Type.MINRET:
                    case Action.Type.MAXRET:
                        continue;
                    default:
                        break;
                }
            }


            cost = 0;
            if (act.cost > bestAction.cost) {
                continue;
            }
            if (loop == 0) {
                // leaf Node
                if(IfCollisionHappens(position, forward)) {
                    bestAction.cost = resetCost;
                }
                else
                {
                    bestAction.cost = cost;
                }


                break;
            }
            for (int pathNum = 0; pathNum < 3; pathNum++) {
                if(node.value.getPath(pathNum).type == GraphObject.Type.Null) {
                    break;
                }
                breakPathLoop = false;
                PathElement pathSeg = node.value.getPath(pathNum);
                Vector3 nextPos = position;
                Vector3 nextForward = forward;
                Vector3 nextRight = right;
                if (pathSeg.type == GraphObject.Type.Straight_X || pathSeg.type == GraphObject.Type.Straight_Z) {
                    // Straight path


                    switch (act.type) {
                    case Action.Type.noRedirection:
                        nextPos = positionUpdate(act, pathSeg.type, position, forward, right, 0, node.value.getPath(pathNum).direction);
                        nextForward = vectorDirectionUpdate(act,pathSeg.type, position, forward, 0, node.value.getPath(pathNum).direction);
                        nextRight = vectorDirectionUpdate(act,pathSeg.type, position, right, 0, node.value.getPath(pathNum).direction);


                        break;
                    case Action.Type.MINRET:
                        nextPos = positionUpdate(act, pathSeg.type, position, forward, right, -redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                        nextForward = vectorDirectionUpdate(act,pathSeg.type, position, forward, -redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
                        nextRight = vectorDirectionUpdate(act,pathSeg.type, position, right, -redirectionManager.globalConfiguration.CURVATURE_RADIUS, node.value.getPath(pathNum).direction);
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
                else {
                    switch (act.type)
                    {
                        case Action.Type.noRedirection:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, 0, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward, 0, node.value.getPath(pathNum).direction);
                            nextRight = vectorDirectionUpdate(act, pathSeg.type, position, right, 0, node.value.getPath(pathNum).direction);


                            break;
                        case Action.Type.MINRET:
                            nextPos = positionUpdate(act, pathSeg.type, position, forward, right, - redirectionManager.globalConfiguration.MIN_ROT_GAIN, node.value.getPath(pathNum).direction);
                            nextForward = vectorDirectionUpdate(act, pathSeg.type, position, forward,- redirectionManager.globalConfiguration.MIN_ROT_GAIN, node.value.getPath(pathNum).direction);
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
                cost = cost + (float)pathSeg.prob * act.cost;
                if (IfCollisionHappens(nextPos, nextForward)) {
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
                    if(node == null)
                    {
                        Debug.Log("node is null ...");
                    }
                    else { 
                        if (node.nextNodes[pathNum].value.getPath(0).type != GraphObject.Type.Null)
                        {

                            // nextNodes are exist
                            nextAction = planning(node.nextNodes[pathNum], nextPos, nextForward, nextRight, loop - 1, isNextReset);

                            if (loop == 1)
                            {
                            }

                            if (nextAction != null)
                            {
                                cost = cost + alpha * (float)pathSeg.prob * nextAction.cost;

                            }
                        }
                    }


                }

                


            }
            // need to calculate cost for all PathSeg

            if (!breakPathLoop) {
                if (cost < bestAction.cost)
                {

                    bestAction.type = act.type;
                    bestAction.cost = cost;
                    bestAction.angle = act.angle;
                }
            }




        }

        return bestAction;
    
    }


        public  Vector3 vectorDirectionUpdate(Action act, GraphObject.Type type, Vector3 position, Vector3 vector, float gain, Quaternion direction) {
        float deltaTime = 0.1f;
        float angle = direction.eulerAngles.y;
        int rotationDir = 0;
        float angleWithGain = 90 * (1 + gain);
  
        
        Vector3 result = new Vector3(0, 0, 0);
        switch (act.type)
        {
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
                // reset action
                result = Quaternion.AngleAxis(act.angle, Vector3.up) * vector;
                return result;
                break;
            default:
                break;
        }
        switch (type)
        {
            case GraphObject.Type.Straight_X:
            case GraphObject.Type.Straight_Z:
                if (-0.001f < gain && gain < 0.001)
                {
                    // Straight ( No Redirection )
                    result = vector;
                }
                else
                {
                    //  Curvature Redirection
                    Vector3 deltaForward = vector;
                    for (float i = 0; i < redirectionFreq; i = i + deltaTime)
                    {

                        deltaForward = Quaternion.AngleAxis(gain * deltaTime, Vector3.up) * deltaForward;
                    }

                    result = deltaForward;
                }
                break;
            case GraphObject.Type.quadrant_4_turn:
                if (0 <= Mathf.Sin((angle + 45) * Mathf.PI / 180))
                {
                    // down to right (right turn)
                    rotationDir = 1;
                }
                else
                {
                    // right to down ( left turn )
                    rotationDir = -1;
                }
                result = Quaternion.AngleAxis(rotationDir * angleWithGain, Vector3.up) * vector;
                break;
            case GraphObject.Type.quadrant_1_turn:
                if (0 <= Mathf.Sin((angle - 45) * Mathf.PI / 180))
                {
                    // up to right ( left turn )
                    rotationDir = -1;
                }
                else
                {
                    // right to up ( right turn )
                    rotationDir = 1;
                }
                result = Quaternion.AngleAxis(rotationDir * angleWithGain, Vector3.up) * vector;
                break;
            case GraphObject.Type.quadrant_2_turn:
                if (0 <= Mathf.Sin((angle + 45) * Mathf.PI / 180))
                {
                    //left to up ( left turn )
                    rotationDir = -1;
                }
                else
                {
                    // up to left ( right turn )
                    rotationDir = 1;
                }
                result = Quaternion.AngleAxis(rotationDir * angleWithGain, Vector3.up) * vector;
                break;
            case GraphObject.Type.quadrant_3_turn:
                if (0 <= Mathf.Sin((angle - 45) * Mathf.PI / 180))
                {
                    //left to down ( right turn )
                    rotationDir = 1;

                }
                else
                {
                    // down to left ( left turn )
                    rotationDir = -1;
                }
                result = Quaternion.AngleAxis(rotationDir * angleWithGain, Vector3.up) * vector;
                break;

            default:
                Debug.Log("position Update Error ...");
                result = vector;
                break;
        }

        
        return result;

        }

    public void isInReset(bool state) {
        resetMode = state;
    }

    public Vector3 getCurve(Vector3 position, Vector3 forward, float deltaTime , float gain)
    {
        Vector3 result;
        Vector3 deltaForward = forward;
        Vector3 deltaPos = position;
        Vector3 prevPos = position;
        for (float i = 0; i < redirectionFreq; i = i + deltaTime)
        {
            prevPos = deltaPos;
            deltaPos = deltaPos + deltaForward * deltaTime;
            Debug.DrawLine(prevPos, deltaPos, Color.red, 2);
            deltaForward = Quaternion.AngleAxis(gain * deltaTime, Vector3.up) * deltaForward;
        }
        result = deltaPos;

        return result;
    }

    public Vector3 positionUpdate(Action act, GraphObject.Type type, Vector3 position, Vector3 forward, Vector3 right, float gain, Quaternion direction) {
        float deltaTime = 0.1f;
        Vector3 result = new Vector3(0, 0, 0);
        Vector3 deltaForward = forward * userSpeed;
        float angle = direction.eulerAngles.y;
        Vector3 deltaRight = forward * userSpeed;

        

        float alpha = 0.2f;


        switch (act.type)
        {
            case Action.Type.MINRET:
            case Action.Type.MAXRET:
            case Action.Type.noRedirection:
                switch (type)
                {

                    case GraphObject.Type.Straight_X:
                    case GraphObject.Type.Straight_Z:
                        if (-0.00001f < gain && gain < 0.00001)
                        {
                            // Straight ( No Redirection )
                            Debug.DrawLine(position, position + (redirectionFreq * deltaForward), Color.red, 2);
                            result = position + (redirectionFreq * deltaForward);
                        }
                        else
                        {

                            result = getCurve(position, deltaForward, deltaTime, gain);
                        }
                        break;
                    case GraphObject.Type.quadrant_4_turn:
                        if (0 <= Mathf.Sin((angle + 45) * Mathf.PI / 180))
                        {
                            // down to right (right turn)

                            result = position + (forward + right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        else
                        {
                            // right to down ( left turn )
                            result = position + (forward - right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        break;
                    case GraphObject.Type.quadrant_1_turn:
                        if (0 <= Mathf.Sin((angle - 45) * Mathf.PI / 180))
                        {
                            // right to up ( left turn )
                            result = position + (forward - right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        else
                        {
                            // up to right ( right turn )
                            result = position + (forward + right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        break;
                    case GraphObject.Type.quadrant_2_turn:
                        if (0 <= Mathf.Sin((angle + 45) * Mathf.PI / 180))
                        {
                            //left to up ( left turn )
                            result = position + (forward - right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        else
                        {
                            // up to left ( right turn )
                            result = position + (forward + right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        break;
                    case GraphObject.Type.quadrant_3_turn:
                        if (0 <= Mathf.Sin((angle - 45) * Mathf.PI / 180))
                        {
                            //left to down ( right turn )
                            result = position + (forward + right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        else
                        {
                            // down to left ( left turn )
                            result = position + (forward - right) * alpha;
                            Debug.DrawLine(position, result, Color.red, 2);
                        }
                        break;
                    default:
                        Debug.Log("position Update Error ...");
                        return position;
                        break;

                }
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
                result = position;
                break;
        }


        return result;
    }

    public bool IfCollisionHappens(Vector3 rPos, Vector3 rDir) {

        Vector2 realPos = new Vector2(rPos.x, rPos.z);
        Vector2 realDir = new Vector2(rDir.x, rDir.z);
        var polygons = new List<List<Vector2>>();
        var trackingSpacePoints = simulationManager.generalManager.trackingSpacePoints;
        var obstaclePolygons = simulationManager.generalManager.obstaclePolygons;
        var userGameobjects = simulationManager.generalManager.redirectedAvatars;


        polygons.Add(trackingSpacePoints);
        foreach (var obstaclePolygon in obstaclePolygons)
            polygons.Add(obstaclePolygon);

        var ifCollisionHappens = false;
        var x = 1;
        foreach (var polygon in polygons) {

            if(x == 1)
            {
                for (int i = 0; i < polygon.Count; i++)
                {
                    var p = polygon[i];
                    var q = polygon[(i + 1) % polygon.Count];

                    if (Utilities.GetSignedAngle(q - p, realPos - p) > 0)
                    {
                        // realPos is out of boundary
                        float angle = Utilities.GetSignedAngle(q - p, realDir);
                        if (Mathf.Sign(angle) > 0)
                        {
                            // angle is positive

                            ifCollisionHappens = true;
                            break;
                        }
                        else if (Mathf.Abs(angle) > 20 && Mathf.Abs(angle) < 160)
                        {
                            // angle is negative and -160 < angle < -20
                            ifCollisionHappens = false;
                        }
                        else
                        {
                            ifCollisionHappens = true;
                        }
                    }
                    else
                    {
                        ifCollisionHappens = false;
                    }
                }
                x++; // next polygon is obastacle polygon
            }
            else
            {
                for (int i = 0; i < polygon.Count; i++)
                {
                    var p = polygon[i];
                    var q = polygon[(i + 1) % polygon.Count];

                    if (Utilities.GetSignedAngle(q - p, realPos - p) > 0)
                    {
                        
                        ifCollisionHappens = false;
                        break;
                    }
                    else
                    {
                        ifCollisionHappens = true;


                    }
                }
            }
            if (ifCollisionHappens)
            {
                break;
            }



        }
        return ifCollisionHappens;
    }

    public bool IfCollideWithPoint(Vector2 realPos, Vector2 realDir, Vector2 obstaclePoint) {
        //judge point, if the avatar will walks into a circle obstacle
        var pointAngle = Vector2.Angle(obstaclePoint - realPos, realDir);
        return (obstaclePoint - realPos).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER && pointAngle < 90 - toleranceAngleError;
    }

    private float Cross(Vector2 a, Vector2 b) {
        return a.x * b.y - a.y * b.x;
    }


}









