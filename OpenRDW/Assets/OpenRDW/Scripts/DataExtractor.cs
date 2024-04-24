using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;
using ViveSR.anipal.Eye;
using UnityEngine.Assertions;
using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;
using System.Text;
using System.Linq;
using movement;


public class DataExtractor : MonoBehaviour {
    private RedirectionManager redirectionManager;

    [HideInInspector]
    public MovementManager movementManager;
    private const float MOVEMENT_THRESHOLD = 0.2f;
    private const float ROTATION_THRESHOLD = 1.5f;
    public Transform head;
    StreamWriter fs = null;
    GameObject sranipal = null;
    bool eyeEnable = false;
    float xVelo;
    float zVelo;
    //Vector4[] 
    float headYaw;
    float headPitch;
    float bodyDir;
    float avgHeadYaw = 0.0f;
    float avgHeadPitch = 0.0f;
    int count;
    Vector3 currPos;
    Vector3 prevPos;
    float conversionXTarget = 0;
    float conversionZTarget = 0;
    string cmd = "testing";
    byte[] receiverBuff = new byte[8192];
    private const int DataLength = 50;
    private const int PredictLength = 125;
    [HideInInspector]
    public int predictActionNum = 3;
    public bool prediction = false; // mode select false : Data Extract for lstm model, true : send each frame data to model

    Queue<Vector3> posQueue;
    Queue<float> headYawQueue;
    Queue<float> headPitchQueue;
    Queue<float> bodyDirQueue;
    Queue<float> xVeloQueue;
    Queue<float> zVeloQueue;
    Queue<float> eyeYawQueue;
    Queue<float> eyePitchQueue;
    Queue<float> baseAngleQueue;
    float[] headYawArray = new float[DataLength];
    float[] headPitchArray = new float[DataLength];
    float[] bodyDirArray = new float[DataLength];
    float[] xVeloArray = new float[DataLength];
    float[] zVeloArray = new float[DataLength];
    float[] eyeYawArray = new float[DataLength];
    float[] eyePitchArray = new float[DataLength];
    float[] baseAngleArray = new float[DataLength];
    bool flag = false;
    private int frameNumber;

    private static EyeData eyeData = new EyeData();
    private bool eye_callback_registered = false;
    public int LengthOfRay = 25;

    float[] predictProb = new float[3];
    float[] predictF = new float[2];

    bool isPredictReady = false;
    public int sendingFrameTerm = 1;

    TcpClient client;
    string serverIP = "127.0.0.1";
    int port = 8000;
    byte[] receivedBuffer;
    StreamReader reader;
    bool socketReady = false;
    NetworkStream stream;

    Socket sock;

    SocketController socketController;
    private Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;

    // Start is called before the first frame update
    [SerializeField] private LineRenderer GazeRayRenderer;
    void Start()
    {

        Debug.Log("frames for 2.5 sec : " + 2.5 / Time.deltaTime);

        headYawQueue = new Queue<float>(DataLength);
        headPitchQueue = new Queue<float>(DataLength);
        bodyDirQueue = new Queue<float>(DataLength);
        xVeloQueue = new Queue<float>(DataLength);
        zVeloQueue = new Queue<float>(DataLength);
        eyeYawQueue = new Queue<float>(DataLength);
        eyePitchQueue = new Queue<float>(DataLength);
        posQueue = new Queue<Vector3>(DataLength);
        baseAngleQueue = new Queue<float>(DataLength);
        frameNumber = 0;
        count = 0;

        if (SRanipal_Eye_Framework.Instance.EnableEye) {
            eyeEnable = true;
        }
        redirectionManager = GetComponentInParent<RedirectionManager>();
        movementManager = GetComponentInParent<MovementManager>();
        head = redirectionManager.headTransform;
        fs = new StreamWriter("User Data(" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ").txt");
        if (fs == null) {
            Debug.LogError("Data file error ...");
        }
        currPos = redirectionManager.currPos;
        prevPos = redirectionManager.prevPos;

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }

        Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;


        GazeDirectionCombinedLocal = new Vector3(0, 0, 0);
        GazeOriginCombinedLocal = new Vector3(0, 0, 0); 

