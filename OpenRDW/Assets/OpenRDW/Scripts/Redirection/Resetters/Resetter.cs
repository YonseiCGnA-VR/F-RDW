using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public abstract class Resetter : MonoBehaviour {
    private static float toleranceAngleError = 1;//Allowable angular error to prevent jamming
    [HideInInspector]
    public RedirectionManager redirectionManager;

    [HideInInspector]
    public MovementManager simulationManager;

    //spin in place hint
    public Transform prefabHUD = null;

    public float collisionangle;
    public Transform instanceHUD;
    public bool pointreset;

    private void Awake()
    {
        simulationManager = GetComponent<MovementManager>();
        collisionangle = 0.0f;
    }

    /// <summary>
    /// Function called when reset trigger is signalled, to see if resetter believes resetting is necessary.
    /// </summary>
    /// <returns></returns>
    public abstract bool IsResetRequired();

    public abstract void InitializeReset();

    public abstract void InjectResetting();

    public abstract void EndReset();

    //manipulation when update every reset
    public abstract void SimulatedWalkerUpdate();


    //rotate physical plane clockwise
    public void InjectRotation(float rotationInDegrees)
    {
        transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, rotationInDegrees);        
        GetComponentInChildren<KeyboardController>().SetLastRotation(rotationInDegrees);        
    }

    public void Initialize()
    {

    }

    public float GetDistanceToCenter()
    {
        return redirectionManager.currPosReal.magnitude;
    }

    //public Vector3 getBoundaryNormalVec()
    //{
    //    var realPos = new Vector2(redirectionManager.currPosReal.x, redirectionManager.currPosReal.z);
    //    var realDir = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
    //    var polygons = new List<List<Vector2>>();
    //    var trackingSpacePoints = simulationManager.generalManager.trackingSpacePoints;
    //    var obstaclePolygons = simulationManager.generalManager.obstaclePolygons;
    //    var userGameobjects = simulationManager.generalManager.redirectedAvatars;

    //    var forwardVector = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
    //    Vector3 normalVector = new Vector3(0, 0,0);
    //    //collect polygons for collision checking
    //    polygons.Add(trackingSpacePoints);
    //    foreach (var obstaclePolygon in obstaclePolygons)
    //        polygons.Add(obstaclePolygon);

    //    var ifCollisionHappens = false;
    //    foreach (var polygon in polygons)
    //    {
    //        for (int i = 0; i < polygon.Count; i++)
    //        {
    //            var p = polygon[i];
    //            var q = polygon[(i + 1) % polygon.Count];

    //            //judge vertices of ploygons
    //            if (IfCollideWithPoint(realPos, realDir, p))
    //            {
    //                ifCollisionHappens = true;
    //                normalVector = Utilities.RotateVector(Utilities.FlattenedDir2D(realDir), -180);
    //                //Debug.Log("point reset true");
    //                break;
    //            }

    //            //judge edge collision
    //            if (Vector3.Cross(q - p, realPos - p).magnitude / (q - p).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER//distance
    //                && Vector2.Dot(q - p, realPos - p) >= 0 && Vector2.Dot(p - q, realPos - q) >= 0//range
    //                )
    //            {
    //                //if collide with border
    //                if (Mathf.Abs(Cross(q - p, realDir)) > 1e-3 && Mathf.Sign(Cross(q - p, realDir)) != Mathf.Sign(Cross(q - p, realPos - p)))
    //                {
    //                    if (Vector2.SignedAngle(q - p, forwardVector) < 0)
    //                    {
    //                        normalVector = Utilities.RotateVector(q - p, -90);
    //                        normalVector = new Vector3(normalVector.x, 0, normalVector.y);
    //                        ifCollisionHappens = true;
    //                        break;
    //                    }

    //                }
    //            }
    //        }
    //        if (ifCollisionHappens)
    //            break;
    //    }
    //    return normalVector.normalized;
    //}
    public Vector3 getBoundaryNormalVec()
    {
        var realPos = new Vector2(redirectionManager.currPosReal.x, redirectionManager.currPosReal.z);
        var realDir = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
        var virDir = new Vector2(redirectionManager.currDir.x, redirectionManager.currDir.z);
        var polygons = new List<List<Vector2>>();
        var trackingSpacePoints = simulationManager.generalManager.trackingSpacePoints;
        var obstaclePolygons = simulationManager.generalManager.obstaclePolygons;
        var userGameobjects = simulationManager.generalManager.redirectedAvatars;

        var forwardVector = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
        Vector3 normalVector = new Vector3(0, 0, 0);
        //collect polygons for collision checking
        polygons.Add(trackingSpacePoints);

        var ifCollisionHappens = false;
        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                var q = polygon[(i + 1) % polygon.Count];

                if (IfCollideWithPoint(realPos, realDir, p))
                {
                    p = polygon[i];
                    q = polygon[(i + 1) % polygon.Count];
                    var r = polygon[(i + polygon.Count - 1) % polygon.Count];
                    Vector2 x = p - q;
                    Vector2 y = p - r;
                    Vector2 nom = x + y;

                    normalVector = Utilities.RotateVector(x + y, -180);
                    normalVector = new Vector3(normalVector.x, 0, normalVector.y);
                    ifCollisionHappens = true;
                    break;
                }

                if (IfCollideWithPoint(realPos, realDir, q))
                {
                    p = polygon[i];
                    q = polygon[(i + 1) % polygon.Count];
                    var r = polygon[(i + 2) % polygon.Count];
                    Vector2 x = q - p;
                    Vector2 y = q - r;
                    Vector2 nom = x + y;
                    normalVector = Utilities.RotateVector(x + y, -180);
                    normalVector = new Vector3(normalVector.x, 0, normalVector.y);
                    ifCollisionHappens = true;
                    break;
                }

                //judge edge collision
                if (Vector3.Cross(q - p, realPos - p).magnitude / (q - p).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER//distance
                    && Vector2.Dot(q - p, realPos - p) >= 0 && Vector2.Dot(p - q, realPos - q) >= 0//range
                    )
                {
                    //if collide with border
                    if (Mathf.Abs(Cross(q - p, realDir)) > 1e-3 && Mathf.Sign(Cross(q - p, realDir)) != Mathf.Sign(Cross(q - p, realPos - p)))
                    {
                        normalVector = Utilities.RotateVector(q - p, -90);
                        normalVector = new Vector3(normalVector.x, 0, normalVector.y);
                        ifCollisionHappens = true;
                        break;

                    }
                }


            }
            if (ifCollisionHappens)
                break;
        }
        polygons.Clear();
        if (!ifCollisionHappens)
        {
            foreach (var obstaclePolygon in obstaclePolygons)
                polygons.Add(obstaclePolygon);

            foreach (var polygon in polygons)
            {
                for (int i = 0; i < polygon.Count; i++)
                {
                    var p = polygon[i];
                    var q = polygon[(i + 1) % polygon.Count];

                    if (IfCollideWithPoint(realPos, realDir, p))
                    {
                        p = polygon[i];
                        q = polygon[(i + 1) % polygon.Count];
                        var r = polygon[(i + polygon.Count - 1) % polygon.Count];
                        Vector2 x = p - q;
                        Vector2 y = p - r;
                        Vector2 nom = x + y;

                        normalVector = nom;
                        normalVector = new Vector3(normalVector.x, 0, normalVector.y);
                        ifCollisionHappens = true;
                        break;
                    }

                    if (IfCollideWithPoint(realPos, realDir, q))
                    {
                        p = polygon[i];
                        q = polygon[(i + 1) % polygon.Count];
                        var r = polygon[(i + 2) % polygon.Count];
                        Vector2 x = q - p;
                        Vector2 y = q - r;
                        Vector2 nom = x + y;
                        normalVector = nom;
                        normalVector = new Vector3(normalVector.x, 0, normalVector.y);
                        ifCollisionHappens = true;
                        break;
                    }

                    //judge edge collision
                    if (Vector3.Cross(q - p, realPos - p).magnitude / (q - p).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER//distance
                        && Vector2.Dot(q - p, realPos - p) >= 0 && Vector2.Dot(p - q, realPos - q) >= 0//range
                        )
                    {
                        //if collide with border
                        if (Mathf.Abs(Cross(q - p, realDir)) > 1e-3 && Mathf.Sign(Cross(q - p, realDir)) != Mathf.Sign(Cross(q - p, realPos - p)))
                        {
                            normalVector = Utilities.RotateVector(q - p, +90);
                            normalVector = new Vector3(normalVector.x, 0, normalVector.y);
                            ifCollisionHappens = true;
                            break;

                        }
                    }
                }
            }

        }
        return normalVector.normalized;
    }

