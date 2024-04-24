using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Predictive_ARC_Redirector : ARC_Redirector
{

    protected List<float> List_FutureActualDistance3Way = new List<float>();
    protected List<float> List_FutureVirtualDistance3Way = new List<float>();

    protected GameObject futureUserTR_R;
    protected GameObject futureUserTR_V;
    //public GameObject futureObject;

    private DataExtractor dataExtractor;
    protected MovementManager simulationManager;

    public Vector3 futurePositionReal;
    public Vector3 futurePositionVirtual;
    public float futureDirectionReal;
   public float futureDirectionVirtual;
    protected float alpha = 0.5f;

    public Vector3 futureDeltaPosition;
    // Start is called before the first frame updated
    void Start()
    {
        initialize();
        dataExtractor = FindObjectOfType<DataExtractor>();
        simulationManager = GetComponent<MovementManager>();
        futureDeltaPosition = new Vector3(0, 0, 0);
        futurePositionReal = new Vector3(0, 0, 0);
        futurePositionVirtual = new Vector3(0, 0, 0);
        futureDirectionReal = 0.0f;
        futureDirectionVirtual = 0;
    }


    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        //Destroy(futureObject);
    }

    public void getFutureCoordinateDirection(Vector3 currPos, Vector3 curDir, Vector3 deltaPosVector, out Vector3 futurePosition, out float futureDirection)
    {
        float currentAngle = Utilities.CalculateAngle(new Vector3(0, 0, 1), curDir);

        var deltaPositionTmp = Utilities.RotateVector(Utilities.FlattenedPos2D(deltaPosVector), currentAngle);

        Vector3 deltaPosition = new Vector3(deltaPositionTmp.x, 0, deltaPositionTmp.y); // real movement in future

        futurePosition = currPos + deltaPosition; // future Position Vector in Real World

        futureDirection = Utilities.CalculateAngle(new Vector3(0, 0, 1), deltaPosition);

        return;
    }
    public override void InjectRedirection()
    {


        Vector2 deltaPosition = redirectionManager.deltaPos;
        float speed = redirectionManager.deltaPos.magnitude / redirectionManager.GetDeltaTime();
        float deltaRotation = redirectionManager.deltaDir; // positive if rotate clockwise
        if (deltaPosition == Vector2.zero && deltaRotation == 0.0f)
        {
            //no Redirection
        }

        // define some variables for redirection

        userPosition = redirectionManager.currPosReal;
        userDirection = redirectionManager.currDir;

        GameObject userTR_R;
        GameObject userTR_V;

        bool predictReady = false;
        float[] futurePos = new float[2];

        float f_dist_qq_sum = 0.0f;
        float translationGainMagnitude = 0.0f;
        float misalignLeft = 0.0f;
        float misalignRight = 0.0f;
        float directionRotation = 0.0f;
        float futureCurvatureGain = 1.0f;
        float futureTranslationGainMagnitude = 1.0f;


        var deltaTime = redirectionManager.GetDeltaTime();
        var maxRotationFromCurvatureGain = CURVATURE_GAIN_CAP_DEGREES_PER_SECOND * deltaTime;
        var maxRotationFromRotationGain = ROTATION_GAIN_CAP_DEGREES_PER_SECOND * deltaTime;

        if (redirectionManager.globalConfiguration.montecarloSimulation && redirectionManager.globalConfiguration.movementController == GlobalConfiguration.MovementController.AutoPilot)
        {

            Vector2 tmp = redirectionManager.globalConfiguration.montecarloScript.getSimulationPositionDenceWaypoint(50);
            Vector3 futurePosition = new Vector3(tmp.x, 0, tmp.y);

            Debug.Log("futurePosition = "+ futurePosition);
            var angle = Utilities.CalculateAngle(new Vector3(0, 0, 1), redirectionManager.currDir);
            futureDeltaPosition = Utilities.RotateVector(futurePosition, -angle);
            predictReady = true;
        }
        else
        {

            predictReady = dataExtractor.getPrediction(ref futurePos);

            futureDeltaPosition = new Vector3(futurePos[0], 0, futurePos[1]);


        }

        if (predictReady )
        {
            futurePositionReal = new Vector3(0, 0, 0);
            futurePositionVirtual = new Vector3(0, 0, 0);
            futureDirectionReal = 0.0f;
            futureDirectionVirtual = 0;
            getFutureCoordinateDirection(redirectionManager.currPosReal, redirectionManager.currDirReal, futureDeltaPosition, out futurePositionReal, out futureDirectionReal);
            getFutureCoordinateDirection(redirectionManager.currPos, redirectionManager.currDir, futureDeltaPosition, out futurePositionVirtual, out futureDirectionVirtual);



            futureUserTR_R = getRealUserTransformObj(futurePositionReal, Quaternion.Euler(0, futureDirectionReal, 0));


            futureUserTR_V = getVirtualUserTransformObj(futurePositionVirtual, Quaternion.Euler(0, futureDirectionVirtual, 0));


            Calc_NWay_Distances(futureUserTR_R.transform, true, 3, out List_FutureActualDistance3Way);
            Calc_NWay_Distances(futureUserTR_V.transform, false, 3, out List_FutureVirtualDistance3Way);

            if (List_FutureActualDistance3Way.Count == 3 && List_FutureVirtualDistance3Way.Count == 3)
            {

                for (int i = 0; i < 3; i++)
                {
                    f_dist_qq_sum += Mathf.Abs(List_FutureActualDistance3Way[i] - List_FutureVirtualDistance3Way[i]);
                }


            }
            else
            {
                Debug.LogError("ERROR : List_Distance3Way");
            }
            if (f_dist_qq_sum == 0.0f)
            {

                return;

            }
            futureTranslationGainMagnitude = Mathf.Clamp(Mathf.Abs(List_FutureActualDistance3Way[0]) / Mathf.Abs(List_FutureVirtualDistance3Way[0]), 1 + redirectionManager.globalConfiguration.MIN_TRANS_GAIN, 1 + redirectionManager.globalConfiguration.MAX_TRANS_GAIN);
            futureTranslationGainMagnitude -= 1;
            Debug.Log("translationGainMagnitude " + translationGainMagnitude);


            Destroy(futureUserTR_V);
            Destroy(futureUserTR_R);
            misalignLeft = List_FutureActualDistance3Way[2] - List_FutureVirtualDistance3Way[2];
            misalignRight = List_FutureActualDistance3Way[1] - List_FutureVirtualDistance3Way[1];
            directionRotation = Mathf.Sign(deltaRotation); // If user is rotating to the left, directionRotation > 0. 그냥 부호임. 왼쪽: 1 or 오른쪽: -1.


            var futureRotationFromCurvatureGain = Mathf.Rad2Deg * (redirectionManager.deltaPos.magnitude / redirectionManager.globalConfiguration.CURVATURE_RADIUS);

            float futureDesiredRotateDirection = 0;
            if (misalignLeft > misalignRight) // If the target is to the left of the user,
            {
                futureDesiredRotateDirection = 1;

                futureCurvatureGain = Mathf.Min(futureRotationFromCurvatureGain * Mathf.Min(1, Mathf.Abs(misalignLeft)), maxRotationFromCurvatureGain);

            }
            else
            {
                futureDesiredRotateDirection = -1;
                futureCurvatureGain = Mathf.Min(futureRotationFromCurvatureGain * Mathf.Min(1, Mathf.Abs(misalignRight)), maxRotationFromCurvatureGain);

            }
            futureCurvatureGain = futureDesiredRotateDirection * futureCurvatureGain;




        }


        userTR_R = getRealUserTransformObj(redirectionManager.currPosReal, Quaternion.LookRotation(redirectionManager.currDirReal));
        userTR_V = getVirtualUserTransformObj(redirectionManager.currPos, Quaternion.LookRotation(redirectionManager.currDir));

        float dist_qq_sum = 0.0f;

        Calc_NWay_Distances(userTR_R.transform, true, 3, out List_ActualDistance3Way);
        Calc_NWay_Distances(userTR_V.transform, false, 3, out List_VirtualDistance3Way);
            
        if (List_ActualDistance3Way.Count == 3 && List_VirtualDistance3Way.Count == 3)
        {
            for (int i = 0; i < 3; i++)
            {
                dist_qq_sum += Mathf.Abs(List_ActualDistance3Way[i] - List_VirtualDistance3Way[i]);
            }


        }
        else
        {
            Debug.LogError("ERROR : List_Distance3Way");
        }
        if (dist_qq_sum == 0.0f)
        {
            Debug.LogWarning("Noredirect");
            return;
            //returnValue.Add(1.0f); //no gain
        }

        Destroy(userTR_R);
        Destroy(userTR_V);
        translationGainMagnitude = Mathf.Clamp(Mathf.Abs(List_ActualDistance3Way[0]) / Mathf.Abs(List_VirtualDistance3Way[0]), 1 + redirectionManager.globalConfiguration.MIN_TRANS_GAIN, 1 + redirectionManager.globalConfiguration.MAX_TRANS_GAIN);
        translationGainMagnitude -= 1;


        misalignLeft = 0;
        misalignRight = 0;
        misalignLeft = List_ActualDistance3Way[2] - List_VirtualDistance3Way[2];
        misalignRight = List_ActualDistance3Way[1] - List_VirtualDistance3Way[1];
        directionRotation = Mathf.Sign(deltaRotation); // If user is rotating to the left, directionRotation > 0. 그냥 부호임. 왼쪽: 1 or 오른쪽: -1.

        float curvatureGain = 0;
        float rotationGain = 0;
        //Debug.Log(misalignLeft + " " + misalignRight);

        var rotationFromCurvatureGain = Mathf.Rad2Deg * (redirectionManager.deltaPos.magnitude / redirectionManager.globalConfiguration.CURVATURE_RADIUS);

        float desiredRotateDirection = 0;
        if (misalignLeft > misalignRight) // If the target is to the left of the user,
        {
            desiredRotateDirection = 1;

            curvatureGain = Mathf.Min(rotationFromCurvatureGain * Mathf.Min(1, Mathf.Abs(misalignLeft)), maxRotationFromCurvatureGain);


        }
        else
        {
            desiredRotateDirection = -1;

            curvatureGain = Mathf.Min(rotationFromCurvatureGain * Mathf.Min(1, Mathf.Abs(misalignRight)), maxRotationFromCurvatureGain);

        }

        curvatureGain = desiredRotateDirection * curvatureGain;


        Vector2 futureDir = Utilities.RotateVector(new Vector2(0, 1), futureDirectionVirtual);
        float futureAngleSign = Mathf.Sign(Utilities.GetSignedAngle(redirectionManager.currDir,new Vector3(futureDir.x, 0, futureDir.y))); // future Direction based on curr dir , positive means clockwise
        float rotationGainDirection = 0;
        if(futureAngleSign * Mathf.Sign(deltaRotation) > 0)
        {
            // rotate toward future dir
            if (dist_qq_sum < f_dist_qq_sum)
            {

                // current state is better than future (bad)

                rotationGain = Mathf.Min(Mathf.Abs(deltaRotation * redirectionManager.globalConfiguration.MAX_ROT_GAIN), maxRotationFromRotationGain);
                rotationGainDirection = Mathf.Sign(deltaRotation);

            }
            else
            {
                // future state is better than current state


                // When user rotate good direction, apply Min rotation gain with opposite direction ( because rotate world function is opposite direction)
                rotationGain = Mathf.Min(Mathf.Abs(deltaRotation * redirectionManager.globalConfiguration.MIN_ROT_GAIN), maxRotationFromRotationGain);
                rotationGainDirection = (-1) * Mathf.Sign(deltaRotation);

            }
        }
        else
        {
            // rotate against future dir
            if (dist_qq_sum < f_dist_qq_sum)
            {

                rotationGain = Mathf.Min(Mathf.Abs(deltaRotation * redirectionManager.globalConfiguration.MIN_ROT_GAIN), maxRotationFromRotationGain);
                rotationGainDirection = (-1) * Mathf.Sign(deltaRotation);

            }
            else
            {

                rotationGain = Mathf.Min(Mathf.Abs(deltaRotation * redirectionManager.globalConfiguration.MAX_ROT_GAIN), maxRotationFromRotationGain);
                rotationGainDirection = Mathf.Sign(deltaRotation);

            }
        }


        if (predictReady)
        {
            Debug.Log("curvatureGain = " + curvatureGain + "futureCurvatureGain = " + futureCurvatureGain);
            curvatureGain = (curvatureGain * alpha) + futureCurvatureGain * ( 1 - alpha) ;

            Debug.Log("After curvatureGain " + curvatureGain);
            translationGainMagnitude = (translationGainMagnitude * alpha) + futureTranslationGainMagnitude *  (1 - alpha);

        }



        // select the largest magnitude
        float rotationMagnitude = 0, curvatureMagnitude = 0;

        bool isCurvatureSelected = true;

        if (speed > MOVEMENT_THRESHOLD)
        {
            curvatureMagnitude = curvatureGain;
        }
        else if (Mathf.Abs(deltaRotation) >= ROTATION_THRESHOLD)
        {
            rotationMagnitude = rotationGain; // 3. An angular rotation rate. 여기에 delta T를 곱해야 Rotation이 됨.


            isCurvatureSelected = false;
        }
        else
        {
            // no Redirection 
            return;
        }



        ////smoothing
        float finalRotation = (1.0f - SMOOTHING_FACTOR) * previousMagnitude + SMOOTHING_FACTOR * Mathf.Abs(rotationMagnitude);
        previousMagnitude = finalRotation;

        // apply final redirection
        if (!isCurvatureSelected)
        {

            InjectRotation(rotationGainDirection * finalRotation);

        }
        else
        {


            InjectCurvature(curvatureGain);

        }
        InjectTranslation(translationGainMagnitude * redirectionManager.deltaPos);


    }


    


}
