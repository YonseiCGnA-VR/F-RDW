using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VirtualPathGenerator
{

    public static int RANDOM_SEED = 3041;
    
    public static Vector2 defaultStartPoint = Vector2.zero;

    public enum DistributionType { Normal, Uniform };
    public enum AlternationType { None, Random, Constant };

    static float zigLength = 5f;
    static float zagAngle = 140;
    static int zigzagWaypointCount = 40;

    public struct SamplingDistribution
    {
        public DistributionType distributionType;
        public float min, max;
        public float mu, sigma;
        public AlternationType alternationType; // Used typicaly for the case of generating angles, where we want the value to be negated at random
        public SamplingDistribution(DistributionType distributionType, float min, float max, AlternationType alternationType = AlternationType.None, float mu = 0, float sigma = 0)
        {
            this.distributionType = distributionType;
            this.min = min;
            this.max = max;
            this.mu = mu;
             this.sigma = sigma;
            this.alternationType = alternationType;
        }
    }

    public struct PathSeed
    {
        public int waypointCount;
        public SamplingDistribution distanceDistribution;
        public SamplingDistribution angleDistribution;
        public PathSeed(SamplingDistribution distanceDistribution, SamplingDistribution angleDistribution, int waypointCount)
        {
            this.distanceDistribution = distanceDistribution;
            this.angleDistribution = angleDistribution;
            this.waypointCount = waypointCount;
        }
        public static PathSeed GetPathSeed90Turn()
        {
            SamplingDistribution distanceSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, 3, 8);
            SamplingDistribution angleSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, 90, 90, AlternationType.Random);
            int waypointCount = 40;
            return new PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
        }

        public static PathSeed GetPathSeedSawtooth()
        {
            SamplingDistribution distanceSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, zigLength, zigLength);
            SamplingDistribution angleSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, zagAngle, zagAngle, AlternationType.Constant);
            int waypointCount = zigzagWaypointCount;
            return new PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
        }

        public static PathSeed GetPathSeedRandomTurn()
        {            
            SamplingDistribution distanceSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, 4, 8);

            SamplingDistribution angleSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, -180, 180);
            int waypointCount = 50;
            return new PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
        }

        public static PathSeed GetPathSeedStraightLine()
        {
            SamplingDistribution distanceSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, 20, 20);            
            SamplingDistribution angleSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, 0, 0);
            int waypointCount = 10;
            return new PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
        }

        public static PathSeed GetPathSeedDenceWaypointRandomTurn(float angleThreshold, float pathLength)
        {
            float minDist = 0.02f;
            float maxDist = 0.04f;
            float meanDist = (minDist + maxDist) / 2.0f;
            //SamplingDistribution distanceSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, minDist, maxDist, AlternationType.None, meanDist, maxDist - meanDist);
            SamplingDistribution distanceSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, minDist, maxDist);
            SamplingDistribution angleSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, -angleThreshold, angleThreshold);
            //SamplingDistribution inPlaceTurnSamplingDistribution = new SamplingDistribution(DistributionType.Uniform, -180, 180);
            int waypointCount = (int)(pathLength / meanDist);
            return new PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
        }
    }

    static float SampleUniform(float min, float max)
    {        
        return Random.Range(min, max);
    }

    static float SampleNormal(float mu = 0, float sigma = 1, float min = float.MinValue, float max = float.MaxValue)
    {
        // From: http://stackoverflow.com/questions/218060/random-gaussian-variables
        float r1 = Random.value;
        float r2 = Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(r1)) * Mathf.Sin(2.0f * Mathf.PI * r2); // Random Normal(0, 1)
        float randNormal = mu + randStdNormal * sigma;
        return Mathf.Max(Mathf.Min(randNormal, max), min);
    }

    static float SampleDistribution(SamplingDistribution distribution)
    {
        float retVal = 0;
        if (distribution.distributionType == DistributionType.Uniform)
        {
            retVal = SampleUniform(distribution.min, distribution.max);
        }
        else if (distribution.distributionType == DistributionType.Normal)
        {
            retVal = SampleNormal(distribution.mu, distribution.sigma, distribution.min, distribution.max);
        }
        //if inverse
        if (distribution.alternationType == AlternationType.Random && Random.value < 0.5f)
            retVal = -retVal;
        return retVal;
    }
    //generate waypoints by pathSeed，ensure the same in every trial



    public static List<Vector2> GenerateInitialPathByPathSeed(PathSeed pathSeed, float targetDist, out float sumOfDistances, out float sumOfRotations)
    {
        Vector2 initialPosition = Vector2.zero;
        Vector2 initialForward = new Vector2(0, 1);//along z axis
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        List<Vector2> waypoints = new List<Vector2>(pathSeed.waypointCount);
        Vector2 position = initialPosition;
        Vector2 forward = initialForward.normalized;
        Vector2 nextPosition, nextForward;
        float sampledDistance, sampledRotation;
        sumOfDistances = 0;
        sumOfRotations = 0;
        int alternator = 1;

        //add start point
        waypoints.Add(position);
        bool finished = false;
        for (; !finished;)
        {
            sampledDistance = SampleDistribution(pathSeed.distanceDistribution);
            if (sampledDistance + sumOfDistances >= targetDist)
            {
                finished = true;
                sampledDistance = targetDist - sumOfDistances;
            }
            // need to check Collision
            sampledRotation = SampleDistribution(pathSeed.angleDistribution);
            if (pathSeed.angleDistribution.alternationType == AlternationType.Constant)
                sampledRotation *= alternator;
            nextPosition = position + sampledDistance * forward;
            nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
            waypoints.Add(nextPosition);
            position = nextPosition;
            forward = nextForward;
            sumOfDistances += sampledDistance;
            sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
            alternator *= -1;
        }

        return waypoints;
    }

    public static List<Vector2> GenerateInitialPathByPathSeed(PathSeed pathSeed, float targetDist, out float sumOfDistances, out float sumOfRotations, int randomSeed)
    {
        RANDOM_SEED = randomSeed;
        Vector2 initialPosition = Vector2.zero;
        Vector2 initialForward = new Vector2(0, 1);//along z axis
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        List<Vector2> waypoints = new List<Vector2>(pathSeed.waypointCount);
        Vector2 position = initialPosition;
        Vector2 forward = initialForward.normalized;
        Vector2 nextPosition, nextForward;
        float sampledDistance, sampledRotation;
        sumOfDistances = 0;
        sumOfRotations = 0;
        int alternator = 1;

        //add start point
        waypoints.Add(position);
        bool finished = false;
        for (; !finished;)
        {
            sampledDistance = SampleDistribution(pathSeed.distanceDistribution);
            if (sampledDistance + sumOfDistances >= targetDist)
            {
                finished = true;
                sampledDistance = targetDist - sumOfDistances;
            }
            // need to check Collision
            sampledRotation = SampleDistribution(pathSeed.angleDistribution);
            if (pathSeed.angleDistribution.alternationType == AlternationType.Constant)
                sampledRotation *= alternator;
            nextPosition = position + sampledDistance * forward;
            nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
            waypoints.Add(nextPosition);
            position = nextPosition;
            forward = nextForward;
            sumOfDistances += sampledDistance;
            sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
            alternator *= -1;
        }
        return waypoints;
    }
    //generate circle path



    public static List<Vector2> GenerateCirclePath(float radius, int waypointNum, out float sumOfDistances, out float sumOfRotations, bool if8 = false)
    {
        Vector2 initialPosition = Vector2.zero;
        Vector2 initialForward = new Vector2(0, 1);
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        List<Vector2> waypoints = new List<Vector2>();
        Vector2 position = initialPosition;
        Vector2 forward = initialForward.normalized;
        Vector2 nextPosition;

        waypoints.Add(position);

        sumOfDistances = 0;
        sumOfRotations = 0;

        var center = new Vector2(radius, 0);
        var startVec = -center;
        float sampledRotation = 360f / waypointNum;
        for (int i = 0; i < waypointNum; i++)
        {
            var vec = Utilities.RotateVector(startVec, -sampledRotation * (i + 1));//clockwise
            nextPosition = center + vec;            
            waypoints.Add(nextPosition);
            sumOfDistances += (nextPosition - position).magnitude;
            sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
            position = nextPosition;
        }
        if (if8) {
            center *= -1;
            startVec *= -1;
            for (int i = 0; i < waypointNum; i++)
            {
                var vec = Utilities.RotateVector(startVec, sampledRotation * (i + 1));
                nextPosition = center + vec;
                waypoints.Add(nextPosition);
                sumOfDistances += (nextPosition - position).magnitude;
                sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
                position = nextPosition;
            }
        }
        return waypoints;
    }

    public static Vector2 GetRandomPositionWithinBounds(float minX, float maxX, float minZ, float maxZ)
    {
        return new Vector2(SampleUniform(minX, maxX), SampleUniform(minZ, maxZ));
    }

    public static Vector2 GetRandomPositionWithinBounds(float minX, float maxX, float minZ, float maxZ, float minLength, float maxLength, Vector2 currPos)
    {
        Vector2 nextPos = new Vector2(SampleUniform(minX, maxX), SampleUniform(minZ, maxZ));
        while((nextPos - currPos).magnitude < minLength)
        {
            nextPos = new Vector2(SampleUniform(minX, maxX), SampleUniform(minZ, maxZ));
        }
        return nextPos;
    }

    public static Vector2 GetRandomPositionWithinBounds(float minLength, float maxLength)
    {


        float distance = SampleUniform(minLength, maxLength);
        Vector2 nextPos = new Vector2(0, distance);
        //float angle = SampleNormal(0, 90);
        float angle = SampleUniform(0, 360);
        nextPos = Utilities.RotateVector(nextPos, angle);
        return nextPos;
    }


    public static Vector2 GetRandomForward()
    {
        float angle = SampleUniform(0, 360);
        return Utilities.RotateVector(Vector2.up, angle).normalized; // Over-protective with the normalizing
    }


    public static bool FasterLineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {

        Vector2 a = p2 - p1;
        Vector2 b = p3 - p4;
        Vector2 c = p1 - p3;

        float alphaNumerator = b.y * c.x - b.x * c.y;
        float alphaDenominator = a.y * b.x - a.x * b.y;
        float betaNumerator = a.x * c.y - a.y * c.x;
        float betaDenominator = a.y * b.x - a.x * b.y;

        bool doIntersect = true;

        if (alphaDenominator == 0 || betaDenominator == 0)
        {
            doIntersect = false;
        }
        else
        {

            if (alphaDenominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
                {
                    doIntersect = false;

                }
            }
            else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
            {
                doIntersect = false;
            }

            if (doIntersect && betaDenominator > 0)
            {
                if (betaNumerator < 0 || betaNumerator > betaDenominator)
                {
                    doIntersect = false;
                }
            }
            else if (betaNumerator > 0 || betaNumerator < betaDenominator)
            {
                doIntersect = false;
            }
        }

        return doIntersect;
    }

    public static List<List<Vector2>> getVEPolygons(string mapName)
    {
        List<List<Vector2>> polygons = new List<List<Vector2>>();
        GameObject mazeSpawner = GameObject.Find(mapName);
        //GameObject mazeSpawner = GameObject.Find("Maze");
        var temp = mazeSpawner.gameObject.GetComponentsInChildren<Transform>();

        //GameObject[] temp = GameObject.FindGameObjectsWithTag("VirtualWall");
        //Transform allChildren = map.gameObject.GetComponentInChildren<Transform>();
        int layerMask = 1 << LayerMask.NameToLayer("VirtualWall");
        //int totalUserCount = globalConfiguration.avatarNum;
        //if (bActual)
        //{
        //    layerMask = 1 << LayerMask.NameToLayer("PhysicalWall"); // Physical Wall
        //}
        //else
        //{
        //    layerMask = 1 << LayerMask.NameToLayer("VirtualWall");
        //}

        for (int i = 1; i < temp.Length; i++)
        {
            if(temp[i].gameObject.layer == LayerMask.NameToLayer("VirtualWall"))
            {
                if(temp[i].gameObject.name == "WallPrefab(Clone)")
                {
                    // add polygon

                    Vector2 objectPosition = Utilities.FlattenedPos2D(temp[i].gameObject.transform.position);
                    Vector2 objectForward = Utilities.FlattenedDir2D(temp[i].gameObject.transform.forward);
                    Vector2 objectRight = Utilities.FlattenedDir2D(temp[i].gameObject.transform.right);

                    float longSideLength = 2.0f;
                    float shortSideLength = 1.0f;

                    Vector2 widthVector = objectRight * longSideLength;
                    Vector2 depthVector = objectForward * shortSideLength;

                    Vector2 quadrant1 = objectPosition + widthVector + depthVector;
                    Vector2 quadrant2 = objectPosition - widthVector + depthVector;
                    Vector2 quadrant3 = objectPosition - widthVector - depthVector;
                    Vector2 quadrant4 = objectPosition + widthVector - depthVector;

                    polygons.Add(new List<Vector2> {
                        new Vector2(quadrant1.x, quadrant1.y),
                        new Vector2(quadrant2.x, quadrant2.y),
                        new Vector2(quadrant3.x, quadrant3.y),
                        new Vector2(quadrant4.x, quadrant4.y),
                    });
                }
                //else if (temp[i].gameObject.name == "PillarPrefab(Clone)")
                else
                {
                    Vector2 objectPosition = Utilities.FlattenedPos2D( temp[i].gameObject.transform.position);
                    float size = 0.5f;
                    polygons.Add(new List<Vector2> {
                        new Vector2(objectPosition.x + size , objectPosition.y + size), // quadrant 1
                        new Vector2(objectPosition.x - size, objectPosition.y + size), // quadramt 2
                        new Vector2(objectPosition.x - size,objectPosition.y - size), // quadrant 3
                        new Vector2(objectPosition.x + size, objectPosition.y - size), // quadrant 4
                    });
                }

            }
        }


        return polygons;
    }

    public static List<Vector2> GenerateInitialPathByPathSeedDenseWaypoint(PathSeed pathSeed, float targetDist, out float sumOfDistances, out float sumOfRotations, int randomSeed, string mapName)
    {
        Vector2 initialPosition = new Vector2(0, 0);//Random.Range(-100,100),Random.Range(-100,100));
        Vector2 initialForward = new Vector2(0, 1);//along z axis
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        List<Vector2> waypoints = new List<Vector2>(pathSeed.waypointCount);
        Vector2 position = initialPosition;
        Vector2 forward = initialForward.normalized;
        Vector2 nextPosition, nextForward;

        float sampledDistance, sampledRotation;
        sumOfDistances = 0;
        sumOfRotations = 0;
        int alternator = 1;

        waypoints.Add(position);
        bool finished = false;

        //var vePolygons = new List<List<Vector2>>();
        var vePolygons = getVEPolygons(mapName);

        float distanceToNextTurn = 0.0f;
        float traveledDistance = 0;
        Vector2 routeVector = new Vector2(0, 0);

        if(vePolygons.Count <= 1)
        {
            return waypoints;
        }
        bool needToCheckInter = true;

        for (; !finished;)
        {
            if (needToCheckInter)
            {
                do
                {
                    //float rotAngle = Random.Range(-180.0f, 180.0f);
                    //forward = Utilities.RotateVector(forward, rotAngle);
                    //distanceToNextTurn = Random.Range(2.0f, 4.0f);
                    //routeVector =  distanceToNextTurn * forward;
                    //traveledDistance = 0;
                    //bool flag = true;
                    //routeVector = GetRandomPositionWithinBounds(2, 3);

                    routeVector = GetRandomPositionWithinBounds(3, 6);
                    forward = routeVector.normalized;
                    distanceToNextTurn = routeVector.magnitude;

                    traveledDistance = 0;
                    bool flag = true;
                    // checking Collision
                    //if (vePolygons.Count <= 1)
                    //{
                    //    flag = false;
                    //    break;
                    //}
                    //foreach (var p in vePolygons)
                    //{
                    //    if(vePolygons.Count <= 1)
                    //    {
                    //        flag = false;
                    //        break;
                    //    }

                    //    for (int i = 0; i < p.Count; i++)
                    //    {
                    //        Vector2 x = p[i];
                    //        Vector2 y = p[(i + 1) % p.Count];

                    //        flag = FasterLineSegmentIntersection(x, y, position, position + routeVector);
                    //        if (flag)
                    //        {
                    //            break;
                    //        }
                    //    }
                    //    if (flag)
                    //    {
                    //        break;
                    //    }
                    //}
                    flag = hasIntersection(vePolygons, position, routeVector);
                    if (!flag)
                    {
                        // Intersection not happen
                        needToCheckInter = false;
                    }
                    if(distanceToNextTurn < 3)
                    {
                        flag = true;
                    }
                } while (needToCheckInter);

            }

            nextPosition = generateNextPosition(sumOfDistances);
            if(hasIntersection(vePolygons, position, nextPosition))
            {
                needToCheckInter = true;
            }
            else
            {
                if (traveledDistance + sampledDistance <= distanceToNextTurn)
                {
                    traveledDistance = traveledDistance + sampledDistance;
                    //nextForward = (nextPosition - position).normalized;
                    // nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
                    waypoints.Add(nextPosition);
                    Debug.Log("distanceToNextTurn : " + distanceToNextTurn);
                    //Debug.Log(nextPosition);
                    position = nextPosition;
                    forward = nextForward;
                    sumOfDistances += sampledDistance;
                    sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
                    alternator *= -1;
                }
                else
                {
                    needToCheckInter = true;
                }
            }
            //do
            //{
            //    nextPosition = generateNextPosition(sumOfDistances);

            //} while (hasIntersection(vePolygons, position, nextPosition));
            


            //bool flag = true;

            //while (flag)
            //{
            //    if (vePolygons.Count == 0)
            //    {
            //        return waypoints;
            //    }
            //    foreach (var p in vePolygons)
            //    {
            //        if (vePolygons.Count == 1)
            //        {
            //            return waypoints;
            //        }
            //        for (int i = 0; i < p.Count; i++)
            //        {
            //            Vector2 x = p[i];
            //            Vector2 y = p[(i + 1) % p.Count];
            //            //flag = FasterLineSegmentIntersection(x, y, position, routeVector);
            //            //flag = FasterLineSegmentIntersection(x, y, position, nextPosition);
            //            if (FasterLineSegmentIntersection(x, y, position, routeVector) || FasterLineSegmentIntersection(x, y, position, nextPosition))
            //            {
            //                break;
            //                flag = true;
            //            }
            //            //if (flag)
            //            //{
            //            //    break;


            //            //}
            //        }
            //        if (flag)
            //        {
            //            //nextPosition = GetRandomPositionWithinBounds(0, 20.0f, 0, 20.0f);
            //            //float rotAngle = Random.Range(0, 360);
            //            //forward = Utilities.RotateVector(forward,rotAngle);
            //            //nextPosition = generateNextPosition(sumOfDistances);
            //            float rotAngle = Random.Range(-180, 180);
            //            forward = Utilities.RotateVector(forward, rotAngle);
            //            distanceToNextTurn = Random.Range(3, 8);
            //            routeVector = position + distanceToNextTurn * forward;
            //            traveledDistance = 0;
            //            break;
            //        }
            //    }
            //}
            if (sumOfDistances >= targetDist)
            {
                finished = true;
            }

        }

        return waypoints;

        

        Vector2 generateNextPosition(float sumOfDistance)
        {

            sampledDistance = SampleDistribution(pathSeed.distanceDistribution);

            if (sampledDistance + sumOfDistance >= targetDist)
            {
                finished = true;
                sampledDistance = targetDist - sumOfDistance;
            }

            sampledRotation = SampleDistribution(pathSeed.angleDistribution);

            if (pathSeed.angleDistribution.alternationType == AlternationType.Constant)
                sampledRotation *= alternator;
            nextPosition = position + sampledDistance * forward;
            nextForward = Utilities.RotateVector(forward, sampledRotation).normalized;

            return nextPosition;
        }
    }

    public static bool hasIntersection(List<List<Vector2>> vePolygons, Vector2 position, Vector2 routeVector)
    {
        bool flag = true;
        if (vePolygons.Count <= 1)
        {
            flag = false;

        }
        else
        {
            foreach (var p in vePolygons)
            {
                if (vePolygons.Count <= 1)
                {
                    flag = false;
                    break;
                }

                for (int i = 0; i < p.Count; i++)
                {
                    Vector2 x = p[i];
                    Vector2 y = p[(i + 1) % p.Count];

                    flag = FasterLineSegmentIntersection(x, y, position, position + routeVector);
                    if (flag)
                    {
                        break;
                    }
                }
                if (flag)
                {
                    break;
                }
            }
        }


        return flag;
    }


    public static List<Vector2> GenerateInitialPathByPathSeedWithCollision(PathSeed pathSeed, float targetDist, out float sumOfDistances, out float sumOfRotations, int randomSeed, string mapName)
    {
        RANDOM_SEED = randomSeed;
        Vector2 initialPosition = new Vector2(0,0);//Random.Range(-100,100),Random.Range(-100,100));
        Vector2 initialForward = new Vector2(0, 1);//along z axis
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        List<Vector2> waypoints = new List<Vector2>(pathSeed.waypointCount);
        Vector2 position = initialPosition;
        Vector2 forward = initialForward.normalized;
        Vector2 nextPosition, nextForward;

        float sampledDistance, sampledRotation;
        sumOfDistances = 0;
        sumOfRotations = 0;
        int alternator = 1;
        //add start point
        waypoints.Add(position);
        bool finished = false;

        //var vePolygons = new List<List<Vector2>>();
        var vePolygons = getVEPolygons(mapName);
        // need a function adding Virtual Environment Polygons

        float distanceToNextTurn = Random.Range(6.0f, 12);
        float traveledDistance = 0;
        Vector2 routeVector = new Vector2(0, 0);

        for (; !finished;)
        {



            nextPosition = generateNextPosition(sumOfDistances,ref traveledDistance);

            bool flag = true;

            while (flag)
            {
                if(vePolygons.Count == 0)
                {
                    return waypoints;
                }
                foreach (var p in vePolygons)
                {
                    if(vePolygons.Count == 1)
                    {
                        return waypoints;
                    }
                    for (int i = 0; i < p.Count; i++)
                    {
                        Vector2 x = p[i];
                        Vector2 y = p[(i + 1) % p.Count];
                        //flag = FasterLineSegmentIntersection(x, y, position, routeVector);
                        flag = FasterLineSegmentIntersection(x, y, position, nextPosition);
                        //if(FasterLineSegmentIntersection(x, y, position, routeVector) || FasterLineSegmentIntersection(x, y, position, position + nextPosition))
                        //{
                        //    break;
                        //    flag = true;
                        //}

                        if (flag)
                        {
                            break;


                        }
                    }
                    if (flag)
                    {
                        //nextPosition = GetRandomPositionWithinBounds(0, 20.0f, 0, 20.0f);


                        //bool flag2 = true;
                        //do
                        //{
                        //    float rotAngle = Random.Range(0, 360);
                        //    forward = Utilities.RotateVector(forward, rotAngle);
                        //    distanceToNextTurn = 7;
                        //    Vector2 nextRoute = position + (distanceToNextTurn * forward);
                        //    foreach (var m in vePolygons)
                        //    {
                        //        if (vePolygons.Count == 1)
                        //        {
                        //            return waypoints;
                        //        }
                        //        for (int i = 0; i < p.Count; i++)
                        //        {
                        //            Vector2 x = m[i];
                        //            Vector2 y = m[(i + 1) % m.Count];
                        //            //flag = FasterLineSegmentIntersection(x, y, position, routeVector);
                        //            flag2 = FasterLineSegmentIntersection(x, y, position, nextRoute);
                        //            //if(FasterLineSegmentIntersection(x, y, position, routeVector) || FasterLineSegmentIntersection(x, y, position, position + nextPosition))
                        //            //{
                        //            //    break;
                        //            //    flag = true;
                        //            //}

                        //            if (flag2)
                        //            {
                        //                break;


                        //            }
                        //        }
                        //    }
                        //} while (flag2);
                        if(checkNextLineCollision(ref forward, ref distanceToNextTurn) == -1 ){
                            return waypoints;
                        }
                        nextPosition = generateNextPosition(sumOfDistances,ref  traveledDistance);
                        traveledDistance = 0;


                        //float rotAngle = Random.Range(-180.0f, 180.0f);
                        //forward = Utilities.RotateVector(forward, rotAngle);
                        //nextPosition = generateNextPosition(sumOfDistances);
                        //traveledDistance = 0;


                        //float rotAngle = Random.Range(-180, 180);
                        //forward = Utilities.RotateVector(forward, rotAngle);
                        //distanceToNextTurn = Random.Range(3, 8);
                        //routeVector = position + distanceToNextTurn * forward;
                        //traveledDistance = 0;

                        break;
                    }
                }
            }
            //traveledDistance = traveledDistance + sampledDistance;


            // nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
            waypoints.Add(nextPosition);
            //Debug.Log(nextPosition);
            if (traveledDistance >= distanceToNextTurn)
            {
                //bool flag2 = true;
                //do
                //{
                //    float rotAngle = Random.Range(0, 360);
                //    forward = Utilities.RotateVector(forward, rotAngle);
                //    distanceToNextTurn = Random.Range(3, 8);
                //    Vector2 nextRoute = position + (distanceToNextTurn * forward);
                //    foreach (var p in vePolygons)
                //    {
                //        if (vePolygons.Count == 1)
                //        {
                //            return waypoints;
                //        }
                //        for (int i = 0; i < p.Count; i++)
                //        {
                //            Vector2 x = p[i];
                //            Vector2 y = p[(i + 1) % p.Count];
                //            //flag = FasterLineSegmentIntersection(x, y, position, routeVector);
                //            flag2 = FasterLineSegmentIntersection(x, y, position, nextRoute);
                //            //if(FasterLineSegmentIntersection(x, y, position, routeVector) || FasterLineSegmentIntersection(x, y, position, position + nextPosition))
                //            //{
                //            //    break;
                //            //    flag = true;
                //            //}

                //            if (flag2)
                //            {
                //                break;


                //            }
                //        }
                //    }
                //} while (flag2);
                if (checkNextLineCollision(ref forward, ref distanceToNextTurn) == -1)
                {
                    return waypoints;
                }
                nextPosition = generateNextPosition(sumOfDistances, ref traveledDistance);
                traveledDistance = 0;


                //float rotAngle = Random.Range(-180.0f, 180.0f);
                //nextForward = Utilities.RotateVector(forward, rotAngle);
                //traveledDistance = 0;
                //distanceToNextTurn = Random.Range(3, 8);
            }
            else
            {
                nextForward = (nextPosition - position).normalized;
            }
            position = nextPosition;
            forward = nextForward;
            sumOfDistances += sampledDistance;
            sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
            alternator *= -1;

            if (sumOfDistances >= targetDist)
            {
                finished = true;
            }
        }

        return waypoints;

        int checkNextLineCollision(ref Vector2 forwardVec, ref float distanceToNextVector)
        {
            bool flag2 = true;
            do
            {
                float rotAngle = Random.Range(0, 360);
                forwardVec = Utilities.RotateVector(forwardVec, rotAngle);
                distanceToNextVector = 6;
                Vector2 nextRoute = position + (distanceToNextVector * forwardVec);
                foreach (var p in vePolygons)
                {
                    if (vePolygons.Count == 1)
                    {
                        return -1;
                    }
                    for (int i = 0; i < p.Count; i++)
                    {
                        Vector2 x = p[i];
                        Vector2 y = p[(i + 1) % p.Count];
                        //flag = FasterLineSegmentIntersection(x, y, position, routeVector);
                        flag2 = FasterLineSegmentIntersection(x, y, position, nextRoute);
                        //if(FasterLineSegmentIntersection(x, y, position, routeVector) || FasterLineSegmentIntersection(x, y, position, position + nextPosition))
                        //{
                        //    break;
                        //    flag = true;
                        //}

                        if (flag2)
                        {
                            break;


                        }
                    }
                }
            } while (flag2);
            return 1;
        }

        Vector2 generateNextPosition(float sumOfDistance, ref float traveledDist)
        {

            sampledDistance = SampleDistribution(pathSeed.distanceDistribution);

            if (sampledDistance + sumOfDistance >= targetDist)
            {
                finished = true;
                sampledDistance = targetDist - sumOfDistance;
            }

            sampledRotation = SampleDistribution(pathSeed.angleDistribution);

            if (pathSeed.angleDistribution.alternationType == AlternationType.Constant)
                sampledRotation *= alternator;
            traveledDist = traveledDist + sampledDistance;
            nextPosition = position + sampledDistance * forward;
            nextForward = Utilities.RotateVector(forward, sampledRotation).normalized;

            return nextPosition;
        }
    }

    //public static List<Vector2> GenerateInitialPathByPathSeedHJ(PathSeed pathSeed, out float sumOfDistances, out float sumOfRotations, int randomSeed)
    //{
    //    RANDOM_SEED = randomSeed;
    //    Vector2 initialPosition = new Vector2(0, 0);//Random.Range(-100,100),Random.Range(-100,100));
    //    Vector2 initialForward = new Vector2(0, 1);//along z axis
    //    // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
    //    // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
    //    List<Vector2> waypoints = new List<Vector2>(pathSeed.waypointCount);
    //    Vector2 position = initialPosition;
    //    Vector2 forward = initialForward.normalized;
    //    Vector2 nextPosition, nextForward;
    //    float sampledDistance, sampledRotation;
    //    sumOfDistances = 0;
    //    sumOfRotations = 0;
    //    //int alternator = 1;
    //    //add start point
    //    waypoints.Add(position);
    //    bool finished = false;

    //    var vePolygons = getVEPolygons();

    //    //vePolygons.Add(new List<Vector2> {
    //    //            new Vector2(-7.5f,7.5f),
    //    //            new Vector2(-7.5f,2.5f),
    //    //            new Vector2(-2.5f,2.5f),
    //    //            new Vector2(-2.5f,7.5f),
    //    //});
    //    //vePolygons.Add(new List<Vector2> {
    //    //            new Vector2(7.5f,7.5f),
    //    //            new Vector2(2.5f,7.5f),
    //    //            new Vector2(2.5f,2.5f),
    //    //            new Vector2(7.5f,2.5f),
    //    //});
    //    //vePolygons.Add(new List<Vector2> {
    //    //            new Vector2(7.5f,-7.5f),
    //    //            new Vector2(7.5f,-2.5f),
    //    //            new Vector2(2.5f,-2.5f),
    //    //            new Vector2(2.5f,-7.5f),
    //    //});
    //    //vePolygons.Add(new List<Vector2> {
    //    //            new Vector2(-7.5f,-7.5f),
    //    //            new Vector2(2.5f,-7.5f),
    //    //            new Vector2(-2.5f,-2.5f),
    //    //            new Vector2(-7.5f,-2.5f),
    //    //});
    //    for (; !finished;)
    //    {
    //        //if(waypoints.Count>=5)
    //        if (waypoints.Count >= pathSeed.waypointCount)
    //        {
    //            finished = true;
    //        }

    //        // sampledDistance = SampleDistribution(pathSeed.distanceDistribution);
    //        // if (sampledDistance + sumOfDistances >= targetDist)
    //        // {
    //        //    finished = true;
    //        //    sampledDistance = targetDist - sumOfDistances;
    //        // }
    //        // sampledRotation = SampleDistribution(pathSeed.angleDistribution);
    //        // if (pathSeed.angleDistribution.alternationType == AlternationType.Constant)
    //        //    sampledRotation *= alternator;
    //        // nextPosition = position + sampledDistance * forward;
    //        //nextPosition = GetRandomPositionWithinBounds(0, 20.0f, 0, 20.0f, 4, 8, position);
    //        nextPosition = GetRandomPositionWithinBounds(3, 8);

    //        bool flag = true;

    //        while (flag)
    //        {
    //            if(vePolygons.Count == 0)
    //            {
    //                flag = false;
    //            }
    //            foreach (var p in vePolygons)
    //            {
    //                if (vePolygons.Count >= 1)
    //                {
    //                    for (int i = 0; i < p.Count; i++)
    //                    {
    //                        Vector2 x = p[i];
    //                        Vector2 y = p[(i + 1) % p.Count];

    //                        flag = FasterLineSegmentIntersection(x, y, position, nextPosition);
    //                        if ((nextPosition - position).magnitude < 4)
    //                        {
    //                            flag = true;
    //                        }
    //                        if (flag)
    //                        {
    //                            break;
    //                        }
    //                    }
    //                    if (flag)
    //                    {
    //                        //nextPosition = GetRandomPositionWithinBounds(0, 20.0f, 0, 20.0f, 4, 8, position);
    //                        nextPosition = GetRandomPositionWithinBounds(3, 8);
    //                        break;
    //                    }

    //                }
    //                else
    //                {
    //                    flag = false;
    //                }

    //            }
    //        }
    //        nextForward = (nextPosition - position).normalized;
    //        // nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
    //        waypoints.Add(nextPosition);
    //        //Debug.Log(nextPosition);
    //        position = nextPosition;
    //        forward = nextForward;
    //        // sumOfDistances += sampledDistance;
    //        // sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
    //        //alternator *= -1;
    //    }

    //    return waypoints;
    //}


}



