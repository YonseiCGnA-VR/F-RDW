using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Predictive_S2C : S2CRedirector
{
    protected DataExtractor dataExtractor;

    public Vector2 futureForceT;//vector calculated by artificial potential fields(total force or negtive gradient), can be used by apf-resetting
    public GameObject futureForcePointer;//visualization of totalForce
    MovementManager simulationManager;


    public void Start()
    {
        dataExtractor = FindObjectOfType<DataExtractor>();


        simulationManager = redirectionManager.movementManager;
        base.Start();

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



    }

    public override void PickRedirectionTarget()
    {
        float[] futurePos = new float[2];
        bool predictReady = false;
        Vector2 futurePosition = new Vector2(0, 0);
        float Angle = 0;
        Vector2 rotatedFuturePos = new Vector2(0, 0);
        var currPosReal = Utilities.FlattenedPos2D(redirectionManager.currPosReal);

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


        Vector2 rotatedVector = Utilities.RotateVector(futurePosition, Utilities.CalculateAngle(new Vector3(0, 0, 1), redirectionManager.currDir));
        Vector3 futurePosVecVirt = redirectionManager.currPos + Utilities.UnFlatten(rotatedVector);

        Vector3 userToCenter = new Vector3 (0, 0, 0);
        getVectorToCenter(ref userToCenter, Utilities.FlattenedPos3D(redirectionManager.currPos));
        TrackingSpaceCenter.transform.position = Utilities.FlattenedPos3D(redirectionManager.trackingSpace.position);
        visualizeVector(VectorToCenter, userToCenter, Utilities.FlattenedPos3D(redirectionManager.currPos));
        Vector3 futureUserToCenter = new Vector3(0, 0, 0);
        getVectorToCenter(ref futureUserToCenter, futurePosVecVirt);

        float futureVectorAngle = Vector2.SignedAngle(futurePosVecVirt, futureUserToCenter);
        Vector3 futureCenterVector = Utilities.UnFlatten(Utilities.RotateVector(Utilities.FlattenedDir2D(redirectionManager.currDir), futureVectorAngle));

        visualizeVector(futureForcePointer, futureUserToCenter, futurePosVecVirt);
        Vector3 totalVectorToCenter = userToCenter.normalized / 2 + futureCenterVector.normalized / 2;


        setRedirectionTarget(totalVectorToCenter);


    }
}
