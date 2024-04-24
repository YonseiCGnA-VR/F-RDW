using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R2C_Resetter : Resetter
{
    // Start is called before the first frame update

    float overallInjectedRotation;
    private int resetCount = 0;

    float requiredRotateSteerAngle = 0;//steering angle，rotate the physical plane and avatar together

    float requiredRotateAngle = 0;//normal rotation angle, only rotate avatar

    float rotateDir;//rotation direction, positive if rotate clockwise

    float speedRatio;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override bool IsResetRequired()
    {
        return IfCollisionHappens();
    }

    public override void InitializeReset()
    {
        //rotate by redirectionManager
        overallInjectedRotation = 0;
        Vector3 centerVector = new Vector3(0, 0, 0);
        getVectorToCenter(ref centerVector, Utilities.FlattenedPos3D(redirectionManager.currPos));
        //float angle = Utilities.GetSignedAngle(Utilities.FlattenedDir2D(redirectionManager.currDir), Utilities.FlattenedDir2D(centerVector));

        var currDir = Utilities.FlattenedDir2D(redirectionManager.currDir);
        var targetRealRotation = 360 - Vector2.Angle(Utilities.FlattenedDir2D(centerVector), currDir);//required rotation angle in real world

        rotateDir = -(int)Mathf.Sign(Utilities.GetSignedAngle(Utilities.UnFlatten(currDir), Utilities.FlattenedDir3D(centerVector)));

        requiredRotateSteerAngle = 360 - targetRealRotation;

        requiredRotateAngle = targetRealRotation;

        speedRatio = requiredRotateSteerAngle / requiredRotateAngle;

        SetHUD((int)rotateDir);

        ////rotate by simulatedWalker
        //// Physical angle
        //requiredRotateAngle = 180;

        ////rotate clockwise by default
        //SetHUD(1);
    }

    protected void getVectorToCenter(ref Vector3 userToCenter, Vector3 startPosition)
    {
        
        Vector3 trackingAreaPosition = Utilities.FlattenedPos3D(redirectionManager.trackingSpace.position);
        userToCenter = trackingAreaPosition - startPosition;
    }

    public override void InjectResetting()
    {
        var steerRotation = speedRatio * redirectionManager.deltaDir;
        if (Mathf.Abs(requiredRotateSteerAngle) <= Mathf.Abs(steerRotation) || requiredRotateAngle == 0)
        {//meet the rotation requirement
            InjectRotation(requiredRotateSteerAngle);

            //reset end
            redirectionManager.OnResetEnd();
            requiredRotateSteerAngle = 0;

        }
        else
        {//rotate the rotation calculated by ratio
            InjectRotation(steerRotation);
            requiredRotateSteerAngle -= Mathf.Abs(steerRotation);
        }

        //if (Mathf.Abs(overallInjectedRotation) < 180)
        //{
        //    float remainingRotation = redirectionManager.deltaDir > 0 ? 180 - overallInjectedRotation : -180 - overallInjectedRotation; // The idea is that we're gonna keep going in this direction till we reach objective
        //    if (Mathf.Abs(remainingRotation) < Mathf.Abs(redirectionManager.deltaDir) || requiredRotateAngle == 0)
        //    {
        //        InjectRotation(remainingRotation);
        //        redirectionManager.OnResetEnd();
        //        overallInjectedRotation += remainingRotation;
        //    }
        //    else
        //    {
        //        InjectRotation(redirectionManager.deltaDir);
        //        overallInjectedRotation += redirectionManager.deltaDir;
        //    }
        //}
        //Debug.Log("requiredRotateAngle:" + requiredRotateAngle + "; overallInjectedRotation:" + overallInjectedRotation);
    }


    //end reset
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
        //finish specified rotation
        if (rotateAngle >= requiredRotateAngle)
        {
            rotateAngle = requiredRotateAngle;
            //Avoid accuracy error
            requiredRotateAngle = 0;
        }
        else
        {
            requiredRotateAngle -= rotateAngle;
        }
        redirectionManager.simulatedWalker.RotateInPlace(rotateAngle * rotateDir);
    }
}
