﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SprUnity;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SprUnity {

#if UNITY_EDITOR
    [CustomEditor(typeof(ActionState))]
    public class ActionStateEditor : Editor {
        public override void OnInspectorGUI() {
            bool textChangeComp = false;
            EditorGUI.BeginChangeCheck();
            Event e = Event.current;
            if (e.keyCode == KeyCode.Return && Input.eatKeyPressOnTextFieldFocus) {
                textChangeComp = true;
                Event.current.Use();
            }
            target.name = EditorGUILayout.TextField("Name", target.name);
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);
            }
            if (textChangeComp) {
                string mainPath = AssetDatabase.GetAssetPath(this);
                //EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath(mainPath));
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath((ActionState)target));
            }
        }
    }
#endif

    public class ActionState : ScriptableObject {
        // ----- ----- ----- ----- -----

        [HideInInspector]
        public ActionStateMachine stateMachine;
        public KeyPoseData keyframe;
        [HideInInspector]
        public List<ActionTransition> transitions = new List<ActionTransition>();
        public List<string> useParams;

        public float duration = 0.5f;
        public float spring = 1.0f;
        public float damper = 1.0f;

        [HideInInspector]
        public BlendController blendController;
        public bool useFace = false;
        public string blend = "";
        public float blendv = 1f;
        public float time = 0.3f;
        public float interval = 0f;
        public KeyPoseTransformer transformers;

        // ----- ----- ----- ----- -----
        // 実行時
        [HideInInspector]
        private float timeFromEnter;
        public float TimeFromEnter {
            get {
                return timeFromEnter;
            }
        }

        // ----- ----- ----- ----- -----
        // Editor関係
        [HideInInspector]
        public Rect stateNodeRect = new Rect(0, 0, 200, 50);
        private bool isDragged;
        private bool isSelected;
        private bool isCurrent = false;
        [HideInInspector]
        public int serialCount;

        private GUIStyle appliedStyle = new GUIStyle();
        static public GUIStyle defaultStyle = new GUIStyle();
        static public GUIStyle selectedStyle = new GUIStyle();
        static public GUIStyle currentStateStyle = new GUIStyle();

        // ----- ----- ----- ----- ----- -----
        // Setter/Getter
        public bool IsCurrent {
            get {
                return isCurrent;
            }
            set {
                isCurrent = value;
            }
        }

        // ----- ----- ----- ----- ----- -----
        // Creator

        static ActionTransition CreateTransition(ActionState from, ActionState to) {
#if UNITY_EDITOR
            var transition = ScriptableObject.CreateInstance<ActionTransition>();
            transition.name = "transition";
            transition.fromState = from;
            transition.toState = to;

            if (from != null) {
                // ActionStateからの遷移
                AssetDatabase.AddObjectToAsset(transition, from);
                from.transitions.Add(transition);
                transition.stateMachine = from.stateMachine;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(from));
            } else {
                // Entryからの遷移
                AssetDatabase.AddObjectToAsset(transition, to.stateMachine);
                to.stateMachine.entryTransitions.Add(transition);
                transition.stateMachine = to.stateMachine;
                transition.time = 0.0f;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(to.stateMachine));
            }

            return transition;
#else
        return null;
