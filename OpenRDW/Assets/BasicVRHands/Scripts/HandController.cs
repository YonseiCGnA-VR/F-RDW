using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using System.Collections.Generic;
using Valve.VR;

public class HandController : MonoBehaviour {

    private Animator animator;

    public SteamVR_Action_Boolean Trigger;
	private InputDevice device;
    private List<InputDevice> rightHandDevices;
    void Start () {
        animator = GetComponent<Animator>();

        rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        if (rightHandDevices.Count == 1)
        {
            device = rightHandDevices[0];
            Debug.Log(string.Format("Device name '{0}' with role '{1}'", device.name, device.role.ToString()));
        }
        else if (rightHandDevices.Count > 1)
        {
            Debug.Log("Found more than one left hand!");
        }
    }
	
	void Update () {
        //bool triggerValue;
        //device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerValue);
        if (Trigger.GetState(SteamVR_Input_Sources.RightHand))
        {
            animator.SetBool("isGrabbing", true);
        }
        else
        {
            animator.SetBool("isGrabbing", false);
        }



	}
}
