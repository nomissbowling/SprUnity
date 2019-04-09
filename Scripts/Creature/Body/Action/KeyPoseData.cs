﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using SprCs;
using SprUnity;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SprUnity {

#if UNITY_EDITOR
    [CustomEditor(typeof(KeyPoseData))]
    public class KeyPoseDataEditor : Editor {

        void OnEnable() {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        void OnDisable() {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

        public override void OnInspectorGUI() {
            bool textChangeComp = false;
            EditorGUI.BeginChangeCheck();
            Event e = Event.current;
            if (e.keyCode == KeyCode.Return && Input.eatKeyPressOnTextFieldFocus) {
                textChangeComp = true;
                Event.current.Use();
            }
            target.name = EditorGUILayout.TextField("Name", target.name);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);
            }
            if (textChangeComp) {
                string mainPath = AssetDatabase.GetAssetPath(this);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((KeyPoseData)target));
            }
            DrawDefaultInspector();
            KeyPoseData keyPose = (KeyPoseData)target;

            if (GUILayout.Button("Test")) {
                keyPose.Action();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Use Current Pose")) {
                keyPose.InitializeByCurrentPose();
            }
        }

        public void OnSceneGUI(SceneView sceneView) {
            KeyPoseData keyPose = (KeyPoseData)target;

            var body = GameObject.FindObjectOfType<Body>();

            foreach (var boneKeyPose in keyPose.boneKeyPoses) {
                if (boneKeyPose.usePosition) {
                    EditorGUI.BeginChangeCheck();
                    if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.World) {
                        Vector3 position = Handles.PositionHandle(boneKeyPose.position, Quaternion.identity);
                        if (EditorGUI.EndChangeCheck()) {
                            Undo.RecordObject(target, "Change KeyPose Target Position");
                            boneKeyPose.position = position;
                            if (body) {
                                boneKeyPose.ConvertWorldToBoneLocal(body);
                            }
                            EditorUtility.SetDirty(target);
                        }
                    }
                    if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BoneBaseLocal && body != null) {
                        boneKeyPose.ConvertBoneLocalToWorld(body);
                        Vector3 position = Handles.PositionHandle(boneKeyPose.position, Quaternion.identity);
                        if (EditorGUI.EndChangeCheck()) {
                            Undo.RecordObject(target, "Change KeyPose Target Position");
                            boneKeyPose.position = position;
                            boneKeyPose.ConvertWorldToBoneLocal(body);
                            EditorUtility.SetDirty(target);
                        }
                    }
                }

                if (boneKeyPose.useRotation) {
                    EditorGUI.BeginChangeCheck();
                    if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.World) {
                        Quaternion rotation = Handles.RotationHandle(boneKeyPose.rotation, boneKeyPose.position);
                        if (EditorGUI.EndChangeCheck()) {
                            Undo.RecordObject(target, "Change KeyPose Target Rotation");
                            boneKeyPose.rotation = rotation;
                            if (body) {
                                boneKeyPose.ConvertWorldToBoneLocal(body);
                            }
                            EditorUtility.SetDirty(target);
                        }
                    }
                    if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BoneBaseLocal && body != null) {
                        boneKeyPose.ConvertBoneLocalToWorld(body);
                        Quaternion rotation = Handles.RotationHandle(boneKeyPose.rotation, boneKeyPose.position);
                        if (EditorGUI.EndChangeCheck()) {
                            Undo.RecordObject(target, "Change KeyPose Target Position");
                            boneKeyPose.rotation = rotation;
                            boneKeyPose.ConvertWorldToBoneLocal(body);
                            EditorUtility.SetDirty(target);
                        }
                    }
                }
            }
        }
    }
    /*
    [CustomPropertyDrawer(typeof(BoneKeyPose))]
    public class BoneKeyPosePropertyDrawer : PropertyDrawer {
        public bool showBoneKeyPose;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            base.OnGUI(position, property, label);
            EditorGUI.BeginProperty(position, label, property);
            showBoneKeyPose = EditorGUILayout.Foldout(showBoneKeyPose, ((HumanBodyBones)property.FindPropertyRelative("boneId").enumValueIndex).ToString());
            if (showBoneKeyPose) {

            }
            EditorGUI.EndProperty();
        }
    }
    */