        // (2) 서버에 연결
        if (prediction) {
            socketController = new SocketController(serverIP, port);

        }

    }

    


    // Update is called once per frame
    void Update() {
        float conversionHeadYaw = 0;
        float conversionHeadPitch = 0;
        float conversionBodyDir = 0;
        float conversionXVelo = 0;
        float conversionZVelo = 0;

        Vector3 popedPos = new Vector3(-500,-500, -500);

        frameNumber++;


       
        SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal);
        

        Vector3 deltaPos = redirectionManager.deltaPos;

            if (!redirectionManager.inReset && ((deltaPos.magnitude / redirectionManager.GetDeltaTime() > MOVEMENT_THRESHOLD) || Mathf.Abs(redirectionManager.deltaDir) >= ROTATION_THRESHOLD)){

                    currPos = redirectionManager.currPos;
                    headYaw = head.transform.eulerAngles.y;
                    if (head.transform.eulerAngles.x > 180) {
                        headPitch = head.transform.eulerAngles.x - 270;
                    }
                    else {
                        headPitch = head.transform.eulerAngles.x + 360 - 270;
                    }

                    float diffYaw = Utilities.GetSignedAngle(Utilities.FlattenedDir3D(deltaPos), Utilities.FlattenedDir3D(head.transform.forward));
                    float baseAngle = Utilities.GetSignedAngle(new Vector3(0, 0, 1), Utilities.FlattenedDir3D((deltaPos)));




                    conversionHeadYaw = diffYaw;
                    conversionHeadPitch = headPitch;
                    Vector3 conversionVelo = Quaternion.AngleAxis(-baseAngle, new Vector3(0, 1, 0)) * deltaPos;
                    conversionXVelo = conversionVelo.x;
                    conversionZVelo = conversionVelo.z;
                    conversionBodyDir = baseAngle;

                    if (!prediction) {
                        // Extract Data Set for Learning LSTM
                     
                        fs.WriteLine(conversionHeadYaw + ", " +  conversionHeadPitch + ", " + conversionBodyDir + ", " + GazeDirectionCombinedLocal.x + ", " + GazeDirectionCombinedLocal.y + ", " + conversionXVelo + ", " +
                            conversionZVelo + ", " + redirectionManager.currPos.x + ", " + redirectionManager.currPos.z);
                    }
                    else {
                        // gather Data for Predicting User's next movements and send to LSTM model

                        cmd = conversionHeadYaw + "," + conversionHeadPitch + "," + conversionBodyDir + "," + GazeDirectionCombinedLocal.x + "," + GazeDirectionCombinedLocal.y + "," + conversionXVelo + "," +
                            conversionZVelo;
                        //Debug.Log("cmd : " + cmd);
                        socketController.send(cmd);

                        string masterOut = socketController.receive();


                        if (masterOut == "0") {
                            isPredictReady = false;
                        }
                        else {
                            string[] predict = masterOut.Split(',');

                            for (int i = 0; i < 2; i++)
                        {
                            predictF[i] = float.Parse(predict[i]);
                        }
                        isPredictReady = true;
                        
                        }

                    }
                    prevPos = currPos;

            }

        

        
    }
    public bool getPrediction(ref float[] prob) {
        if (isPredictReady) {
            Array.Copy(predictF, prob, 2);
            return true;
        }
        return false;

    }

    public void OnDestroy() {
        fs.Close();
    }


    private static void EyeCallback(ref EyeData eye_data) {
        eyeData = eye_data;
    }


}

public abstract class BaseSocketController
{
    protected string serverIP = "127.0.0.1";
    protected int port = 8000;
    protected byte[] receiverBuff = new byte[8192];
    protected Socket sock;


    public BaseSocketController(string IP, int port)
    {
        this.serverIP = IP;
        this.port = port;
    }
    public BaseSocketController() : this("127.0.0.1", 8000)
    {
    }



    public abstract void connect();

    public abstract string receive();

    public abstract void send(string message);
}

class SocketController : BaseSocketController
{
    public SocketController(string IP, int port) : base(IP, port)
    {
        this.connect();
    }

    public SocketController() : base("127.0.0.1", 8000)
    {
        this.connect();
    }

    public override void connect()
    {

        string cmd = "testing";
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        var ep = new IPEndPoint(IPAddress.Parse(serverIP), port);
        sock.Connect(ep);

        byte[] buff = Encoding.UTF8.GetBytes(cmd);

        sock.Send(buff, SocketFlags.None);

        int n = sock.Receive(receiverBuff);
        string data = Encoding.UTF8.GetString(receiverBuff, 0, n);
        Debug.Log("receive : " + data);
    }

    public override string receive()
    {
        int n = sock.Receive(receiverBuff);
        string masterOut = Encoding.UTF8.GetString(receiverBuff, 0, n);
        return masterOut;
    }

    public override void send(string message)
    {
        byte[] buff = Encoding.UTF8.GetBytes(message);

        sock.Send(buff, SocketFlags.None);
    }
}