#endif
        }

        // ----- ----- ----- ----- ----- -----
        // State Machine Events

        // Enter event of the state
        public List<BoneSubMovementPair> OnEnter() {
            isCurrent = true;
            timeFromEnter = 0.0f;
            Debug.Log("Enter state:" + name + " at time:" + Time.time);
            appliedStyle = currentStateStyle;
            Body body = stateMachine.body;
            if (body == null) { body = GameObject.FindObjectOfType<Body>(); }
            if (body != null) {
                List<float> parameters = new List<float>();
                foreach (var l in useParams) {
                    parameters.Add(stateMachine.parameters[l]);
                }
                if (useFace) {
                    if (stateMachine.blendController == null) {
                        stateMachine.blendController = body.GetComponent<BlendController>();
                        blendController = stateMachine.blendController;
                    }
                    if (blendController != null) {
                        blendController.BlendSet(interval, blend, blendv, time);
                    }
                }

                // ターゲット位置による変換後のKeyPose

                return keyframe.Action(body, duration, 0, spring, damper);
            }
            return null;
        }

        public void OnUpdate() {
            timeFromEnter += Time.fixedDeltaTime;
        }

        // Exit event of the state
        public void OnExit() {
            appliedStyle = defaultStyle;
        }


        // ----- ----- ----- ----- ----- -----
        // Editor

        public void DrawStateNode(int id) {
            //GUI.DragWindow();
            /*
            int nTransitions = transitions.Count;
            for(int i = 0; i < nTransitions; i++) {
                Rect rect = new Rect(new Vector2(0, i * 20 + 15), new Vector2(stateNodeRect.width, 20));
                transitions[i].priority = i;
                GUI.Box(rect, transitions[i].name);
            }
            stateNodeRect.height = Mathf.Max(20 + 20 * nTransitions, 50);
            */
        }

        public void Drag(Vector2 delta) {
            stateNodeRect.position += delta;
        }

        public void Draw(int id, bool isCurrent) {
            //GUI.Box(stateNodeRect, name, GUI.skin.box);
            if (isCurrent) {
                if (isSelected || isDragged) {
                    GUI.Window(id, stateNodeRect, DrawStateNode, name, "flow node 6 on");
                } else {
                    GUI.Window(id, stateNodeRect, DrawStateNode, name, "flow node 6");
                }
            }else if (isSelected || isDragged) {
                GUI.Window(id, stateNodeRect, DrawStateNode, name, "flow node 0 on");
            } else {
                GUI.Window(id, stateNodeRect, DrawStateNode, name, "flow node 0");
            }
        }

        public bool ProcessEvents() {
#if UNITY_EDITOR
            Event e = Event.current;
            switch (e.type) {
                case EventType.MouseDown:
                    if (e.button == 0) {
                        if (stateNodeRect.Contains(e.mousePosition)) {
                            isDragged = true;
                            isSelected = true;
                            Selection.activeObject = this;
                            appliedStyle = selectedStyle;
                            GUI.changed = true;
                        } else {
                            GUI.changed = true;
                            isSelected = false;
                            appliedStyle = defaultStyle;
                        }
                    }
                    if (e.button == 1) {
                        if (stateNodeRect.Contains(e.mousePosition)) {
                            OnContextMenu(e.mousePosition);
                        }
                    }
                    break;

                case EventType.MouseUp:
                    isDragged = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isDragged) {
                        Drag(e.delta);
                        e.Use();
                        return true;
                    }
                    break;
            }
#endif
            return false;
        }

        private void OnContextMenu(Vector2 mousePosition) {
#if UNITY_EDITOR
            GenericMenu genericMenu = new GenericMenu();
            List<ActionState> states = stateMachine.states;
            int nStates = states.Count;
            for (int i = 0; i < nStates; i++) {
                ActionState state = states[i]; // 
                genericMenu.AddItem(new GUIContent("Add Transition to../" + states[i].name), false, () => OnClickAddTransition(this, state));
            }
            genericMenu.AddItem(new GUIContent("Add Transition to../" + "Exit"), false, () => OnClickAddTransition(this, null));
            genericMenu.AddItem(new GUIContent("Add Transition from../" + "Entry"), false, () => OnClickAddTransition(null, this));
            int nTransitions = transitions.Count;
            for (int i = 0; i < nTransitions; i++) {
                ActionTransition transition = transitions[i]; // 
                genericMenu.AddItem(new GUIContent("Remove Transition/" + transition.name + i), false, () => OnRemoveTransition(transition));
            }
            genericMenu.AddItem(new GUIContent("Remove State"), false, () => OnRemoveState());
            genericMenu.ShowAsContext();
#endif
        }

        private void OnClickAddTransition(ActionState from, ActionState to) {
            CreateTransition(from, to);
        }

        private void OnRemoveState() {
            // ステートマシンの関係する遷移を全部消す
            List<ActionTransition> deleteList = new List<ActionTransition>();
            List<ActionTransition> removeListEntry = new List<ActionTransition>();
            var entryTransitions = stateMachine.entryTransitions;
            for (int i = 0; i < entryTransitions.Count; i++) {
                if (entryTransitions[i].toState == this) {
                    removeListEntry.Add(entryTransitions[i]);
                    deleteList.Add(entryTransitions[i]);
                }
            }
            foreach (var transition in removeListEntry) {
                entryTransitions.Remove(transition);
            }
            var states = stateMachine.states;
            List<ActionTransition> removeList = new List<ActionTransition>();
            for (int i = 0; i < states.Count; i++) {
                removeList.Clear();
                foreach (var transition in states[i].transitions) {
                    if (transition.fromState == this || transition.toState == this) {
                        removeList.Add(transition);
                        deleteList.Add(transition);
                    }
                }
                foreach (var transition in removeList) {
                    states[i].transitions.Remove(transition);
                }
            }
            foreach (var deleteTransition in deleteList) {
                Object.DestroyImmediate(deleteTransition, true);
            }
#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(this.stateMachine);
            Object.DestroyImmediate(this, true);
            AssetDatabase.ImportAsset(path);
#endif
        }

        private void OnRemoveTransition(ActionTransition transition) {
#if UNITY_EDITOR
            transitions.Remove(transition);
            Object.DestroyImmediate(transition, true);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this.stateMachine));
#endif
        }

        public void OnValidate() {
            stateMachine.isChanged = true;
        }
    }

}