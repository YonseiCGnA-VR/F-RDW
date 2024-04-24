using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using Valve.VR;

public class HeadFollower : MonoBehaviour {        
    private RedirectionManager redirectionManager;
    private MovementManager movementManager;

    [HideInInspector]
    public bool ifVisible;

    [HideInInspector]
    public GameObject avatar;//avatar for visualization

    [HideInInspector]
    public GameObject avatarRoot;//avatar root, control movement like translation and rotation, Avoid interference of action data

    private Vector3 prePos;
    private Animator animator;

    private GlobalConfiguration globalConfiguration;

    [HideInInspector]
    public int avatarId;

    public SteamVR_Action_Boolean Trigger;
    private InputDevice device;
    private List<InputDevice> rightHandDevices;
    private bool hasCreatedAvatar;//if already create the avatar visualization

    private void Awake()
    {
        redirectionManager = GetComponentInParent<RedirectionManager>();
        movementManager = GetComponentInParent<MovementManager>();
        globalConfiguration = GetComponentInParent<GlobalConfiguration>();
        ifVisible = false;        
    }

    public void CreateAvatarViualization() {
        if (hasCreatedAvatar)
            return;
        hasCreatedAvatar = true;
        avatarId = movementManager.avatarId;
        avatarRoot = globalConfiguration.CreateAvatar(transform, movementManager.avatarId);
        Debug.Log("HeadFollower's transform :" + transform);
        animator = avatarRoot.GetComponentInChildren<Animator>();
        avatar = animator.gameObject;
        avatar.layer = 8;
        ChangeLayersRecursively(avatarRoot.transform, "Avatar");
    }

    public void ChangeLayer(string name)
    {
        ChangeLayersRecursively(transform, name);
    }

    public void ChangeLayersRecursively(Transform trans, string name)
    {
        trans.gameObject.layer = LayerMask.NameToLayer(name);
        foreach (Transform child in trans)
        {
            ChangeLayersRecursively(child, name);
        }
    }

    // Use this for initialization
    void Start () {
        prePos = transform.position;
        Debug.Log("HeadFollower prepos :" + prePos);

        rightHandDevices= new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        if (rightHandDevices.Count == 1)
        {
            InputDevice device = rightHandDevices[0];
            Debug.Log(string.Format("Device name '{0}' with role '{1}'", device.name, device.role.ToString()));
        }
        else if (rightHandDevices.Count > 1)
        {
            Debug.Log("Found more than one left hand!");
        }

        //ObjectGenerator.generateObject(redirectionManager.currPos);
    }
	
    public void UpdateManually() {
        transform.position = redirectionManager.currPos;        
        if (redirectionManager.currDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(redirectionManager.currDir, Vector3.up);

        prePos = transform.position;
    }    

    //change the color of the avatar
    public void ChangeColor(Color color) {
        var newMaterial= new Material(Shader.Find("Standard"));
        newMaterial.color = color;
        foreach (var mr in avatar.GetComponentsInChildren<MeshRenderer>())
        {
            mr.material = newMaterial;            
        }
        foreach (var mr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            mr.material = newMaterial;
        }
    }
    public void SetAvatarBodyVisibility(bool ifVisible) {
        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
            mr.enabled = ifVisible;
        foreach (var sr in GetComponentsInChildren<SkinnedMeshRenderer>())
            sr.enabled = ifVisible;
    }

    public void OnTriggerStay(Collider other)
    {

        if (other.gameObject.CompareTag("Coin"))
        {

            bool triggerValue = false;
            if (Trigger.GetState(SteamVR_Input_Sources.RightHand))
            {
                Destroy(other.gameObject);
                ObjectGenerator.destroyObject();
                ObjectGenerator.generateObject(redirectionManager.currPos);

            }

        }
        
    }


}
