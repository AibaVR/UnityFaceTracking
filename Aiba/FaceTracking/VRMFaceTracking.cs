using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UniGLTF;
using UnityEngine;
using uOSC;
using VRM;

namespace Aiba.FaceTracking
{
    public class VRMFaceTracking : MonoBehaviour
    {
        private OscServer _oscServer = new OscServer();

        public Vector3 offset = Vector3.zero;

        public bool trackFace = true;
        public bool trackTransforms = true;

        public const string LeftEyeGroup = "Left Eye";
        public const string RightEyeGroup = "Right Eye";
        public const string MouthGroup = "Mouth";
        public const string JawGroup = "Mouth";
        public const string TongueGroup = "Tongue";
        public const string NoseGroup = "Tongue";
        public const string CheekGroup = "Cheek";

        public AnimationCurve leftEyeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve rightEyeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve mouthCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve jawCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve tongueCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve noseCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve cheekCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private readonly Dictionary<string, string> _allShapes = new Dictionary<string, string>
        {
            { "EyeBlinkLeft", LeftEyeGroup },
            { "EyeLookDownLeft", LeftEyeGroup },
            { "EyeLookInLeft", LeftEyeGroup },
            { "EyeLookOutLeft", LeftEyeGroup },
            { "EyeLookUpLeft", LeftEyeGroup },
            { "EyeSquintLeft", LeftEyeGroup },
            { "EyeWideLeft", LeftEyeGroup },
            { "EyeBlinkRight", RightEyeGroup },
            { "EyeLookDownRight", RightEyeGroup },
            { "EyeLookInRight", RightEyeGroup },
            { "EyeLookOutRight", RightEyeGroup },
            { "EyeLookUpRight", RightEyeGroup },
            { "EyeSquintRight", RightEyeGroup },
            { "EyeWideRight", RightEyeGroup },
            { "JawForward", JawGroup },
            { "JawLeft", JawGroup },
            { "JawRight", JawGroup },
            { "JawOpen", JawGroup },
            { "MouthClose", MouthGroup },
            { "MouthFunnel", MouthGroup },
            { "MouthPucker", MouthGroup },
            { "MouthRight", MouthGroup },
            { "MouthLeft", MouthGroup },
            { "MouthSmileRight", MouthGroup },
            { "MouthSmileLeft", MouthGroup },
            { "MouthFrownRight", MouthGroup },
            { "MouthFrownLeft", MouthGroup },
            { "MouthDimpleLeft", MouthGroup },
            { "MouthDimpleRight", MouthGroup },
            { "MouthStretchLeft", MouthGroup },
            { "MouthStretchRight", MouthGroup },
            { "MouthRollLower", MouthGroup },
            { "MouthRollUpper", MouthGroup },
            { "MouthShrugLower", MouthGroup },
            { "MouthShrugUpper", MouthGroup },
            { "MouthPressLeft", MouthGroup },
            { "MouthPressRight", MouthGroup },
            { "MouthLowerDownLeft", MouthGroup },
            { "MouthLowerDownRight", MouthGroup },
            { "MouthUpperUpLeft", MouthGroup },
            { "MouthUpperUpRight", MouthGroup },
            { "BrowDownLeft", LeftEyeGroup },
            { "BrowDownRight", RightEyeGroup },
            { "BrowInnerUp", "" }, // Intentionally unset
            { "BrowOuterUpLeft", LeftEyeGroup },
            { "BrowOuterUpRight", RightEyeGroup },
            { "CheekPuff", CheekGroup },
            { "CheekSquintLeft", LeftEyeGroup },
            { "CheekSquintRight", RightEyeGroup },
            { "NoseSneerLeft", NoseGroup },
            { "NoseSneerRight", NoseGroup },
            { "TongueOut", TongueGroup },
        };

        private readonly string[] _allTransforms =
        {
            "Head",
            "Neck",
            "Chest",
            "Spine",
            "Hips",
        };

        public float transformSmoothTime = 0.1f;
        public float blendShapeSmoothTime = 0.1f;

        public float TransformSmoothTime
        {
            get => transformSmoothTime / 10;
            set => transformSmoothTime = value * 10;
        }

        public float BlendShapeSmoothTime
        {
            get => blendShapeSmoothTime / 10;
            set => blendShapeSmoothTime = value * 10;
        }