//public bool IfCollisionHappens()
//    {
//        var realPos = new Vector2(redirectionManager.currPosReal.x, redirectionManager.currPosReal.z);
//        var realDir = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
//        var polygons = new List<List<Vector2>>();
//        var trackingSpacePoints = simulationManager.generalManager.trackingSpacePoints;
//        var obstaclePolygons = simulationManager.generalManager.obstaclePolygons;
//        var userGameobjects = simulationManager.generalManager.redirectedAvatars;

//        var forwardVector = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
        
//        //collect polygons for collision checking
//        polygons.Add(trackingSpacePoints);
//        foreach (var obstaclePolygon in obstaclePolygons)
//            polygons.Add(obstaclePolygon);

//        var ifCollisionHappens = false;
//        foreach (var polygon in polygons)
//        {
//            for (int i = 0; i < polygon.Count; i++)
//            {
//                var p = polygon[i];
//                var q = polygon[(i + 1) % polygon.Count];

//                //judge vertices of ploygons
//                if (IfCollideWithPoint(realPos, realDir, p) || IfCollideWithPoint(realPos, realDir, q))
//                {
//                    ifCollisionHappens = true;
//                    pointreset = true;
//                    //Debug.Log("point reset true");
//                    break;
//                }
                
//                //judge edge collision
//                if (Vector3.Cross(q - p, realPos - p).magnitude / (q - p).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER//distance
//                    && Vector2.Dot(q - p, realPos - p) >= 0 && Vector2.Dot(p - q, realPos - q) >= 0//range
//                    )
//                {                    
//                    //if collide with border
//                    if (Mathf.Abs(Cross(q - p, realDir)) > 1e-3 && Mathf.Sign(Cross(q - p, realDir)) != Mathf.Sign(Cross(q - p, realPos - p)))
//                    {
//                        if(Vector2.SignedAngle(q - p, forwardVector) < 0)
//                        {
//                            ifCollisionHappens = true;
//                            pointreset = false;
//                            break;
//                        }