#endif

    [Serializable]
    public class BoneKeyPose {
        public HumanBodyBones boneId = HumanBodyBones.Hips;
        public string boneIdString = "";
        public enum CoordinateMode {
            World, // World
            BoneBaseLocal, // Local coordinate (Bone GameObject)
            BodyLocal, // Local coordinate (Body GameObject)
        };
        public CoordinateMode coordinateMode;
        // World Info
        public Vector3 position = new Vector3();
        public Quaternion rotation = new Quaternion();
        // Local Info
        public HumanBodyBones coordinateParent;
        public Vector3 localPosition = new Vector3();
        public Vector3 normalizedLocalPosition = new Vector3();
        public Quaternion localRotation = Quaternion.identity;
        // Control Flags
        public bool usePosition = true;
        public bool useRotation = true;
        // 
        public float lookAtRatio = 0;
        // 
        public Vector2 boneKeyPoseTiming = new Vector2(0.0f, 1.0f);
        public float startTime {
            get { return boneKeyPoseTiming.x; }
            set { boneKeyPoseTiming.x = value; }
        }
        public float endTime {
            get { return boneKeyPoseTiming.y; }
            set { boneKeyPoseTiming.y = value; }
        }

        public BoneKeyPose Clone() {
            BoneKeyPose k = new BoneKeyPose();
            k.boneId = this.boneId;
            k.boneIdString = this.boneIdString;
            k.coordinateMode = this.coordinateMode;
            k.position = this.position;
            k.rotation = this.rotation;
            k.coordinateParent = this.coordinateParent;
            k.localPosition = this.localPosition;
            k.normalizedLocalPosition = this.normalizedLocalPosition;
            k.localRotation = this.localRotation;
            k.usePosition = this.usePosition;
            k.useRotation = this.useRotation;
            k.lookAtRatio = this.lookAtRatio;
            k.boneKeyPoseTiming = this.boneKeyPoseTiming;
            return k;
        }

        public void Enable(bool e) {
            usePosition = useRotation = e;
        }

        public void ConvertBoneLocalToWorld(Body body = null) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                Bone coordinateBaseBone = body[coordinateParent];
                if (coordinateBaseBone.ikActuator?.phIKActuator != null) {
                    Posed ikSolidPose = coordinateBaseBone.ikActuator.phIKActuator.GetSolidTempPose();
                    position = ikSolidPose.Pos().ToVector3() + ikSolidPose.Ori().ToQuaternion() * (normalizedLocalPosition * body.height);
                    rotation = ikSolidPose.Ori().ToQuaternion() * localRotation;
                } else {
                    position = coordinateBaseBone.transform.position + coordinateBaseBone.transform.rotation * (normalizedLocalPosition * body.height);
                    rotation = coordinateBaseBone.transform.rotation * localRotation;
                }
            }
        }
        public void ConvertWorldToBoneLocal(Body body = null) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                Bone coordinateBaseBone = body[coordinateParent];
                if (coordinateBaseBone.ikActuator?.phIKActuator != null) {
                    Posed ikSolidPose = coordinateBaseBone.ikActuator.phIKActuator.GetSolidTempPose();
                    localPosition = Quaternion.Inverse(ikSolidPose.Ori().ToQuaternion()) * (position - ikSolidPose.Pos().ToVector3());
                    normalizedLocalPosition = localPosition / body.height;
                    localRotation = Quaternion.Inverse(ikSolidPose.Ori().ToQuaternion()) * rotation;
                } else {
                    localPosition = Quaternion.Inverse(coordinateBaseBone.transform.rotation) * (position - coordinateBaseBone.transform.position);
                    normalizedLocalPosition = localPosition / body.height;
                    localRotation = Quaternion.Inverse(coordinateBaseBone.transform.rotation) * rotation;
                }
            }
        }
        public void ConvertBoneLocalToOtherBoneLocal(Body body, HumanBodyBones from, HumanBodyBones to) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                ConvertBoneLocalToWorld();
                coordinateParent = to;
                ConvertWorldToBoneLocal();
            }
        }

        public void ConvertBodyLocalToWorld(Body body) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                position = body.transform.position + body.transform.rotation * (normalizedLocalPosition * body.height);
                rotation = body.transform.rotation * localRotation;
            }
        }

        public void ConvertWorldToBodyLocal(Body body) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                localPosition = Quaternion.Inverse(body.transform.rotation) * (position - body.transform.position);
                normalizedLocalPosition = localPosition / body.height;
                localRotation = Quaternion.Inverse(body.transform.rotation) * rotation;
            }
        }

        public void ConvertBodyLocalToBoneLocal(Body body) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                ConvertBodyLocalToWorld(body);
                ConvertWorldToBoneLocal(body);
            }
        }

        public void ConvertBoneLocalToBodyLocal(Body body) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                ConvertBoneLocalToWorld(body);
                ConvertWorldToBodyLocal(body);
            }
        }
    }

    // 実行時に使用されるKeyPose
    public class KeyPose {
        public List<BoneKeyPose> boneKeyPoses = new List<BoneKeyPose>();

        public float testDuration = 1.0f;
        public float testSpring = 1.0f;
        public float testDamper = 1.0f;

        public BoneKeyPose this[string key] {
            get {
                foreach (var boneKeyPose in boneKeyPoses) {
                    if (boneKeyPose.boneId.ToString() == key) {
                        return boneKeyPose;
                    }
                }
                return null;
            }
        }
        // <!!> Is it better ?
        public BoneKeyPose this[HumanBodyBones key] {
            get { return this[key.ToString()]; }
        }

        public void ParserSpecifiedParts(KeyPoseData data, HumanBodyBones[] boneIds = null) {
            boneKeyPoses.Clear();
            foreach(var boneKeyPose in data.boneKeyPoses) {
                foreach(var boneId in boneIds) {
                    if(boneId == boneKeyPose.boneId) {
                        boneKeyPoses.Add(boneKeyPose.Clone());
                    }
                }
            }
        }
        public void Parser(KeyPoseData data) {
            boneKeyPoses.Clear();
            foreach (var boneKeyPose in data.boneKeyPoses) {
                boneKeyPoses.Add(boneKeyPose.Clone());
            }
        }

        public List<BoneSubMovementPair> Action(Body body = null, float duration = -1, float startTime = -1, float spring = -1, float damper = -1, Quaternion? rotate = null) {
            if (!rotate.HasValue) { rotate = Quaternion.identity; }

            if (duration < 0) { duration = testDuration; }
            if (startTime < 0) { startTime = 0; }
            if (spring < 0) { spring = testSpring; }
            if (damper < 0) { damper = testDamper; }

            List<BoneSubMovementPair> logs = new List<BoneSubMovementPair>();
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                foreach (var boneKeyPose in boneKeyPoses) {
                    if (boneKeyPose.usePosition || boneKeyPose.useRotation) {
                        Bone bone = (boneKeyPose.boneIdString != "") ? body[boneKeyPose.boneIdString] : body[boneKeyPose.boneId];
                        Quaternion ratioRotate = Quaternion.Slerp(Quaternion.identity, (Quaternion)rotate, boneKeyPose.lookAtRatio);
                        var pose = new Pose(ratioRotate * boneKeyPose.position, ratioRotate * boneKeyPose.rotation);
                        if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BoneBaseLocal) {
                            Bone baseBone = body[boneKeyPose.coordinateParent];
                            pose.position = baseBone.transform.position + baseBone.transform.rotation * (boneKeyPose.normalizedLocalPosition * body.height);
                            pose.rotation = boneKeyPose.localRotation * baseBone.transform.rotation;
                        }
                        if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BodyLocal) {
                            pose.position = body.transform.position + body.transform.rotation * (boneKeyPose.normalizedLocalPosition * body.height);
                            pose.rotation = boneKeyPose.localRotation * body.transform.rotation;
                        }
                        var springDamper = new Vector2(spring, damper);
                        var sub = bone.controller.AddSubMovement(pose, springDamper, startTime + duration, duration, usePos: boneKeyPose.usePosition, useRot: boneKeyPose.useRotation);
                        BoneSubMovementPair log = new BoneSubMovementPair(bone, sub.Clone());
                        Debug.Log(sub.p0 + " " + sub.p1 + " " + sub.t0 + " " + sub.t1);
                        logs.Add(log);
                    }
                }
            }
            return logs;
        }
    }

