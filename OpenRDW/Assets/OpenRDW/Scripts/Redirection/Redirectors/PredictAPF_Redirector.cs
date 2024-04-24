using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredictAPF_Redirector : APF_Redirector
{
    private const float CURVATURE_GAIN_CAP_DEGREES_PER_SECOND = 15;  // degrees per second
    private const float ROTATION_GAIN_CAP_DEGREES_PER_SECOND = 30;  // degrees per second
    private DataExtractor dataExtractor;
    public Vector2 currentForce;
    private float alpha = 1.0f;
    public GameObject futureForcePointer;
    public GameObject currentForcePointer;
    // Start is called before the first frame update
    void Awake()
    {
        dataExtractor = FindObjectOfType<DataExtractor>();
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateTotalForcePointer(Vector2 currentForceT, Vector2 futureForceT, Vector2 futureVec, Vector2 totalForceT)
    {
        //record this new force
        totalForce = totalForceT;
        currentForce = currentForceT;
        var simulationManager = redirectionManager.movementManager;

        if (totalForcePointer == null && !redirectionManager.globalConfiguration.runInBackstage)
        {
            totalForcePointer = Instantiate(redirectionManager.globalConfiguration.totalForceArrow);
            totalForcePointer.transform.SetParent(transform);
            totalForcePointer.transform.position = Vector3.zero;
            foreach (var mr in totalForcePointer.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = redirectionManager.movementManager.ifVisible;
            }

        }
        if (futureForcePointer == null && !redirectionManager.globalConfiguration.runInBackstage)
        {
            futureForcePointer = Instantiate(redirectionManager.globalConfiguration.futureArrow);
            futureForcePointer.transform.SetParent(transform);
            futureForcePointer.transform.position = Vector3.zero;

            foreach (var mr in futureForcePointer.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = redirectionManager.movementManager.ifVisible;
            }

        }
        if (currentForcePointer == null && !redirectionManager.globalConfiguration.runInBackstage)
        {
            currentForcePointer = Instantiate(redirectionManager.globalConfiguration.negArrow);
            currentForcePointer.transform.SetParent(transform);
            currentForcePointer.transform.position = Vector3.zero;

            foreach (var mr in totalForcePointer.GetComponentsInChildren<MeshRenderer>())
            {
                mr.enabled = redirectionManager.movementManager.ifVisible;
            }

        }

        if (currentForcePointer != null)
        {
            currentForcePointer.SetActive(simulationManager.ifVisible);
            currentForcePointer.transform.position = redirectionManager.currPos + new Vector3(0, 0.1f, 0);

            if (currentForceT.magnitude > 0)
                currentForcePointer.transform.forward = transform.rotation * Utilities.UnFlatten(currentForceT);
        }
        if (futureForcePointer != null)
        {
            //futureForcePointer.SetActive(simulationManager.ifVisible);
            Vector2 rotatedVector = Utilities.RotateVector(futureVec, Utilities.CalculateAngle(new Vector3(0, 0, 1), redirectionManager.currDir));
            futureForcePointer.transform.position = redirectionManager.currPos + new Vector3(rotatedVector.x, 0, rotatedVector.y) + new Vector3(0, 0.1f, 0);

            if (futureForceT.magnitude > 0)
                futureForcePointer.transform.forward = transform.rotation * Utilities.UnFlatten(futureForceT);
        }
        if (totalForcePointer != null)
        {
            totalForcePointer.SetActive(simulationManager.ifVisible);
            totalForcePointer.transform.position = redirectionManager.currPos + new Vector3(0, 0.1f, 0);

            if (totalForceT.magnitude > 0)
                totalForcePointer.transform.forward = transform.rotation * Utilities.UnFlatten(totalForceT);
        }
    }

    public override void InjectRedirection()
    {
        float currRf = 0.0f;
        float futureRf = 0.0f;
        Vector2 currNg = new Vector2(0, 0);
        Vector2 futureNg = new Vector2(0, 0);
        var obstaclePolygons = redirectionManager.globalConfiguration.obstaclePolygons;
        var trackingSpacePoints = redirectionManager.globalConfiguration.trackingSpacePoints;
        var currPosReal = Utilities.FlattenedPos2D(redirectionManager.currPosReal);
        Vector2 futurePosition = new Vector2(0, 0);
        float[] futurePos = new float[2];
        GetRepulsiveForceAndNegativeGradient(obstaclePolygons, currPosReal, trackingSpacePoints, out currRf, out currNg);
        var totalNegativeGradient = new Vector2(0, 0);
        bool predictReady = false;
        Vector2 rotatedFuturePos = new Vector2(0, 0);
        float Angle = 0;

        Vector2 boundaryStartPoint = new Vector2(0, 0);
        Vector2 boundaryEndPoint = new Vector2(0, 0);

        if (redirectionManager.globalConfiguration.montecarloSimulation && redirectionManager.globalConfiguration.movementController == GlobalConfiguration.MovementController.AutoPilot)
        {

            Vector2 simulatedPos = redirectionManager.globalConfiguration.montecarloScript.getSimulationPositionDenceWaypoint(50);

            futurePos[0] = simulatedPos.x;
            futurePos[1] = simulatedPos.y;
            predictReady = true;
            futurePosition = new Vector2(futurePos[0], futurePos[1]);
            var angle = Utilities.CalculateAngle(new Vector3(0, 0, 1), redirectionManager.currDir);
            futurePosition = Utilities.RotateVector(futurePosition, -angle);
        }
        else
        {
            predictReady = dataExtractor.getPrediction(ref futurePos);
            futurePosition = new Vector2(futurePos[0], futurePos[1]);

        }



        if (predictReady)
        {
            Angle = Utilities.CalculateAngle(new Vector3(0, 0, 1), redirectionManager.currDirReal);
            rotatedFuturePos = Utilities.RotateVector(futurePosition, Angle);
            Vector2 futurePosVecReal = rotatedFuturePos + currPosReal;

            if (Utilities.isOutOfBoundaries(futurePosVecReal, trackingSpacePoints, obstaclePolygons, ref boundaryStartPoint, ref boundaryEndPoint))
            {
                // futurePosition is out of Boundaries, reset happened on futurePosition
                futureNg = Utilities.RotateVector(futurePosVecReal, 180).normalized;

            }
            else
            {
                GetRepulsiveForceAndNegativeGradient(obstaclePolygons, futurePosVecReal, trackingSpacePoints, out futureRf, out futureNg);
            }

            totalNegativeGradient = ((currNg * (1 - alpha)) + (futureNg * alpha)).normalized;




        }
        else
        {

            totalNegativeGradient = currNg;



        }


        this.UpdateTotalForcePointer(currNg, futureNg, futurePosition, totalNegativeGradient);

        ApplyRedirectionByNegativeGradient(totalNegativeGradient);
    }

    public void GetRepulsiveForceAndNegativeGradient(List<List<Vector2>> obstaclePolygons, Vector2 position, List<Vector2> trackingSpacePoints, out float rf, out Vector2 negativeGradient)
    {
        var nearestPosList = new List<Vector2>();


        //physical borders' contributions
        for (int i = 0; i < trackingSpacePoints.Count; i++)
        {
            var p = trackingSpacePoints[i];
            var q = trackingSpacePoints[(i + 1) % trackingSpacePoints.Count];
            var nearestPos = Utilities.GetNearestPos(position, new List<Vector2> { p, q });
            nearestPosList.Add(nearestPos);
        }

        //obstacle contribution
        foreach (var obstacle in obstaclePolygons)
        {
            var nearestPos = Utilities.GetNearestPos(position, obstacle);
            nearestPosList.Add(nearestPos);
        }

        //consider avatar as point obstacles
        foreach (var user in redirectionManager.globalConfiguration.redirectedAvatars)
        {
            var uId = user.GetComponent<MovementManager>().avatarId;
            //ignore self
            if (uId == redirectionManager.movementManager.avatarId)
                continue;
            var nearestPos = user.GetComponent<RedirectionManager>().currPosReal;
            nearestPosList.Add(Utilities.FlattenedPos2D(nearestPos));
        }



        rf = 0;
        negativeGradient = Vector2.zero;
        foreach (var obPos in nearestPosList)
        {
            rf += 1 / (position - obPos).magnitude;

            //get gradient contributions
            var gDelta = -Mathf.Pow(Mathf.Pow(position.x - obPos.x, 2) + Mathf.Pow(position.y - obPos.y, 2), -3f / 2) * (position - obPos);

            negativeGradient += -gDelta;//negtive gradient
        }
        negativeGradient = negativeGradient.normalized;

    }

    //apply redirection by negtive gradient
    public void ApplyRedirectionByNegativeGradient(Vector2 ng)
    {
        var currDir = Utilities.FlattenedDir2D(redirectionManager.currDirReal);
        var prevDir = Utilities.FlattenedDir2D(redirectionManager.prevDirReal);
        float g_c = 0;//curvature
        float g_r = 0;//rotation
        float g_t = 0;//translation

        //calculate translation
        if (Vector2.Dot(ng, currDir) < 0)
        {
            g_t = -redirectionManager.globalConfiguration.MIN_TRANS_GAIN;
        }


        var deltaTime = redirectionManager.GetDeltaTime();
        var maxRotationFromCurvatureGain = CURVATURE_GAIN_CAP_DEGREES_PER_SECOND * deltaTime;
        var maxRotationFromRotationGain = ROTATION_GAIN_CAP_DEGREES_PER_SECOND * deltaTime;

        var desiredFacingDirection = Utilities.UnFlatten(ng);//vector of negtive gradient in physical space
        int desiredSteeringDirection = (-1) * (int)Mathf.Sign(Utilities.GetSignedAngle(redirectionManager.currDirReal, desiredFacingDirection));

        //calculate rotation by curvature gain
        var rotationFromCurvatureGain = Mathf.Rad2Deg * (redirectionManager.deltaPos.magnitude / redirectionManager.globalConfiguration.CURVATURE_RADIUS);
        g_c = desiredSteeringDirection * Mathf.Min(rotationFromCurvatureGain, maxRotationFromCurvatureGain);

        var deltaDir = redirectionManager.deltaDir;
        if (deltaDir * desiredSteeringDirection < 0)
        {//rotate away from negtive gradient
            g_r = desiredSteeringDirection * Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.globalConfiguration.MIN_ROT_GAIN), maxRotationFromRotationGain);
        }
        else
        {//rotate towards negtive gradient
            g_r = desiredSteeringDirection * Mathf.Min(Mathf.Abs(deltaDir * redirectionManager.globalConfiguration.MAX_ROT_GAIN), maxRotationFromRotationGain);
        }

        // Translation Gain
        InjectTranslation(g_t * redirectionManager.deltaPos);

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


    public override void GetPriority()
    {
        base.GetPriority();
    }
}
