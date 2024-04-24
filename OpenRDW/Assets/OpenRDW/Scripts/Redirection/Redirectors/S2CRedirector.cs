using UnityEngine;
using System.Collections;

public class S2CRedirector : SteerToRedirector {


    // Testing Parameters
    protected bool dontUseTempTargetInS2C = false;
    protected GameObject RedirectionTarget;
    protected GameObject TrackingSpaceCenter;
    protected GameObject VectorToCenter;

    protected const float S2C_BEARING_ANGLE_THRESHOLD_IN_DEGREE = 160;
    protected const float S2C_TEMP_TARGET_DISTANCE = 4;

    public void Start()
    {
        if (RedirectionTarget == null && !redirectionManager.globalConfiguration.runInBackstage)
        {
            RedirectionTarget = Instantiate(redirectionManager.globalConfiguration.RedirectionTarget);
            RedirectionTarget.transform.SetParent(transform);
            RedirectionTarget.transform.position = Vector3.zero;

        }

        if (TrackingSpaceCenter == null && !redirectionManager.globalConfiguration.runInBackstage)
        {
            TrackingSpaceCenter = Instantiate(redirectionManager.globalConfiguration.TrackingSpaceCenter);
            TrackingSpaceCenter.transform.SetParent(transform);
            TrackingSpaceCenter.transform.position = Vector3.zero;

        }

        if (VectorToCenter == null && !redirectionManager.globalConfiguration.runInBackstage)
        {
            VectorToCenter = Instantiate(redirectionManager.globalConfiguration.negArrow);
            VectorToCenter.transform.SetParent(transform);
            VectorToCenter.transform.position = Vector3.zero;
        }
    }

    public void getVectorToCenter(ref Vector3 userToCenter, Vector3 startPosition)
    {
        Vector3 trackingAreaPosition = Utilities.FlattenedPos3D(redirectionManager.trackingSpace.position);
        userToCenter = trackingAreaPosition - startPosition;
    }

    public void setRedirectionTarget(Vector3 vectorToTarget)
    {
        float bearingToTarget = Vector3.Angle(vectorToTarget, redirectionManager.currDir);//unsigned angle
        float directionToTarget = Mathf.Sign(Utilities.GetSignedAngle(redirectionManager.currDir, vectorToTarget));//signed angle
        //Debug.Log(bearingToCenter);
        if (bearingToTarget >= S2C_BEARING_ANGLE_THRESHOLD_IN_DEGREE && !dontUseTempTargetInS2C)
        {
            //Generate temporary target
            if (noTmpTarget)
            {
                tmpTarget = new GameObject("S2C Temp Target");
                tmpTarget.transform.position = redirectionManager.currPos + S2C_TEMP_TARGET_DISTANCE * (Quaternion.Euler(0, directionToTarget * 90, 0) * redirectionManager.currDir);
                RedirectionTarget.transform.position = redirectionManager.currPos + S2C_TEMP_TARGET_DISTANCE * (Quaternion.Euler(0, directionToTarget * 90, 0) * redirectionManager.currDir);
                tmpTarget.transform.parent = transform;
                noTmpTarget = false;
            }
            currentTarget = tmpTarget.transform;
        }
        else
        {
            currentTarget = redirectionManager.trackingSpace;
            RedirectionTarget.transform.position = redirectionManager.trackingSpace.position;
            if (!noTmpTarget)
            {
                Destroy(tmpTarget);
                noTmpTarget = true;
            }
        }
        
    }

    public void visualizeVector(GameObject visualObject, Vector3 userToCenter, Vector3 startPosition)
    {
        TrackingSpaceCenter.transform.position = Utilities.FlattenedPos3D(redirectionManager.trackingSpace.position);
        visualObject.transform.position = Utilities.FlattenedPos3D(startPosition) + userToCenter.magnitude / 2 * userToCenter.normalized;
        visualObject.transform.localScale = new Vector3(1, 1, userToCenter.magnitude);
        visualObject.transform.forward = userToCenter.normalized;
    }



    public override void PickRedirectionTarget()
    {

        Vector3 userToCenter = new Vector3(0, 0, 0);
        getVectorToCenter(ref userToCenter, Utilities.FlattenedPos3D(redirectionManager.currPos));
        visualizeVector(VectorToCenter, userToCenter, Utilities.FlattenedPos3D(redirectionManager.currPos));

        setRedirectionTarget(userToCenter);
        
    }

}