#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Action/Create KeyPose")]
#endif
    public class KeyPoseData : ScriptableObject {
        public List<BoneKeyPose> boneKeyPoses = new List<BoneKeyPose>();

        public float testDuration = 1.0f;
        public float testSpring = 1.0f;
        public float testDamper = 1.0f;

        public BoneKeyPose this[string key] {
            get {
                foreach (var boneKeyPose in boneKeyPoses) {
                    if (boneKeyPose.boneId.ToString() == key) {
                        return boneKeyPose;
                    }
                }
                Debug.LogWarning("KeyPose " + this.name + " does not contain BoneKeyPose of " + key);
                return null;
            }
        }
        // <!!> Is it better ?
        public BoneKeyPose this[HumanBodyBones key] {
            get { return this[key.ToString()]; }
        }

        public void InitializeByCurrentPose(Body body = null) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                boneKeyPoses.Clear();
                foreach (var bone in body.bones) {
                    if (bone.ikEndEffector != null && bone.controller != null && bone.controller.enabled) {
                        BoneKeyPose boneKeyPose = new BoneKeyPose();
                        boneKeyPose.position = bone.transform.position;
                        boneKeyPose.rotation = bone.transform.rotation;

                        for (int i = 0; i < (int)HumanBodyBones.LastBone; i++) {
                            if (((HumanBodyBones)i).ToString() == bone.label) {
                                boneKeyPose.boneId = (HumanBodyBones)i;
                            }
                        }

                        if (bone.ikEndEffector.phIKEndEffector != null) {
                            if (bone.ikEndEffector.phIKEndEffector.IsPositionControlEnabled()) {
                                boneKeyPose.position = bone.ikEndEffector.phIKEndEffector.GetTargetPosition().ToVector3();
                                boneKeyPose.usePosition = true;
                            } else {
                                boneKeyPose.usePosition = false;
                            }

                            if (bone.ikEndEffector.phIKEndEffector.IsOrientationControlEnabled()) {
                                boneKeyPose.rotation = bone.ikEndEffector.phIKEndEffector.GetTargetOrientation().ToQuaternion();
                                boneKeyPose.useRotation = true;
                            } else {
                                boneKeyPose.useRotation = false;
                            }

                        } else {
                            if (bone.ikEndEffector.desc.bPosition) {
#if UNITY_EDITOR
                                if (EditorApplication.isPlaying) {
                                    boneKeyPose.position = ((Vec3d)(bone.ikEndEffector.desc.targetPosition)).ToVector3();
                                }
#endif
                                boneKeyPose.usePosition = true;
                            } else {
                                boneKeyPose.usePosition = false;
                            }

                            if (bone.ikEndEffector.desc.bOrientation) {
#if UNITY_EDITOR
                                if (EditorApplication.isPlaying) {
                                    boneKeyPose.rotation = ((Quaterniond)(bone.ikEndEffector.desc.targetOrientation)).ToQuaternion();
                                }
#endif
                                boneKeyPose.useRotation = true;
                            } else {
                                boneKeyPose.useRotation = false;
                            }

                        }

                        boneKeyPose.ConvertWorldToBoneLocal();
                        boneKeyPoses.Add(boneKeyPose);
                    }
                }

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
#endif
            }
        }

        public List<BoneSubMovementPair> Action(Body body = null, float duration = -1, float startTime = -1, float spring = -1, float damper = -1, Quaternion? rotate = null) {
            if (!rotate.HasValue) { rotate = Quaternion.identity; }

            if (duration < 0) { duration = testDuration; }
            if (startTime < 0) { startTime = 0; }
            if (spring < 0) { spring = testSpring; }
            if (damper < 0) { damper = testDamper; }

            List<BoneSubMovementPair> logs = new List<BoneSubMovementPair>();
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                foreach (var boneKeyPose in boneKeyPoses) {
                    if (boneKeyPose.usePosition || boneKeyPose.useRotation) {
                        Bone bone = (boneKeyPose.boneIdString != "") ? body[boneKeyPose.boneIdString] : body[boneKeyPose.boneId];
                        Quaternion ratioRotate = Quaternion.Slerp(Quaternion.identity, (Quaternion)rotate, boneKeyPose.lookAtRatio);
                        var pose = new Pose(ratioRotate * boneKeyPose.position, ratioRotate * boneKeyPose.rotation);
                        if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BoneBaseLocal) {
                            Bone baseBone = body[boneKeyPose.coordinateParent];
                            pose.position = baseBone.transform.position + baseBone.transform.rotation * (boneKeyPose.normalizedLocalPosition * body.height);
                            pose.rotation = boneKeyPose.localRotation * baseBone.transform.rotation;
                        }
                        if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BodyLocal) {
                            pose.position = body.transform.position + body.transform.rotation * (boneKeyPose.normalizedLocalPosition * body.height);
                            pose.rotation = boneKeyPose.localRotation * body.transform.rotation;
                        }
                        var springDamper = new Vector2(spring, damper);
                        var sub = bone.controller.AddSubMovement(pose, springDamper, startTime + duration, duration, usePos: boneKeyPose.usePosition, useRot: boneKeyPose.useRotation);
                        BoneSubMovementPair log = new BoneSubMovementPair(bone, sub.Clone());
                        Debug.Log(sub.p0 + " " + sub.p1 + " " + sub.t0 + " " + sub.t1);
                        logs.Add(log);
                    }
                }
            }
            return logs;
        }

        public List<BoneKeyPose> GetBoneKeyPoses(Body body) {
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body == null) { return null; }
            List<BoneKeyPose> appliedBoneKeyPoses = new List<BoneKeyPose>();
            foreach (var boneKeyPose in boneKeyPoses) {
                BoneKeyPose keyPoseApplied = new BoneKeyPose();
                Bone coordinateBaseBone = body[boneKeyPose.coordinateParent];
                Bone controlBone = body[boneKeyPose.boneId];
                //Vector3 targetDir = target ? target.transform.position - coordinateBaseBone.transform.position : coordinateBaseBone.transform.rotation * Vector3.forward;

                keyPoseApplied.boneId = boneKeyPose.boneId;
                if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BoneBaseLocal) {
                    // 位置の補正
                    //Vector3 pos = coordinateBaseBone.transform.position + Quaternion.LookRotation(targetDir, coordinateBaseBone.transform.rotation * Vector3.up) * boneKeyPose.localPosition;
                    keyPoseApplied.position = coordinateBaseBone.transform.position + coordinateBaseBone.transform.rotation * (boneKeyPose.normalizedLocalPosition * body.height);
                    // 姿勢の補正
                    //Quaternion rot = boneKeyPose.localRotation * Quaternion.LookRotation(targetDir, coordinateBaseBone.transform.rotation * Vector3.up);
                    keyPoseApplied.rotation = boneKeyPose.localRotation * coordinateBaseBone.transform.rotation;
                } else if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.World) {
                    keyPoseApplied.position = boneKeyPose.position;
                    keyPoseApplied.rotation = boneKeyPose.rotation;
                } else if (boneKeyPose.coordinateMode == BoneKeyPose.CoordinateMode.BodyLocal) {
                    // 位置の補正
                    keyPoseApplied.position = body.transform.position + body.transform.rotation * (boneKeyPose.normalizedLocalPosition * body.height);
                    // 姿勢の補正
                    keyPoseApplied.rotation = boneKeyPose.localRotation * body.transform.rotation;
                }
                keyPoseApplied.usePosition = boneKeyPose.usePosition;
                keyPoseApplied.useRotation = boneKeyPose.useRotation;

                appliedBoneKeyPoses.Add(keyPoseApplied);
            }
            return appliedBoneKeyPoses;
        }
    }

}