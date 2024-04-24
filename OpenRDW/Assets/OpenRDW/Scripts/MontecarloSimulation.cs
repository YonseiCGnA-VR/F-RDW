using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MontecarloSimulation : MonoBehaviour
{
    protected float boundaryRadius = 1.0f;

    public enum Distribution
    {
        Uniform,
        Normal
    }

    [HideInInspector]
    public GlobalConfiguration globalConfiguration;
    public MovementManager movementManager;
    public RedirectionManager redirectionManager;
    private List<Vector2> waypoints;
    private Sampler sampler;
    private float meanError;
    private float stdError;
    private Distribution distribution;
    private Vector2 Error;
    private float alpha;
    // Start is called before the first frame update
    void Start()
    {
        globalConfiguration = GetComponentInParent<GlobalConfiguration>();
        initializeMC();
    }

    public void initializeMC()
    {
        movementManager = globalConfiguration.redirectedAvatars[0].GetComponent<MovementManager>();
        redirectionManager = globalConfiguration.redirectedAvatars[0].GetComponent<RedirectionManager>();
        waypoints = movementManager.waypoints;
        meanError = globalConfiguration.meanError;
        stdError = globalConfiguration.stdError;
        distribution = globalConfiguration.distribution;
        switch (distribution)
        {
            case Distribution.Uniform:
                sampler = new UniformSampler(boundaryRadius);
                break;
            case Distribution.Normal:
                sampler = new NormalSampler(boundaryRadius, meanError, stdError);
                break;
            default:
                Debug.LogError("MonteCarlo Simulation Sampler Error ... " + distribution);
                break;
        }

        Error = new Vector2(0, 0);
        alpha = 0.95f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Vector2 getSimulationPositionDenceWaypoint(int predictSecond = 50)
    {
        int predictTerm = predictSecond;
        Vector2 currPosReal = Utilities.FlattenedPos2D( redirectionManager.currPosReal);
        Vector2 currDirReal = Utilities.FlattenedDir2D( redirectionManager.currDirReal);
        Vector2 futurePositionDisplacement = new Vector2(0, 0);
        futurePositionDisplacement = getWayPoints(predictTerm);


        var normalDistribution = sampler.sampling();
        Error = (Error * alpha) + normalDistribution * (1 - alpha);
        Vector2 simulatedVirtualPosition = futurePositionDisplacement + Error;
        //Vector2 simulatedVirtualPosition = futurePositionDisplacement;

        return simulatedVirtualPosition;
    }

    public Vector3 getSimulatedProb(float correctRate, float minCorrectProb, float actionThreshold, int predictionTerm)
    {
        Vector3 result = new Vector3(0, 0, 0); // result[0] : left Prob , result[1] : forward Prob, result[2] : right Prob

        int predictTerm = predictionTerm * 50;
        Vector2 currPos = Utilities.FlattenedPos2D(redirectionManager.currPos);
        Vector2 currDir = Utilities.FlattenedDir2D(redirectionManager.currDir);
        Vector2 futurePos = getWayPoints(predictTerm);

        float angle = Utilities.GetSignedAngle(currDir, (futurePos - currPos).normalized);
        int actionIndex = -1;
        if(angle > actionThreshold)
        {
            // User move Right
            actionIndex = 2;
            

        }else if (angle < -actionThreshold)
        {
            // User move Left
            actionIndex = 0;
        }
        else
        {
            // User move forward
            actionIndex = 1;
        }
        Debug.Log("future Action : " + actionIndex);
        float modelCorrectProb = Random.Range(0.0f, 1.0f);

        float majorProb = Random.Range(minCorrectProb, 1.0f);
        float remainder = 1 - majorProb;
        float[] Prob = new float[2];
        Prob[0] = Random.Range(0.0f, remainder);
        Prob[1] = remainder - Prob[0];

        if (modelCorrectProb > correctRate)
        {
            // wrong Prediction
            Debug.Log("Wrong movement prediction simul");
            switch (actionIndex)
            {
                case 0:
                case 2:
                    // User will turn left, but wrong Prediction
                    result[1] = majorProb;
                    result[0] = Prob[0];
                    result[2] = Prob[1];
                    break;
                case 1:
                    result[0] = Random.Range(0.4f, 1.0f);
                    result[2] = Random.Range(0.0f, 1 - result[0]);
                    result[1] = 1 - result[0] - result[2];
                    break;
                default:
                    Debug.LogError("Error in Montecarlo Prob Simulation");
                    break;
            }
        }
        else
        {
            // correct Prediction
            Debug.Log("Correct movement prediction simul");


            int j = 0;
            for(int i = 0; i < 3; i++)
            {
                if(i == actionIndex)
                {
                    result[i] = majorProb;
                }
                else
                {
                    if(j <= 1)
                    {
                        result[i] = Prob[j];
                        j++;
                    }

                }
            }
            

        }

        return result;

    }


    public Vector2 getWayPoints(int predictTerm)
    {
        Vector2 futurePositionDisplacement;
        if (movementManager.waypointIterator + predictTerm < waypoints.Count)
        {
            futurePositionDisplacement = waypoints[movementManager.waypointIterator + predictTerm] - waypoints[movementManager.waypointIterator];
        }
        else
        {
            futurePositionDisplacement = waypoints[waypoints.Count - 1] - waypoints[movementManager.waypointIterator];

        }
        return futurePositionDisplacement;
    }


}

public abstract class Sampler
{
    protected float mean = 0.0f;
    protected float std = 0.0f;
    public float boundaryRadius;

    public Sampler(float boundaryRadius , float mean = 0, float std = 1)
    {
        this.boundaryRadius = boundaryRadius;
        this.mean = mean;
        this.std = std;
    }
    public virtual Vector2 sampling()
    {
        return new Vector2(-500, -500);
    }

    public float SampleUniform(float min, float max)
    {
        Random.seed = System.DateTime.Now.Minute * System.DateTime.Now.Second;
        return Random.Range(min, max);
    }

    public float SampleNormal(float mu = 0, float std = 1, float min = float.MinValue, float max = float.MaxValue)
    {
        Random.seed = System.DateTime.Now.Minute * System.DateTime.Now.Second;

        // From: http://stackoverflow.com/questions/218060/random-gaussian-variables
        float r1 = Random.value;
        float r2 = Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(r1)) * Mathf.Sin(2.0f * Mathf.PI * r2); // Random Normal(0, 1)
        float randNormal = mu + randStdNormal * std;
        return Mathf.Max(Mathf.Min(randNormal, max), min);
    }
}

public class UniformSampler : Sampler
{
    public UniformSampler(float boundaryRadius) : base(boundaryRadius,0, 0)
    {

    }

    public override Vector2 sampling()
    {
        Vector2 sampledPosition = new Vector2(0, 0);
        do
        {
            sampledPosition = new Vector2(SampleUniform(-boundaryRadius, boundaryRadius), SampleUniform(- boundaryRadius, boundaryRadius));
        } while (sampledPosition.magnitude > boundaryRadius);

        return sampledPosition;
    }
}

public class NormalSampler : Sampler
{
    public NormalSampler(float boundaryRadius, float mean = 0, float std = 1) : base(boundaryRadius, mean, std)
    {

    }
    public override Vector2 sampling()
    {
        Vector2 sampledPosition = new Vector2(0, 0);


        float distance = SampleNormal(this.mean, this.std); // sampling distance
        float angle = SampleUniform(0, 360);
        // distance diffrence mean : 45cm, std : 35cm need to be fixed ...
        sampledPosition = Utilities.RotateVector(new Vector2(0, distance), angle);


        return sampledPosition;
    }


}


