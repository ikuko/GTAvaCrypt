﻿#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerLayer = UnityEditor.Animations.AnimatorControllerLayer;
using Object = UnityEngine.Object;

namespace GeoTetra.GTAvaCrypt
{
    public class AvaCryptController
    {
        private string[] _avaCryptKeyNames;
        private AnimationClip[] _clipsFalse;
        private AnimationClip[] _clipsTrue;

        private const string StateMachineName = "AvaCryptKey{0} State Machine";
        private const string BlendTreeName = "AvaCryptKey{0} Blend Tree";
        private const string BitKeySwitchName = "AvaCryptKey{0}{1} BitKey Switch";

        public void InitializeCount(int count)
        {
            _clipsFalse = new AnimationClip[count];
            _clipsTrue = new AnimationClip[count];
            _avaCryptKeyNames = new string[count];
            for (int i = 0; i < _avaCryptKeyNames.Length; ++i)
            {
                _avaCryptKeyNames[i] = $"BitKey{i}";
            }
        }
        
        public void ValidateAnimations(GameObject gameObject, AnimatorController controller)
        {
            for (int i = 0; i < _avaCryptKeyNames.Length; ++i)
            {
                ValidateClip(gameObject, controller, i);
            }

            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                for (int i = 0; i < _clipsFalse.Length; ++i)
                {
                    string transformPath = AnimationUtility.CalculateTransformPath(meshRenderer.transform, gameObject.transform);
                    _clipsFalse[i].SetCurve(transformPath, typeof(MeshRenderer), $"material._BitKey{i}", new AnimationCurve(new Keyframe(0, 0)));
                    _clipsTrue[i].SetCurve(transformPath, typeof(MeshRenderer), $"material._BitKey{i}", new AnimationCurve(new Keyframe(0, 1)));
                }
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                for (int i = 0; i < _clipsFalse.Length; ++i)
                {
                    string transformPath = AnimationUtility.CalculateTransformPath(skinnedMeshRenderer.transform,gameObject.transform);
                    _clipsFalse[i].SetCurve(transformPath, typeof(SkinnedMeshRenderer), $"material._BitKey{i}", new AnimationCurve(new Keyframe(0, 0)));
                    _clipsTrue[i].SetCurve(transformPath, typeof(SkinnedMeshRenderer), $"material._BitKey{i}", new AnimationCurve(new Keyframe(0, 1)));
                }
            }

            AssetDatabase.SaveAssets();
        }

        private void ValidateClip(GameObject gameObject, AnimatorController controller, int index)
        {
            string controllerPath = AssetDatabase.GetAssetPath(controller);
            
            string clipName = $"{gameObject.name}_{_avaCryptKeyNames[index]}";
            string clipNameFalse = $"{clipName}_False";
            string clipNameFalseFile = $"{clipNameFalse}.anim";
            string clipNameTrue = $"{clipName}_True";
            string clipNameTrueFile = $"{clipNameTrue}.anim";

            if (!AssetDatabase.IsValidFolder(Path.Combine(Path.GetDirectoryName(controllerPath), "GTAvaCrypt")))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(controllerPath), "GTAvaCrypt");

