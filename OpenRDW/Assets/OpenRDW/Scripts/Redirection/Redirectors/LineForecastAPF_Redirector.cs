using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineForecastAPF_Redirector : PredictAPF_Redirector
{
    float alpha = 0.5f;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
        float scale = 1.0f;
        Vector2 boundaryStartPoint = new Vector2(0, 0);
        Vector2 boundaryEndPoint = new Vector2(0, 0);

        Vector2 currentDirection = Utilities.FlattenedDir2D(redirectionManager.currDirReal);
        futurePosition = new Vector3(0, 1, 0) * scale;

        Angle = Utilities.CalculateAngle(new Vector3(0, 0, 1), redirectionManager.currDirReal);
        rotatedFuturePos = Utilities.RotateVector(futurePosition, Angle);
        Vector2 futurePosVecReal = rotatedFuturePos + currPosReal;

        GetRepulsiveForceAndNegativeGradient(obstaclePolygons, currPosReal, trackingSpacePoints, out currRf, out currNg);
        GetRepulsiveForceAndNegativeGradient(obstaclePolygons, futurePosVecReal, trackingSpacePoints, out futureRf, out futureNg);

        this.UpdateTotalForcePointer(currNg, futureNg, futurePosition, totalNegativeGradient);

        totalNegativeGradient = ((currNg * (1 - alpha)) + (futureNg * alpha)).normalized;

        ApplyRedirectionByNegativeGradient(totalNegativeGradient);
    }
}
