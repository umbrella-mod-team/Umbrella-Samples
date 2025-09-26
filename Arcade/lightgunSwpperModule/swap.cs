using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WIGU;

public class lightgunSwapperModule : MonoBehaviour
{
    private GameObject gunObject;
    private GameObject[] gunClones;
    private GameObject[] triggerClones;
    private GameObject[] originalArms;
    private Transform[] triggers;
    private bool isInitialized;
    private bool isLeftHand;

    private void Start()
    {
        this.gunObject = lightgunSwapperModule.FindChild(((Component)this).gameObject, "Gun");
        this.gunClones = new GameObject[3];
        this.triggerClones = new GameObject[3];
        this.originalArms = new GameObject[3];
        this.triggers = new Transform[3];
    }

    private void Update()
    {
        if (PlayerControllerHelper.IsObjectSelectedOrGrabbed(((Component)this).gameObject) && (OVRInput.GetDown((OVRInput.Button)1, (OVRInput.Controller)int.MinValue) || OVRInput.GetDown((OVRInput.Button)4, (OVRInput.Controller)int.MinValue) || Input.GetKeyDown((KeyCode)32 /*0x20*/) && !Input.GetKey((KeyCode)306)) && this.gunObject != null)
        {
            if (this.isInitialized)
            {
                this.StartCoroutine(this.AttachGun());
            }
            else
            {
                this.ResetGuns();
                this.isInitialized = true;
            }
        }
        if ((OVRInput.Get((OVRInput.Button)32768 /*0x8000*/, (OVRInput.Controller)int.MinValue) && OVRInput.Get((OVRInput.Button)8388608 /*0x800000*/, (OVRInput.Controller)int.MinValue) || Input.GetKey((KeyCode)306) && Input.GetKeyDown((KeyCode)32 /*0x20*/)) && this.isInitialized && this.gunObject != null && !this.gunObject.activeSelf)
            this.ResetGuns();
        if (!this.isInitialized || this.isLeftHand || this.gunObject == null || this.gunObject.activeSelf || !OVRInput.GetDown((OVRInput.RawButton)268435456 /*0x10000000*/, (OVRInput.Controller)int.MinValue))
            return;
        this.StartCoroutine(this.AttachGun());
    }

    private void ResetGuns()
    {
        if (this.isInitialized)
        {
            for (int index = 0; index < 3; ++index)
            {
                if (this.originalArms[index] != null && this.originalArms[index].name != "LightgunController")
                    this.originalArms[index].GetComponent<MeshRenderer>().enabled = true;
                if (this.triggers[index] != null)
                    this.triggers[index].GetComponent<MeshRenderer>().enabled = true;
                if (this.gunClones[index] != null)
                    Destroy(this.gunClones[index]);
                if (this.triggerClones[index] != null)
                    Destroy(this.triggerClones[index]);
            }
        }
        this.gunObject.SetActive(true);
        this.originalArms[0] = GameObject.Find("Head/LightgunController");
        this.originalArms[1] = GameObject.Find("Hands/HandRight/r_hand_skeletal_lowres/hands:hands_geom/LightgunController");
        this.originalArms[2] = GameObject.Find("Hands/HandLeft/l_hand_skeletal_lowres/hands:hands_geom/LightgunController");
        this.isLeftHand = false;
    }

    private IEnumerator AttachGun()
    {
        yield return new WaitForSeconds(0.2f);
        this.LoadGuns();
        this.CloneGuns();
        this.HideOriginals();
    }

    private void LoadGuns()
    {
        for (int index = 0; index < 3; ++index)
        {
            if (this.originalArms[index] != null && this.originalArms[index].name == "LightgunController")
            {
                GameObject gameObject = lightgunSwapperModule.FindChild(this.originalArms[index], "Aim");
                if (gameObject != null && gameObject.transform.parent != null)
                {
                    this.originalArms[index] = gameObject.transform.parent.gameObject;
                    this.triggers[index] = this.originalArms[index].transform.Find("Trigger");
                }
            }
        }
    }

    private void CloneGuns()
    {
        for (int index = 0; index < 3; ++index)
        {
            if (this.originalArms[index] != null && this.gunClones[index] == null && this.originalArms[index].name != "LightgunController")
            {
                this.gunClones[index] = Instantiate<GameObject>(this.gunObject);
                this.gunClones[index].transform.SetParent(this.gunObject.transform);
                Transform gripTransform = this.originalArms[index].transform.Find("Grip");
                Transform pivotTransform = this.gunClones[index].transform.Find("Pivot");
                if (gripTransform != null && pivotTransform != null)
                {
                    Vector3 offset = pivotTransform.position - this.gunClones[index].transform.position;
                    this.gunClones[index].transform.position = gripTransform.position - offset;
                    this.gunClones[index].transform.SetParent(this.originalArms[index].transform);
                    this.gunClones[index].transform.localRotation = pivotTransform.localRotation;
                    this.triggerClones[index] = this.gunClones[index].transform.Find("Aim").gameObject;
                }
                this.gunClones[index].SetActive(true);
                this.gunClones[index].name = "Gun";
                this.gunObject.SetActive(false);
            }
        }
    }

    private void HideOriginals()
    {
        for (int index = 0; index < 3; ++index)
        {
            if (this.originalArms[index] != null && this.originalArms[index].name != "LightgunController")
                this.originalArms[index].GetComponent<MeshRenderer>().enabled = false;
            if (this.triggers[index] != null)
            {
                this.triggers[index].GetComponent<MeshRenderer>().enabled = false;
                if (this.triggerClones[index] != null)
                    this.triggerClones[index].transform.SetParent(this.triggers[index]);
            }
        }
    }

    private static GameObject FindChild(GameObject parent, string childName)
    {
        Transform childTransform = parent.transform.Find(childName);
        if (childTransform != null)
            return childTransform.gameObject;
        Stack<Transform> transformStack = new Stack<Transform>();
        transformStack.Push(parent.transform);
        while (transformStack.Count > 0)
        {
            foreach (Transform subChild in transformStack.Pop())
            {
                if (subChild.name == childName)
                    return subChild.gameObject;
                transformStack.Push(subChild);
            }
        }
        return null;
    }
}