//                    }
//                }
//            }
//            if (ifCollisionHappens)
//                break;
//        }        
        
//        if (!ifCollisionHappens)
//        {//if collide with other avatars
//            foreach (var us in userGameobjects)
//            {                
//                //ignore self
//                if (us.Equals(gameObject))
//                    continue;
//                //collide with other avatars
//                if (IfCollideWithPoint(realPos, realDir, Utilities.FlattenedPos2D(us.GetComponent<RedirectionManager>().currPosReal)))
//                {
//                    ifCollisionHappens = true;                    
//                    break;
//                }
//            }
//        }

//        return ifCollisionHappens;
//    }

    public bool IfCollisionHappens()
    {
        var realPos = new Vector2(redirectionManager.currPosReal.x, redirectionManager.currPosReal.z);
        var realDir = new Vector2(redirectionManager.currDirReal.x, redirectionManager.currDirReal.z);
        var polygons = new List<List<Vector2>>();
        var trackingSpacePoints = simulationManager.generalManager.trackingSpacePoints;
        var obstaclePolygons = simulationManager.generalManager.obstaclePolygons;
        var userGameobjects = simulationManager.generalManager.redirectedAvatars;

        //collect polygons for collision checking
        polygons.Add(trackingSpacePoints);
        foreach (var obstaclePolygon in obstaclePolygons)
            polygons.Add(obstaclePolygon);

        var ifCollisionHappens = false;
        var x = 1;
        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                var q = polygon[(i + 1) % polygon.Count];

                //judge vertices of ploygons

                if (IfCollideWithPoint(realPos, realDir, p) || IfCollideWithPoint(realPos, realDir, q))
                {
                    ifCollisionHappens = true;

                    if (x == 1)
                        collisionangle = Vector2.Angle(q - p, realDir);
                    else
                        collisionangle = 180 - Vector2.Angle(q - p, realDir);
                    pointreset = true;
                    break;
                }


                //judge edge collision
                if (Vector3.Cross(q - p, realPos - p).magnitude / (q - p).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER//distance
                    && Vector2.Dot(q - p, realPos - p) >= 0 && Vector2.Dot(p - q, realPos - q) >= 0//range
                    )
                {
                    //if collide with border
                    if (Mathf.Abs(Cross(q - p, realDir)) > 1e-3 && Mathf.Sign(Cross(q - p, realDir)) != Mathf.Sign(Cross(q - p, realPos - p)))
                    {

                        ifCollisionHappens = true;
                        if (x == 1)
                            collisionangle = Vector2.Angle(q - p, realDir);
                        else
                            collisionangle = 180 - Vector2.Angle(q - p, realDir);
                        pointreset = false;
                        break;
                    }

                }
            }
            if (ifCollisionHappens)
                break;
            x++;
        }

        if (!ifCollisionHappens)
        {//if collide with other avatars
            foreach (var us in userGameobjects)
            {
                //ignore self
                if (us.Equals(gameObject))
                    continue;
                //collide with other avatars
                if (IfCollideWithPoint(realPos, realDir, Utilities.FlattenedPos2D(us.GetComponent<RedirectionManager>().currPosReal)))
                {


                    collisionangle = 90;
                    ifCollisionHappens = true;
                    break;
                }
            }
        }

        return ifCollisionHappens;
    }


    //if collide with vertices
    public bool IfCollideWithPoint(Vector2 realPos, Vector2 realDir, Vector2 obstaclePoint)
    {
        //judge point, if the avatar will walks into a circle obstacle
        var pointAngle = Vector2.Angle(obstaclePoint - realPos, realDir);
        return (obstaclePoint - realPos).magnitude <= redirectionManager.globalConfiguration.RESET_TRIGGER_BUFFER && pointAngle < 90 - toleranceAngleError;
    }
    private float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    
    //initialize spin in place hint, rotateDir==1:rotate clockwise, otherwise, rotate counter clockwise
    public void SetHUD(int rotateDir)
    {
        if (prefabHUD == null)
            prefabHUD = Resources.Load<Transform>("Resetter HUD");
        
        if (simulationManager.ifVisible) {
            instanceHUD = Instantiate(prefabHUD);
            instanceHUD.parent = redirectionManager.headTransform;
            instanceHUD.localPosition = instanceHUD.position;
            instanceHUD.localRotation = instanceHUD.rotation;

            //rotate clockwise
            if (rotateDir == 1)
            {
                instanceHUD.GetComponent<TextMesh>().text = "Turn\n→";
            }
            else
            {
                instanceHUD.GetComponent<TextMesh>().text = "Turn\n←";
            }
        }
    }

    //destroy HUD object
    public void DestroyHUD() {
        if (instanceHUD != null)
            Destroy(instanceHUD.gameObject);
    }
}