            if (controller.animationClips.All(c => c.name != clipNameFalse))
            {
                _clipsFalse[index] = new AnimationClip()
                {
                    name = clipNameFalse
                };
                string clip0Path = Path.Combine(Path.GetDirectoryName(controllerPath), "GTAvaCrypt", clipNameFalseFile);
                AssetDatabase.CreateAsset(_clipsFalse[index], clip0Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Adding and Saving Clip: {clip0Path}");
            }
            else
            {
                _clipsFalse[index] = controller.animationClips.FirstOrDefault(c => c.name == clipNameFalse);
                Debug.Log($"Found clip: {clipNameFalse}");
            }
            
            if (controller.animationClips.All(c => c.name != clipNameTrue))
            {
                _clipsTrue[index] = new AnimationClip()
                {
                    name = clipNameTrue
                };
                string clip100Path = Path.Combine(Path.GetDirectoryName(controllerPath), "GTAvaCrypt", clipNameTrueFile);
                AssetDatabase.CreateAsset(_clipsTrue[index], clip100Path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Adding and Saving Clip: {clip100Path}");
            }
            else
            {
                _clipsTrue[index] = controller.animationClips.FirstOrDefault(c => c.name == clipNameTrue);
                Debug.Log($"Found clip: {clipNameTrue}");
            }
        }

        public void ValidateParameters(AnimatorController controller)
        {
            foreach (string keyName in _avaCryptKeyNames)
            {
                if (controller.parameters.All(parameter => parameter.name != keyName))
                {
                    controller.AddParameter(keyName, AnimatorControllerParameterType.Bool);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Adding parameter: {keyName}");
                }
                else
                {
                    Debug.Log($"Parameter already added: {keyName}");
                }
            }
        }

        public void ValidateLayers(AnimatorController controller)
        {
            for (int i = 0; i < _avaCryptKeyNames.Length; ++i)
            {
                if (controller.layers.Any(l => l.name == _avaCryptKeyNames[i]))
                {
                    Debug.Log($"Layer already existing: {_avaCryptKeyNames[i]}");
                    var layerIdx = Array.FindIndex(controller.layers, l => l.name == _avaCryptKeyNames[i]);
                    AnimatorControllerLayer layer = controller.layers[layerIdx];

                    if (layer.stateMachine != null)
                        AssetDatabase.RemoveObjectFromAsset(layer.stateMachine);

                    Debug.Log("Array.IndexOf(controller.layers, layer)=" + layerIdx);
                    controller.RemoveLayer(layerIdx);
                }

                CreateLayer(i, controller);
            }
        }
        
        void CreateLayer(int index, AnimatorController controller)
        {
            Debug.Log($"Creating layer: {_avaCryptKeyNames[index]}");
            
            string controllerPath = AssetDatabase.GetAssetPath(controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = _avaCryptKeyNames[index],
                defaultWeight = 1,
                stateMachine = new AnimatorStateMachine
                {
                    name = string.Format(StateMachineName, index)
                },
            };

            controller.AddLayer(layer);
            AssetDatabase.AddObjectToAsset(layer.stateMachine, controllerPath);
            AssetDatabase.SaveAssets();
            
            AddBitKeySwitch(index, layer, controller);
        }
        
        void AddBitKeySwitch(int index, AnimatorControllerLayer layer, AnimatorController controller)
        {
            string trueSwitchName = string.Format(BitKeySwitchName, "True", index);
            string falseSwitchName = string.Format(BitKeySwitchName, "False", index);
            
            AnimatorState falseState = layer.stateMachine.AddState(falseSwitchName);
            falseState.motion = _clipsFalse[index];
            falseState.speed = 1;
            falseState.writeDefaultValues = false;
            
            AnimatorCondition falseCondition = new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                parameter = _avaCryptKeyNames[index],
                threshold = 0
            };

            AnimatorStateTransition falseTransition = layer.stateMachine.AddAnyStateTransition(falseState);
            falseTransition.canTransitionToSelf = false;
            falseTransition.duration = 0;
            falseTransition.conditions = new[] {falseCondition};

            AnimatorState trueState = layer.stateMachine.AddState(trueSwitchName);
            trueState.motion = _clipsTrue[index];
            trueState.speed = 1;
            trueState.writeDefaultValues = false;

            AnimatorCondition trueCondition = new AnimatorCondition
            {
                mode = AnimatorConditionMode.If,
                parameter = _avaCryptKeyNames[index],
            };
            
            AnimatorStateTransition trueTransition = layer.stateMachine.AddAnyStateTransition(trueState);
            trueTransition.canTransitionToSelf = false;
            trueTransition.duration = 0;
            trueTransition.conditions = new[] {trueCondition};
            
            AssetDatabase.SaveAssets();
        }
        
        public void DeleteAvaCryptV1ObjectsFromController(AnimatorController controller)
        {
            string controllerPath = AssetDatabase.GetAssetPath(controller);
            foreach (Object subObject in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
            {
                if (subObject != null && subObject.hideFlags == HideFlags.None && subObject.name.Contains("AvaCrypt"))
                {
                    AssetDatabase.RemoveObjectFromAsset(subObject);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