        public GameObject model;

        private VRMBlendShapeProxy Proxy => model.GetComponent<VRMBlendShapeProxy>();

        private Dictionary<string, Smoothable<float>> _targetShapes;
        private Dictionary<string, Smoothable<Tuple<Vector3, Quaternion>>> _targetTransforms;
        private Dictionary<BlendShapeKey, float> _bsValues;
        private Smoothable<Tuple<Vector3, Quaternion>> _targetRoot;

        public Transform root;
        public string headName = "Head";
        private Transform _head;
        public string neckName = "Neck";
        private Transform _neck;
        public string chestName = "Chest";
        private Transform _chest;
        public string spineName = "Spine";
        private Transform _spine;
        public string hipsName = "Hips";
        private Transform _hips;

        // OSC Params
        public int port = 39541;
        public bool autoStart = true;

        // Start is called before the first frame update
        private void Start()
        {
#if UNITY_EDITOR

            _oscServer.OnDataReceivedEditor.AddListener(HandleVmcMessage);
#else
            _oscServer.onDataReceived.AddListener(HandleVmcMessage);
#endif

            // Init Vars
            _targetShapes = new Dictionary<string, Smoothable<float>>();
            _targetTransforms = new Dictionary<string, Smoothable<Tuple<Vector3, Quaternion>>>();
            _bsValues = new Dictionary<BlendShapeKey, float>();

            SetBones();

            var tuple = new Tuple<Vector3, Quaternion>(Vector3.zero, Quaternion.identity);

            _targetRoot = new Smoothable<Tuple<Vector3, Quaternion>>(tuple, tuple, Time.time, Time.time);

            foreach (var shape in _allShapes.Keys)
            {
                _targetShapes[shape] = new Smoothable<float>(0, 0, Time.time, Time.time);
                _bsValues[BlendShapeKey.CreateUnknown(shape)] = 0;
            }

            foreach (var t in _allTransforms)
            {
                _targetTransforms[t] = new Smoothable<Tuple<Vector3, Quaternion>>(tuple, tuple, Time.time, Time.time);
            }

            if (!Proxy)
            {
                Debug.LogError("Failed to get proxy component.");
            }
        }

        private void SetBones()
        {
            var animator = model.GetComponent<Animator>();
            foreach (var bone in animator.avatar.humanDescription.human)
            {
                switch (bone.humanName)
                {
                    case "Head":
                        headName = bone.boneName;
                        break;
                    case "Neck":
                        neckName = bone.boneName;
                        break;
                    case "Chest":
                        chestName = bone.boneName;
                        break;
                    case "Spine":
                        spineName = bone.boneName;
                        break;
                    case "Hips":
                        hipsName = bone.boneName;
                        break;
                }
            }

            root = model.transform.root;

            _head = root.FindDescendant(headName);
            _neck = root.FindDescendant(neckName);
            _chest = root.FindDescendant(chestName);
            _spine = root.FindDescendant(spineName);
            _hips = root.FindDescendant(hipsName);
        }

        // Update is called once per frame
        private void Update()
        {
            _oscServer.UpdatePort(port);
            _oscServer.UpdateReceive();

            if (trackFace)
            {
                UpdateBlendShapes();
            }

            if (trackTransforms)
            {
                UpdateRoot();
                UpdateBones();
            }
        }

        void Awake()
        {
            _oscServer.UpdatePort(port);
        }

        void OnEnable()
        {
            if (autoStart)
            {
                _oscServer.StartServer();
            }
        }

        void OnDisable()
        {
            _oscServer.StopServer();
        }

        private void UpdateRoot()
        {
            var progress = _targetRoot.GetProgress();

            root.position = progress < 1
                ? Vector3.Lerp(_targetRoot.Prev.Item1, _targetRoot.Val.Item1, _targetRoot.GetProgress())
                : _targetRoot.Val.Item1;
        }

        private void UpdateBones()
        {
            foreach (var r in _targetTransforms)
            {
                var t = GetTransformFromName(r.Key);
                if (!t) return;

                var progress = r.Value.GetProgress();

                t.rotation = progress < 1
                    ? Quaternion.Slerp(r.Value.Prev.Item2, r.Value.Val.Item2, r.Value.GetProgress())
                    : r.Value.Val.Item2;
            }
        }

