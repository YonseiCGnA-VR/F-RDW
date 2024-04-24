using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public class ARC_Resetter : Resetter
{
    protected int resetCount;
    protected float requiredRotateSteerAngle = 0;//steering angle，rotate the physical plane and avatar together

    protected float requiredRotateAngle = 0;//normal rotation angle, only rotate avatar

    protected float rotateDir;//rotation direction, positive if rotate clockwise

    [HideInInspector]
    public GlobalConfiguration globalConfiguration;

    ARC_Redirector redirector;

    protected float speedRatio;

    protected List<float> List_ActualDistance3Way = new List<float>();
    protected List<float> List_VirtualDistance3Way = new List<float>();

    protected List<float> List_ActualDistance20Way = new List<float>();
    protected List<float> List_VirtualDistance20Way = new List<float>();


    // Start is called before the first frame update
    void Start()
    {
        Initialized();

    }

    protected void Initialized()
    {
        resetCount = 0;
        globalConfiguration = redirectionManager.globalConfiguration;
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public override void InitializeReset()
    {
        // calculate Angle when user reset occured
        var redirectorTmp = redirectionManager.redirector;

        if (redirectorTmp.GetType() == (typeof(ARC_Redirector)) || redirectorTmp.GetType().IsSubclassOf(typeof(ARC_Redirector)))
        {
            redirector = (ARC_Redirector)redirectorTmp;
            GameObject realUser = redirector.getRealUserTransformObj();

            Calc_NWay_Distances(realUser.transform, true, 20);

            Vector3 wallnormalvec = getBoundaryNormalVec();
            RaycastHit hit;
            List<Vector3> dir4way = new List<Vector3>();
            List<float> dist4way = new List<float>();
            dir4way.Add(Vector3.forward); // Global Coordinates
            dir4way.Add(Vector3.right);
            dir4way.Add(-Vector3.forward);
            dir4way.Add(-Vector3.right);


            int physicalWallMask = 1 << LayerMask.NameToLayer("PhysicalWall");
            int obstacleMask;
            for (int i = 0; i < 4; i++)
            {
                if (Physics.Raycast(realUser.transform.position, dir4way[i], out hit, 1000.0f, physicalWallMask))
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PhysicalWall") || hit.collider.gameObject.layer == LayerMask.NameToLayer("PhysicalUser"))
                    {
                        dist4way.Add(hit.distance);
                    }
                    else if (hit.collider.gameObject.name == "obstacle_" + i)
                    {
                        dist4way.Add(hit.distance);
                    }
                }
            }

            List<int> list_dir_FirstConditionSatisfied = new List<int>();
            IfCollisionHappens();
            for (int i = 0; i < 20; i++)
            {
                float value = 0.0f;
                Vector3 dir1 = Quaternion.AngleAxis(18 * i, Vector3.up) * Vector3.forward;
                //value = Vector3.Dot(wallnormalvec, dir1);
                value = Mathf.Abs(Vector3.Angle(wallnormalvec, dir1));

                //if (value > 0.0f)
                if (!pointreset)
                {
                    if (value <= 80.0f)
                    {
                        list_dir_FirstConditionSatisfied.Add(i);
                    }
                }
                else
                {
                    if (value <= 30.0f)
                    {
                        list_dir_FirstConditionSatisfied.Add(i);
                    }
                }
                
            }
            GameObject virtualUser = redirector.getVirtualUserTransformObj();

            // Find values that satisfy condi 1 && condi 2
            float virtualDist = 0.0f;
            int virtualWallMask = 1 << LayerMask.NameToLayer("VirtualWall");
            if (Physics.Raycast(virtualUser.transform.position, virtualUser.transform.forward, out hit, 1000.0f, virtualWallMask))
            {
                Debug.DrawLine(virtualUser.transform.position, virtualUser.transform.position + virtualUser.transform.forward * 1000.0f, Color.blue, 2.0f);
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("VirtualWall"))
                {
                    virtualDist = hit.distance;
                }

            }

            List<int> list_dir_SecondConditionSatisfied = new List<int>();
            List<float> list_dir_SecondConditionSatisfied_distance = new List<float>();
            for (int i = 0; i < list_dir_FirstConditionSatisfied.Count; i++)
            {
                float diff = List_ActualDistance20Way[list_dir_FirstConditionSatisfied[i]] - virtualDist;

                if (diff >= 0.0f)
                {
                    list_dir_SecondConditionSatisfied.Add(list_dir_FirstConditionSatisfied[i]);
                    list_dir_SecondConditionSatisfied_distance.Add(diff);
                }
            }


            // condi 1 && condi 2
            int dirIndex = 0;

            if (list_dir_SecondConditionSatisfied.Count > 0)
            {
                dirIndex = list_dir_SecondConditionSatisfied[list_dir_SecondConditionSatisfied_distance.IndexOf(list_dir_SecondConditionSatisfied_distance.Min())];
            }
            else
            {
                List<float> list_dist_ABS_FirstConditionSatisfied = new List<float>();
                for (int i = 0; i < list_dir_FirstConditionSatisfied.Count; i++)
                {
                    list_dist_ABS_FirstConditionSatisfied.Add(Mathf.Abs(List_ActualDistance20Way[list_dir_FirstConditionSatisfied[i]] - virtualDist));
                }

                dirIndex = list_dir_FirstConditionSatisfied[list_dist_ABS_FirstConditionSatisfied.IndexOf(list_dist_ABS_FirstConditionSatisfied.Min())];

            }


            Vector3 temp = Quaternion.AngleAxis(18 * dirIndex, Vector3.up) * Vector3.forward;

            var targetRealRotation = Vector2.SignedAngle(Utilities.FlattenedDir2D(realUser.transform.forward), new Vector2(temp.x, temp.z));

            if (targetRealRotation > 0)
            {
                //right Reset
                requiredRotateAngle = 360.0f - targetRealRotation;

            }
            else
            {
                requiredRotateAngle = 360.0f + targetRealRotation;
            }

            requiredRotateSteerAngle = 360 - requiredRotateAngle;
            //
            rotateDir = -(int)Mathf.Sign(Utilities.GetSignedAngle(redirectionManager.currDirReal,temp ));
            speedRatio = requiredRotateSteerAngle / requiredRotateAngle;
            //realTargetRotation = Matrix3x3.CreateRotation(targetAngle) * realUser.transform2D.forward;
            //virtualTargetRotation = Matrix3x3.CreateRotation(360) * virtualUser.transform2D.forward;


            Destroy(virtualUser);
            Destroy(realUser);

            SetHUD((int)rotateDir);

        }
        else
        {
            Debug.Log("RedirectorType: " + redirectorTmp.GetType());
            Debug.LogError("non-ARC redirector can't use ARC_resetter");
        }

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
    }

    public override void EndReset()
    {
        resetCount = resetCount + 1;
        Debug.Log("Reset Count : " + resetCount);
        DestroyHUD();
    }

    public override bool IsResetRequired()
    {
        return IfCollisionHappens();
    }



    public override void SimulatedWalkerUpdate()
    {
        var rotateAngle = redirectionManager.GetDeltaTime() * redirectionManager.globalConfiguration.rotationSpeed;

        //finish rotating
        if (rotateAngle >= requiredRotateAngle)
        {
            rotateAngle = requiredRotateAngle;
            requiredRotateAngle = 0;
        }
        else
        {
            requiredRotateAngle -= rotateAngle;
        }
        if (rotateDir == 1)
        {
            redirectionManager.simulatedWalker.RotateInPlace(rotateAngle);
        }
        else
        {
            redirectionManager.simulatedWalker.RotateInPlace(-rotateAngle);
        }
    }

    public void Calc_NWay_Distances(Transform _transform, bool bActual, int N_waycount)
    {
        float distance = 0.0f;
        RaycastHit hit;
        List<Vector3> direction = new List<Vector3>();

        int layerMask;
        int totalUserCount = globalConfiguration.avatarNum;
        if (bActual)
        {
            layerMask = 1 << LayerMask.NameToLayer("PhysicalWall"); // Physical Wall
        }
        else
        {
            layerMask = 1 << LayerMask.NameToLayer("VirtualWall");
        }
        Debug.Log("layerMask : " + layerMask);

        if (N_waycount == 3)
        {
            direction.Add(Vector3.forward);
            direction.Add(Vector3.right);
            direction.Add(-Vector3.right);

            if (bActual)
            {
                List_ActualDistance3Way.Clear();
            }
            else
            {
                List_VirtualDistance3Way.Clear();
            }
        }
        else if (N_waycount == 20)
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 result = Quaternion.AngleAxis(18 * i, Vector3.up) * Vector3.forward;
                direction.Add(result);
            }


            if (bActual)
            {
                List_ActualDistance20Way.Clear();
            }
            else
            {
                List_VirtualDistance20Way.Clear();
            }
        }

        for (int i = 0; i < direction.Count; i++)
        {
            if (Physics.Raycast(_transform.position, direction[i], out hit, 1000.0f, layerMask))
            {
                if (bActual)
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PhysicalWall"))
                    {
                        distance = hit.distance;
                        //Debug.Log(hit.collider.gameObject.name);
                        //Debug.DrawLine(_transform.position, _transform.position + direction[i], Color.red, Time.deltaTime);
                        //Debug.Log(hit.transform.gameObject.name);
                    }
                    else
                    {
                        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PhysicalUser"))
                        {
                            distance = hit.distance;
                            break;
                        }

                    }
                }
                else
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("VirtualWall"))
                    {
                        distance = hit.distance;
                    }

                }

            }
            if(distance <= 0)
            {
                distance = 0;
            }

            if (N_waycount == 3)
            {
                if (bActual)
                {
                    List_ActualDistance3Way.Add(distance);
                }
                else
                {
                    List_VirtualDistance3Way.Add(distance);
                }
            }
            else if (N_waycount == 20)
            {
                if (bActual)
                {
                    List_ActualDistance20Way.Add(distance);
                }
                else
                {
                    List_VirtualDistance20Way.Add(distance);
                }
            }

            distance = 0.0f;
        }
    }
}
