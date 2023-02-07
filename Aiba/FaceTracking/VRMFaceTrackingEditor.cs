#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Slider = UnityEngine.UIElements.Slider;

namespace Aiba.FaceTracking
{
    [CustomEditor(typeof(VRMFaceTracking))]
    [CanEditMultipleObjects]
    public class VRMFaceTrackingEditor : Editor
    {
        private VisualElement _root;

        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            _root = new VisualElement();

            // Title 
            _root.Add(new Label
            {
                text = "Aiba's VRM Face Tracker",
                style =
                {
                    color = new StyleColor { value = Color.red },
                    fontSize = new StyleLength { value = new Length { value = 24, unit = LengthUnit.Pixel } },
                    marginBottom = new StyleLength { value = new Length { value = 16, unit = LengthUnit.Pixel } }
                },
            });

            AddConnectionSettings();
            AddMappings();
            AddSmoothing();
            AddSensitivities();

            // Return the finished inspector UI
            return _root;
        }

        private void AddConnectionSettings()
        {
            var foldout = new Foldout
            {
                text = "VMCP Connection Settings",
                style =
                {
                    marginBottom = new StyleLength { value = new Length { value = 16, unit = LengthUnit.Pixel } }
                }
            };

            foldout.Add(new Label
            {
                text =
                    @"Enter the connection details to you face tracking app.",
                style =
                {
                    paddingBottom = new StyleLength { value = new Length { value = 8, unit = LengthUnit.Pixel } },
                    maxWidth = new StyleLength { value = new Length { value = 100, unit = LengthUnit.Percent } },
                }
            });

            foldout.Add(new Toggle { label = "Autostart", bindingPath = "autoStart" });
            foldout.Add(new IntegerField
            {
                bindingPath = "port", label = "Port", tooltip = "This is found in your face tracking app on your phone."
            });

            _root.Add(foldout);
        }

        private void AddMappings()
        {
            var foldout = new Foldout
            {
                text = "Mapping",
                style =
                {
                    marginBottom = new StyleLength { value = new Length { value = 16, unit = LengthUnit.Pixel } }
                }
            };

            foldout.Add(new ObjectField
                { bindingPath = "model", objectType = typeof(GameObject), label = "VRM Model" });
            
            foldout.Add(new TextField{ bindingPath = "headName", label = "Head Bone Name" });
            
            foldout.Add(new Toggle{ bindingPath = "trackFace", label = "Track Face", tooltip = "" });
            foldout.Add(new Toggle{ bindingPath = "trackTransforms", label = "Track Movement", tooltip = "" });

            _root.Add(foldout);
        }

        private void AddSmoothing()
        {
            var foldout = new Foldout
            {
                text = "Smoothing",
                style =
                {
                    marginBottom = new StyleLength { value = new Length { value = 16, unit = LengthUnit.Pixel } }
                }
            };

            foldout.Add(new Label
            {
                text =
                    @"Adjust the smoothing values for your transforms & blendshapes.\nThis will help with reducing model jitter.",
                style =
                {
                    paddingBottom = new StyleLength { value = new Length { value = 8, unit = LengthUnit.Pixel } },
                    maxWidth = new StyleLength { value = new Length { value = 100, unit = LengthUnit.Percent } },
                }
            });

            foldout.Add(new Label { text = "Transform Smoothing" });
            foldout.Add(new Slider
            {
                bindingPath = "transformSmoothTime", lowValue = 0, highValue = 1f, value = .1f
            });

            foldout.Add(new Label { text = "Blendshape Smoothing" });
            foldout.Add(new Slider
            {
                bindingPath = "blendShapeSmoothTime", lowValue = 0, highValue = 1f, value = .1f
            });

            _root.Add(foldout);
        }

        private void AddSensitivities()
        {
            var foldout = new Foldout
            {
                text = "Sensitivities"
            };

            foldout.Add(new Label
            {
                text =
                    "Adjust the curves to change how sensitive each blendshape is.",
                style =
                {
                    paddingBottom = new StyleLength { value = new Length { value = 8, unit = LengthUnit.Pixel } },
                    maxWidth = new StyleLength { value = new Length { value = 100, unit = LengthUnit.Percent } },
                }
            });

            var ks = new SensGroup[]
            {
                new SensGroup(VRMFaceTracking.LeftEyeGroup, "leftEyeCurve"),
                new SensGroup(VRMFaceTracking.RightEyeGroup, "rightEyeCurve"),
                new SensGroup(VRMFaceTracking.MouthGroup, "mouthCurve"),
                new SensGroup(VRMFaceTracking.JawGroup, "jawCurve"),
                new SensGroup(VRMFaceTracking.TongueGroup, "tongueCurve"),
                new SensGroup(VRMFaceTracking.NoseGroup, "noseCurve"),
                new SensGroup(VRMFaceTracking.CheekGroup, "cheekCurve"),
            };

            foreach (var k in ks)
            {
                foldout.Add(new Label { text = k.Label });
                foldout.Add(new CurveField { bindingPath = k.DataKey });
            }


            _root.Add(foldout);
        }
    }

    internal struct SensGroup
    {
        public SensGroup(string label, string dataKey)
        {
            Label = label;
            DataKey = dataKey;
        }

        public readonly string Label;
        public readonly string DataKey;
    }
}

#endif
