using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathClass;

public class MPCRedResetter : Resetter
{
    // Start is called before the first frame update
    float overallInjectedRotation;

    float requiredRotateAngle = 0;
    float requiredRotateSteerAngle = 360;
    int resetCount;
    float speedRatio;
    bool setAngle = false;
    bool turnLeft = true; 
    Node<PathArray> pathList;
    PathExtractorScript pathExtractorScript;
    private DataExtractor dataExtractor;
    private float[] predictProb;


    public override bool IsResetRequired()
    {
        return IfCollisionHappens();
    }
    public void Start()
    {
        dataExtractor = FindObjectOfType<DataExtractor>();
        predictProb = new float[dataExtractor.predictActionNum];
        pathExtractorScript = GameObject.Find("Path Extractor").GetComponent<PathExtractorScript>();
        resetCount = 0;
    }

    public override void InitializeReset()
    {

        var redirectorTmp = redirectionManager.redirector;

        if (redirectorTmp.GetType() == (typeof(MPCRed_Redirector)) || redirectorTmp.GetType().IsSubclassOf(typeof(MPCRed_Redirector)))
        {
            Action act;
            if (redirectorTmp.GetType() == (typeof(MPCRed_Redirector)))
            {
                redirectorTmp.GetComponent<MPCRed_Redirector>().isInReset(true);
                act = redirectorTmp.GetComponent<MPCRed_Redirector>().initPlanning(pathExtractorScript.getPathList(), redirectionManager.currPosReal, redirectionManager.currDirReal, redirectorTmp.GetComponent<MPCRed_Redirector>().Depth, true);
                redirectorTmp.GetComponent<MPCRed_Redirector>().isInReset(false);
            }
            else
            {

                redirectorTmp.GetComponent<MPCRed_withProb>().isInReset(true);
                act = redirectorTmp.GetComponent<MPCRed_withProb>().initPlanning(pathExtractorScript.getPathList(), redirectionManager.currPosReal, redirectionManager.currDirReal, redirectorTmp.GetComponent<MPCRed_withProb>().Depth, true);
                redirectorTmp.GetComponent<MPCRed_withProb>().isInReset(false);
            }


            Debug.Log("cur Reset act : " + act.type);
            var targetRealRotation = getResetAngle(act);
            if (targetRealRotation > 0)
            {
                //right Reset
                requiredRotateAngle = 360.0f - targetRealRotation;
                turnLeft = true;
                SetHUD(-1);
            }
            else
            {
                requiredRotateAngle = 360.0f + targetRealRotation;
                turnLeft = false;
                SetHUD(1);
            }
            requiredRotateSteerAngle = 360 - requiredRotateAngle;


            speedRatio = requiredRotateSteerAngle / requiredRotateAngle;
        }
        else
        {
            Debug.Log("RedirectorType: " + redirectorTmp.GetType());
            Debug.LogError("non-MPCRed redirector can't use MPCRed_resetter");
        }
        redirectionManager.inReset = true;



    }

    public float getResetAngle(Action act)
    {
        float result = 0.0f;
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
                result = act.angle;
                break;
            default:
                result = 180;
                break;

        }
        return result;
    }
    public override void InjectResetting()
    {

        var steerRotation = speedRatio * redirectionManager.deltaDir;
        if (Mathf.Abs(requiredRotateSteerAngle) <= Mathf.Abs(steerRotation) || requiredRotateAngle == 0)
        {//meet the rotation requirement
            if (!turnLeft)
            {
                // user right turn
                transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, steerRotation);
                GetComponentInChildren<KeyboardController>().SetLastRotation(-steerRotation);
            }
            else
            {
                // user left turn
                InjectRotation(requiredRotateSteerAngle);
            }


            //reset end
            redirectionManager.OnResetEnd();
            requiredRotateSteerAngle = 0;

        }
        else
        {//rotate the rotation calculated by ratio
            if (!turnLeft)
            {
                // user right turn
                //InjectRotation(-steerRotation);
                transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, steerRotation);
                GetComponentInChildren<KeyboardController>().SetLastRotation(-steerRotation);
            }
            else
            {
                // user left turn
                InjectRotation(steerRotation);

            }
            requiredRotateSteerAngle -= Mathf.Abs(steerRotation);
        }


       
    }

    public override void EndReset()
    {
        resetCount = resetCount + 1;
        Debug.Log("Reset Count : " + resetCount);
        DestroyHUD();
    }
    public override void SimulatedWalkerUpdate()
    {
        // Act is if there's some dummy target a meter away from you requiring you to rotate        
        var rotateAngle = redirectionManager.GetDeltaTime() * redirectionManager.globalConfiguration.rotationSpeed;


        if (rotateAngle >= requiredRotateAngle)
        {
            //finish rotating
            rotateAngle = requiredRotateAngle;
            requiredRotateAngle = 0;
        }
        else
        {
            requiredRotateAngle -= rotateAngle;
        }
        if (!turnLeft)
        {
            redirectionManager.simulatedWalker.RotateInPlace(rotateAngle);
        }
        else
        {
            redirectionManager.simulatedWalker.RotateInPlace(-rotateAngle);
        }

    }
    // Update is called once per frame
}