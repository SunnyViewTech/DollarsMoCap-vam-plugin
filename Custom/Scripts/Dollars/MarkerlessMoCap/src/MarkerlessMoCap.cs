using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;
using UnityEngine.Events;


namespace Dollars
{
    public class MarkerlessMoCap : MVRScript
    {
        private const string title1 = "Dollars Markerless MoCap";
        private const string title2 = "Virt-A-Mate plugin";
        private const string aboutText = "\n\n<b><size=36><color=#3366CC>" + title1 + "</color></size></b>\n\n" + title2;// + "\n<size=20>" + pluginDate + "</size>";
        private UIDynamicTextField dtfAbout;

        private const string titleweb = "\nDownload Dollars Markerless MoCap at,\n\nen: www.dollarsmocap.com\ncn: www.sunnyview.tech/dollarsmarkerless";
        private const string webText = "<size=26>" + titleweb + "</size>";
        private UIDynamicTextField dtfWebsite;

        private UIDynamicTextField portTextField;
        private int port = 39739;

        private UIDynamicSlider smoothingSlider;
        private JSONStorableFloat smoothingFloat;
        private float BoneFilter = 0.7f;

        private UIDynamicButton connectButton;

        private UIDynamicToggle faceCapButton;
        public JSONStorableBool StorableIsFaceCapturing;

        private Dollars.OSC osc;
        private bool connected = false;
        private bool received = false;

        private FreeControllerV3[] jointControls;

        private float srcHipHeight = 0.8788f;
        private float scale;
        private Vector3 initHipPos;
        private Vector3 srcInitHipPos;
        private Quaternion selfInitRotation;
        private Quaternion[] srcJointsInitRotation = new Quaternion[21];

        private Vector3[] poss = new Vector3[21];
        private Quaternion[] rots = new Quaternion[21];
        private Quaternion[] boneRotFilter = new Quaternion[21];
        private float[] fingers = new float[12];

        bool FaceCapturing;
        private float[] bsvalues = new float[7];
        private float[] CurrentBSValues = new float[7];

        Dictionary<string, int> boneids = new Dictionary<string, int>
        {
            ["Hips"]            = 0,
            ["LeftUpperLeg"]    = 1,
            ["RightUpperLeg"]   = 2,
            ["LeftLowerLeg"]    = 3,
            ["RightLowerLeg"]   = 4,
            ["LeftFoot"]        = 5,
            ["RightFoot"]       = 6,
            ["LeftToes"]        = 7,
            ["RightToes"]       = 8,
            ["Spine"]           = 9,
            ["Chest"]           = 10,
            ["Neck"]            = 11,
            ["Head"]            = 12,
            ["LeftShoulder"]    = 13,
            ["RightShoulder"]   = 14,
            ["LeftUpperArm"]    = 15,
            ["RightUpperArm"]   = 16,
            ["LeftLowerArm"]    = 17,
            ["RightLowerArm"]   = 18,
            ["LeftHand"]        = 19,
            ["RightHand"]       = 20,
        };

        Dictionary<string, int> controlids = new Dictionary<string, int>
        {
            ["hip"]         = 0,
            ["lThigh"]      = 1,
            ["rThigh"]      = 2,
            ["lKnee"]       = 3,
            ["rKnee"]       = 4,
            ["lFoot"]       = 5,
            ["rFoot"]       = 6,
            ["lToe"]        = 7,
            ["rToe"]        = 8,
            ["abdomen2"]    = 9,
            ["chest"]       = 10,
            ["neck"]        = 11,
            ["head"]        = 12,
            ["lShoulder"]   = 13,
            ["rShoulder"]   = 14,
            ["lArm"]        = 15,
            ["rArm"]        = 16,
            ["lElbow"]      = 17,
            ["rElbow"]      = 18,
            ["lHand"]       = 19,
            ["rHand"]       = 20,
        };

        Dictionary<string, int> arkitids = new Dictionary<string, int>
        {
            ["jawOpen"] = 0,
            ["eyeBlinkLeft"] = 1,
            ["eyeBlinkRight"] = 2,
            ["mouthSmileLeft"] = 3,
            ["mouthSmileRight"] = 4,
            ["mouthDimpleLeft"] = 5,
            ["mouthDimpleRight"] = 6,
        };

        Dictionary<int, string> blendshapeids = new Dictionary<int, string>
        {
            { 0, "Mouth Open" },
            { 1, "Eyes Closed Left" },
            { 2, "Eyes Closed Right" },
            { 3, "Mouth Smile Simple Left" },
            { 4, "Mouth Smile Simple Right" },
            { 5, "MouthDimple_L" },
            { 6, "MouthDimple_R" },
        };