        private void UpdateBlendShapes()
        {
            foreach (var s in _targetShapes)
            {
                foreach (var v in _bsValues.Keys.Where(v => v.Name == s.Key))
                {
                    _bsValues[v] = Mathf.Lerp(s.Value.Prev, s.Value.Val, s.Value.GetProgress());

                    break;
                }
            }

            Proxy.SetValues(_bsValues);
        }

        private AnimationCurve GetBlendShapeCurve(string bsName)
        {
            var group = _allShapes[bsName];

            switch (group)
            {
                case "Left Eye":
                    return leftEyeCurve;
                case "Right Eye":
                    return rightEyeCurve;
                case "Mouth":
                    return mouthCurve;
                case "Jaw":
                    return jawCurve;
                case "Tongue":
                    return tongueCurve;
                case "Nose":
                    return noseCurve;
                case "Cheek":
                    return cheekCurve;
                default:
                    return AnimationCurve.Linear(0, 0, 1, 1);
            }
        }

        private Tuple<Vector3, Quaternion> GetTupleFromValues(IReadOnlyList<object> values)
        {
            if (values.Count != 8) throw new Exception("Invalid array length.");

            var px = (float)values[1];
            var py = (float)values[2];
            var pz = (float)values[3];
            var qx = (float)values[4];
            var qy = (float)values[5];
            var qz = (float)values[6];
            var qw = (float)values[7];

            var targetPos = new Vector3(px, py, pz);
            var targetRot = new Quaternion(qx, qy, qz, qw);

            return new Tuple<Vector3, Quaternion>(targetPos + offset, targetRot);
        }

        [UsedImplicitly]
        public void HandleVmcMessage(Message m)
        {
            switch (m.address)
            {
                case "/VMC/Ext/Root/Pos":
                    _targetRoot.Prev = new Tuple<Vector3, Quaternion>(root.position, root.rotation);
                    _targetRoot.Val = GetTupleFromValues(m.values);
                    _targetRoot.StartTime = Time.time;
                    _targetRoot.TargetTime = Time.time + TransformSmoothTime;
                    return;
                case "/VMC/Ext/Bone/Pos":
                    var bone = (string)m.values[0];

                    if (_targetTransforms.ContainsKey(bone))
                    {
                        var t = GetTransformFromName(bone);
                        if (!t)
                        {
                            return;
                        }

                        // TODO: Check allocs
                        _targetTransforms[bone].Prev = new Tuple<Vector3, Quaternion>(t.position, t.rotation);
                        _targetTransforms[bone].Val = GetTupleFromValues(m.values);
                        _targetTransforms[bone].StartTime = Time.time;
                        _targetTransforms[bone].TargetTime = Time.time + TransformSmoothTime;
                    }

                    break;
                case "/VMC/Ext/Blend/Val":
                    var bs = (string)m.values[0];
                    var val = (float)m.values[1];

                    if (!_targetShapes.ContainsKey(bs))
                    {
                        break;
                    }

                    var adjustedVal = Mathf.Clamp01(GetBlendShapeCurve(bs).Evaluate(val));

                    var prev = Proxy.GetValue(bs);
                    _targetShapes[bs].Prev = prev;
                    _targetShapes[bs].Val = adjustedVal;
                    _targetShapes[bs].StartTime = Time.time;
                    _targetShapes[bs].TargetTime = Time.time + BlendShapeSmoothTime;
                    break;
            }
        }

        [CanBeNull]
        private Transform GetTransformFromName(string tName)
        {
            switch (tName)
            {
                case "Head":
                    return _head;
                case "Neck":
                    return _neck;
                case "Chest":
                    return _chest;
                case "Spine":
                    return _spine;
                case "Hips":
                    return _hips;
                default:
                    return null;
            }
        }
    }

    internal class Smoothable<T>
    {
        public Smoothable(T prev, T val, float startTime, float targetTime)
        {
            Prev = prev;
            Val = val;
            StartTime = startTime;
            TargetTime = targetTime;
        }

        public float GetProgress()
        {
            var total = TargetTime - StartTime;

            var progress = Time.time - StartTime;

            if (total == 0)
            {
                return 1;
            }

            return Mathf.Clamp01(progress / total);
        }

        public T Prev { get; set; }
        public T Val { get; set; }
        public float StartTime { get; set; }
        public float TargetTime { get; set; }
    }
}