        void OnReceiveBone(OscMessage message)
        {
            received = true;
            int index = 0;
            string x = message.GetString(0);
            float px = message.GetFloat(1);
            float py = message.GetFloat(2);
            float pz = message.GetFloat(3);
            float rx = message.GetFloat(4);
            float ry = message.GetFloat(5);
            float rz = message.GetFloat(6);
            float rw = message.GetFloat(7);
            if (boneids.ContainsKey(x))
            {
                index = boneids[x];
                poss[index].x = px;
                poss[index].y = py;
                poss[index].z = pz;
                rots[index].x = rx;
                rots[index].y = ry;
                rots[index].z = rz;
                rots[index].w = rw;
            }
        }
        private JSONStorable geometry;
        private DAZCharacterSelector character;
        private GenerateDAZMorphsControlUI morphControl;

        void OnReceiveFinger(OscMessage message)
        {
            received = true;
            int index = 0;
            string x = message.GetString(0);

            for (int i = 0; i < 12; i++)
            {
                fingers[i] = message.GetFloat(i + 1);
            }
        }

        void OnReceiveBlendshape(OscMessage message)
        {
            string x = message.GetString(0);

            if (arkitids.ContainsKey(x))
            {
                bsvalues[arkitids[x]] = message.GetFloat(1);
            }
        }

        private void ClearUI()
        {
            if (dtfAbout != null)
            {
                RemoveTextField(dtfAbout);
            }
            if (dtfWebsite != null)
            {
                RemoveTextField(dtfWebsite);
            }
            if (connectButton != null)
            {
                RemoveButton(connectButton);
            }
            if (smoothingSlider != null)
            {
                RemoveSlider(smoothingSlider);
            }
            if (portTextField != null)
            {
                RemoveTextField(portTextField);
            }
            if (faceCapButton != null)
            {
                RemoveToggle(faceCapButton);
            }
        }

        private void OnSliderChanged()
        {
            BoneFilter = smoothingFloat.val;
        }

        public JSONStorableFloat AddSlider(string name, float defaultVal, float min, float max, UnityAction<float> onChange = null, bool rightSide = false)
        {
            JSONStorableFloat storableFloat = new JSONStorableFloat(name, defaultVal, min, max, true, true);
            storableFloat.storeType = JSONStorableParam.StoreType.Full;
            smoothingSlider = this.CreateSlider(storableFloat, rightSide);
            RegisterFloat(storableFloat);

            if (onChange != null)
            {
                smoothingSlider.slider.onValueChanged.AddListener(onChange);
            }

            return storableFloat;
        }

        private void RenderUI()
        {
            JSONStorableString about = new JSONStorableString("About", aboutText);
            dtfAbout = CreateTextField(about, false);
            dtfAbout.UItext.alignment = TextAnchor.MiddleCenter;
            dtfAbout.UItext.supportRichText = true;
            dtfAbout.UItext.fontSize = 26;
            dtfAbout.UItext.color = Color.black;
            dtfAbout.height = 200;

            JSONStorableString web = new JSONStorableString("web", webText);
            dtfWebsite = CreateTextField(web, false);
            dtfWebsite.UItext.supportRichText = true;
            dtfWebsite.UItext.fontSize = 26;
            dtfWebsite.UItext.color = Color.black;
            dtfWebsite.height = 177;

            portTextField = CreateTextField(new JSONStorableString("port", port.ToString()), true);
            portTextField.backgroundImage.color = Color.white;
            portTextField.height = 30;
            var targetValuesInput = portTextField.gameObject.AddComponent<InputField>();
            targetValuesInput.textComponent = portTextField.UItext;
            targetValuesInput.textComponent.fontSize = 80;
            targetValuesInput.text = port.ToString();
            targetValuesInput.image = portTextField.backgroundImage;
            targetValuesInput.onValueChanged.AddListener((string value) =>
            {
                port = int.Parse(value);
            });


            connectButton = CreateButton("Connect", true);
            connectButton.buttonColor = Color.green;
            connectButton.textColor = Color.black;

            if (connected)
            {
                connectButton.label = "Disconnect";
                connectButton.buttonColor = Color.red;
                connectButton.textColor = Color.white;
            }
            connectButton.button.onClick.AddListener(Connect);

            smoothingFloat = AddSlider("Smoothing", BoneFilter, 0f, 1f, delegate { OnSliderChanged(); }, true);

            StorableIsFaceCapturing = new JSONStorableBool("Face Capture (alpha)", FaceCapturing, (bool value) => {
                if (value) { StartFaceCap(); }
                else { StopFaceCap(); }
            });
            faceCapButton = CreateToggle(StorableIsFaceCapturing, true);
            faceCapButton.height = 75;

        }

        private static JSONStorable personEyelids;
        bool autoblink;

        private void StartFaceCap()
        {
            FaceCapturing = true;
            autoblink = personEyelids.GetBoolParamValue("blinkEnabled");
            personEyelids.SetBoolParamValue("blinkEnabled", false);
        }

        private void StopFaceCap()
        {
            FaceCapturing = false;
            personEyelids.SetBoolParamValue("blinkEnabled", autoblink);
        }

        public override void Init()
        {
            srcInitHipPos = new Vector3(0f, srcHipHeight, 0f);
            geometry = containingAtom.GetStorableByID("geometry");
            character = geometry as DAZCharacterSelector;
            morphControl = character.morphsControlUI;
            RenderUI();

            try
            {
                if (containingAtom.type != "Person")
                {
                    SuperController.LogError($"This plugin can only be used with a 'Person' atom.");
                    return;
                }

                jointControls = new FreeControllerV3[21];

                string key;

                var controllers = containingAtom.GetComponentsInChildren<FreeControllerV3>(true);
                foreach (var control in controllers)
                {
                    if (control == null || control.followWhenOff == null || control.control == null)
                        continue;

                    key = control.name.Substring(0, control.name.Length - 7);
                    if (controlids.ContainsKey(key))
                    {
                        jointControls[controlids[key]] = control;
                    }
                }
            }
            catch (System.Exception e)
            {
                SuperController.LogError("Exception caught in Init(): " + e);
            }
        }

        Quaternion rot;
        Vector3 pos, posFilter;

        void Update()
        {
            osc.Update();
            morphControl.GetMorphByDisplayName("Left Thumb Bend").morphValue = fingers[0];
            morphControl.GetMorphByDisplayName("Left Index Finger Bend").morphValue = fingers[1];
            morphControl.GetMorphByDisplayName("Left Mid Finger Bend").morphValue = fingers[2];
            morphControl.GetMorphByDisplayName("Left Ring Finger Bend").morphValue = fingers[3];
            morphControl.GetMorphByDisplayName("Left Pinky Finger Bend").morphValue = fingers[4];
            morphControl.GetMorphByDisplayName("Left Fingers In-Out").morphValue = fingers[5];
            morphControl.GetMorphByDisplayName("Right Thumb Bend").morphValue = fingers[6];
            morphControl.GetMorphByDisplayName("Right Index Finger Bend").morphValue = fingers[7];
            morphControl.GetMorphByDisplayName("Right Mid Finger Bend").morphValue = fingers[8];
            morphControl.GetMorphByDisplayName("Right Ring Finger Bend").morphValue = fingers[9];
            morphControl.GetMorphByDisplayName("Right Pinky Finger Bend").morphValue = fingers[10];
            morphControl.GetMorphByDisplayName("Right Fingers In-Out").morphValue = fingers[11];

            if (received)
            {
                pos = selfInitRotation * ((poss[0] - srcInitHipPos) * scale) + initHipPos;
                jointControls[0].control.position = pos;

                for (int i = 0; i < 21; i++)
                {
                    rot = srcJointsInitRotation[i];
                    rot *= rots[i];
                    boneRotFilter[i] = Quaternion.Slerp(boneRotFilter[i], rot, 1.0f - BoneFilter);
                    jointControls[i].control.rotation = boneRotFilter[i];
                }
                received = false;
            }

            if (FaceCapturing)
            {
                for (int i = 0; i < arkitids.Count; i++)
                {
                    CurrentBSValues[i] = (CurrentBSValues[i] * BoneFilter) + bsvalues[i] * (1.0f - BoneFilter);
                    try
                    {
                        morphControl.GetMorphByDisplayName(blendshapeids[i]).morphValue = CurrentBSValues[i];
                    }
                    catch { }
                }
            }
        }
        void OnDisable()
        {
            osc.Close();
        }

        void OnDestroy()
        {
            osc.Close();
        }

        private void Calibrate()
        {
            jointControls[0].currentRotationState = FreeControllerV3.RotationState.Lock;
            jointControls[0].currentPositionState = FreeControllerV3.PositionState.Lock;

            for (int i = 1; i < 21; i++)
            {
                jointControls[i].currentRotationState = FreeControllerV3.RotationState.Lock;
                jointControls[i].currentPositionState = FreeControllerV3.PositionState.Off;
            }

            initHipPos = jointControls[0].control.position;
            for (int i = 0; i < 21; i++)
            {
                srcJointsInitRotation[i] = jointControls[i].control.rotation;
            }
            selfInitRotation = jointControls[0].control.transform.rotation;
            scale = jointControls[0].control.position.y / srcHipHeight;
        }

        private void Connect()
        {
            if (!connected)
            {
                Calibrate();
                osc = new OSC(port);
                osc.StartListen();
                osc.SetAddressHandler("/VMC/Ext/Bone/Pos", OnReceiveBone);
                osc.SetAddressHandler("/VMC/Ext/Finger", OnReceiveFinger);
                osc.SetAddressHandler("/VMC/Ext/Blend/Val", OnReceiveBlendshape);
                connected = true;
            }
            else
            {
                osc.Close();
                connected = false;
            }
            ClearUI();
            RenderUI();
        }
    }
}