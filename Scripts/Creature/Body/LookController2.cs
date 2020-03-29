using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using SprUnity;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LookController2))]
public class LookController2Editor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        LookController2 action = (LookController2)target;

        EditorGUILayout.PrefixLabel("速");
        action.speed = EditorGUILayout.Slider(action.speed, 0.0f, 1.0f);

        EditorGUILayout.Space();

        EditorGUILayout.PrefixLabel("目（水平）");
        action.manualEyeAngle.y = EditorGUILayout.Slider(action.manualEyeAngle.y, -90.0f, 90.0f);
        EditorGUILayout.PrefixLabel("目（垂直）");
        action.manualEyeAngle.x = EditorGUILayout.Slider(action.manualEyeAngle.x, -90.0f, 90.0f);

        EditorGUILayout.Space();

        EditorGUILayout.PrefixLabel("頭（水平）");
        action.manualHeadAngle.y = EditorGUILayout.Slider(action.manualHeadAngle.y, -90.0f, 90.0f);
        EditorGUILayout.PrefixLabel("頭（垂直）");
        action.manualHeadAngle.x = EditorGUILayout.Slider(action.manualHeadAngle.x, -90.0f, 90.0f);
    }
}
#endif

public class LookController2 : LookController {

    // Manually Control Mode
    public bool manualEye = false;
    public bool manualHead = false;

    public TextMesh debugText = null;
    public bool showDebugText = false;

    enum ManualControlTarget { Eye, Head };
    private ManualControlTarget manualControlTarget = ManualControlTarget.Eye;

    public bool showPanel = false;
    public float correctionValue = 0.0f;
    public KeyCode trigger = KeyCode.Alpha3;
    public string filename = "EyeCalib.txt";

    // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

    // 頭の基準姿勢（サッケードのたびに変化する）
    private Quaternion baseHeadRotation;

    // 次のサッケードで頭の基準姿勢になる値（現在の頭の姿勢にローパスフィルタをかけたもの）
    private Quaternion nextBaseHeadRotation;

    // 現在の頭の目標姿勢
    private Quaternion targetHeadRotation;

    private Texture2D menuTexture;

    // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

    // 手動モードにおける目の方向
    [HideInInspector]
    public Vector2 manualEyeAngle = new Vector2(0, 0);

    // 手動モードにおける頭の方向
    [HideInInspector]
    public Vector3 manualHeadAngle = new Vector3(0, 0, 0);

    // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

    void OnGUI() {
        if (showPanel) {
            GUIStyle style = new GUIStyle();
            style.normal.background = menuTexture;
            GUI.BeginGroup(new Rect(20, 20, 440, 210), style);
            GUI.color = Color.black;

            GUI.Label(new Rect(10, 15, 100, 100), "視線キャリブレーション");

            GUI.Label(new Rect(10, 50, 100, 30), "補正量：" + correctionValue.ToString());
            correctionValue = GUI.HorizontalSlider(new Rect(120, 55, 300, 30), correctionValue, -100, +100);

            GUI.Label(new Rect(10, 15 + 35 * 2, 100, 30), "目：" + manualEyeAngle.ToString());
            GUI.Label(new Rect(10, 15 + 35 * 3, 100, 30), "頭：" + manualHeadAngle.ToString());

            GUI.EndGroup();
        }
    }

    void Start () {
        debugText.gameObject.SetActive(false);

        menuTexture = new Texture2D(16, 16);
        for (int i = 0; i < menuTexture.height; i++) {
            for (int j = 0; j < menuTexture.height; j++) {
                menuTexture.SetPixel(i, j, Color.grey);
            }
        }

        Load();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Z)) {
            manualEye = true;
            manualControlTarget = ManualControlTarget.Eye;
        }
        if (Input.GetKeyDown(KeyCode.X)) {
            manualHead = true;
            manualControlTarget = ManualControlTarget.Head;
        }

        if (Input.GetKeyDown(KeyCode.A)) {
            manualEye = false;
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            manualHead = false;
        }

        // --

        float moveAngle = Time.deltaTime * 90.0f;

        if (Input.GetKey(KeyCode.LeftArrow)) {
            if (manualControlTarget == ManualControlTarget.Eye) {
                manualEyeAngle.y = Mathf.Clamp(manualEyeAngle.y + moveAngle, -90, 90);
            } else {
                manualHeadAngle.y = Mathf.Clamp(manualHeadAngle.y + moveAngle, -90, 90);
            }
        }
        if (Input.GetKey(KeyCode.RightArrow)) {
            if (manualControlTarget == ManualControlTarget.Eye) {
                manualEyeAngle.y = Mathf.Clamp(manualEyeAngle.y - moveAngle, -90, 90);
            } else {
                manualHeadAngle.y = Mathf.Clamp(manualHeadAngle.y - moveAngle, -90, 90);
            }
        }
        if (Input.GetKey(KeyCode.DownArrow)) {
            if (manualControlTarget == ManualControlTarget.Eye) {
                manualEyeAngle.x = Mathf.Clamp(manualEyeAngle.x + moveAngle * 0.5f, -45, 45);
            } else {
                manualHeadAngle.x = Mathf.Clamp(manualHeadAngle.x + moveAngle * 0.5f, -45, 45);
            }
        }
        if (Input.GetKey(KeyCode.UpArrow)) {
            if (manualControlTarget == ManualControlTarget.Eye) {
                manualEyeAngle.x = Mathf.Clamp(manualEyeAngle.x - moveAngle * 0.5f, -45, 45);
            } else {
                manualHeadAngle.x = Mathf.Clamp(manualHeadAngle.x - moveAngle * 0.5f, -45, 45);
            }
        }
        if (Input.GetKey(KeyCode.PageDown)) {
            manualHeadAngle.z = Mathf.Clamp(manualHeadAngle.z + moveAngle * 0.5f, -45, 45);
        }
        if (Input.GetKey(KeyCode.PageUp)) {
            manualHeadAngle.z = Mathf.Clamp(manualHeadAngle.z - moveAngle * 0.5f, -45, 45);
        }

        // --

        if (Input.GetKey(KeyCode.C)) {
            showDebugText = true;
            if (Input.GetKey(KeyCode.LeftShift)) {
                debugText.gameObject.SetActive(true);
            }
        }
        if (Input.GetKey(KeyCode.V)) {
            showDebugText = false;
            if (Input.GetKey(KeyCode.LeftShift)) {
                debugText.gameObject.SetActive(false);
            }
        }

        // -- 
        if (Input.GetKeyDown(trigger)) {
            showPanel = !showPanel;
            if (!showPanel) {
                Save();
            }
        }
    }

    void FixedUpdate () {
        if (!initialized) {
            if (body != null && body["Head"] != null && body["Head"].solid != null && body["Head"].solid.phSolid != null && body.initialized) {
                Quaternion initialHeadRotation = body["Head"].transform.rotation;
                baseHeadRotation = initialHeadRotation;
                nextBaseHeadRotation = initialHeadRotation;
                targetHeadRotation = initialHeadRotation;

                initialized = true;
            }
        }

        if (!initialized) { return; }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

        if (waitTimer > 0) {
            waitTimer -= Time.fixedDeltaTime;
        } else {

            if (target != null) {
                // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----
                // 目の運動

                // 視線移動量を算出
                Vector3 targEyeDir = (target.transform.position - body["Head"].transform.position).normalized;
                if (targEyeDir.magnitude < 1e-5) { targEyeDir = Vector3.forward; }

                Vector3 targEyeDirVert = targEyeDir; targEyeDirVert.x = 0;
                Vector3 targEyeDirHoriz = targEyeDir; targEyeDirHoriz.y = 0;
                float targAngleVert = Vector3.SignedAngle(Vector3.forward, targEyeDirVert, Vector3.right);
                float targAngleHoriz = Vector3.SignedAngle(Vector3.forward, targEyeDirHoriz, Vector3.up);
                debugText.text = targAngleHoriz + ", " + targAngleVert + "\r\n";

                // キャリブレーション
                float targAngleVertCalib = 1.0f * targAngleVert;
                float targAngleHorizCalib = 2.1f * targAngleHoriz + correctionValue;
                targEyeDir = Quaternion.Euler(targAngleVertCalib, targAngleHorizCalib, 0) * Vector3.forward;

                // -- 手動オーバーライド
                if (manualEye) {
                    targEyeDir = Quaternion.Euler(manualEyeAngle.x, manualEyeAngle.y, 0) * Vector3.forward;
                    debugText.text += manualEyeAngle.y + ", " + manualEyeAngle.x + "\r\n";
                } else {
                    manualEyeAngle.x = targAngleVert;
                    manualEyeAngle.y = targAngleHoriz;
                }
                // --

                // 視線移動速度等の計算
                Vector3 currLEyeDir = body["LeftEye"].controller.rotTrajectory.Last().q1 * new Vector3(0, 0, 1);
                Vector3 currREyeDir = body["RightEye"].controller.rotTrajectory.Last().q1 * new Vector3(0, 0, 1);
                Vector3 currEyeDir = (currLEyeDir + currREyeDir) * 0.5f;
                float diffAngleEye = Vector3.Angle(targEyeDir, currEyeDir);

                // -- Smooth Persuitの最大追随速度は普通は30[deg/sec]らしいので、これを超えたらSaccade
                bool saccade = (diffAngleEye / Time.fixedDeltaTime > 30.0f);

                // -- 視線移動速度の決定
                float durationEye;
                if (saccade) {
                    durationEye = diffAngleEye * (1 / 500.0f); //  1/500 [sec/deg]
                } else {
                    durationEye = Time.fixedDeltaTime;
                }

                // 視線ベクトルをクォータニオンに変換
                Quaternion eyeTargetRotation = Quaternion.LookRotation(targEyeDir);

                // 動作指示
                if (manualEye) { durationEye = 0.05f; }
                body["LeftEye"].controller.AddSubMovement(new Pose(new Vector3(), eyeTargetRotation), new Vector2(1, 1), durationEye, durationEye);
                body["RightEye"].controller.AddSubMovement(new Pose(new Vector3(), eyeTargetRotation), new Vector2(1, 1), durationEye, durationEye);

                // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----
                // 頭の運動

                if (saccade) {
                    baseHeadRotation = nextBaseHeadRotation;
                }

                // <!!> 今回は頭基準姿勢は固定とする
                baseHeadRotation = Quaternion.identity;

                targetHeadRotation = Quaternion.Slerp(baseHeadRotation, eyeTargetRotation, 0.2f);

                // -- 手動オーバーライド
                if (manualHead) {
                    targetHeadRotation = Quaternion.Euler(manualHeadAngle.x, manualHeadAngle.y, manualHeadAngle.z);
                } else {
                    Vector3 targHeadDirVert = targetHeadRotation * Vector3.forward; targHeadDirVert.x = 0;
                    Vector3 targHeadDirHoriz = targetHeadRotation * Vector3.forward; targHeadDirHoriz.y = 0;
                    manualHeadAngle.x = Vector3.SignedAngle(Vector3.forward, targHeadDirVert, Vector3.right);
                    manualHeadAngle.y = Vector3.SignedAngle(Vector3.forward, targHeadDirHoriz, Vector3.up);
                }
                // --

                Vector3 currHeadDir = body["Head"].controller.rotTrajectory.Last().q1 * new Vector3(0, 0, 1);
                Vector3 targHeadDir = targetHeadRotation * new Vector3(0, 0, 1);
                float diffAngleHead = Vector3.Angle(currHeadDir, targHeadDir);

                // 頭部運動の速度を移動量とspeedの設定値に応じて決定
                float minDurationHead = 0.2f;
                float durationHead = Mathf.Max((1 / (60.0f * speed)) * diffAngleHead, minDurationHead);

                // 動作指示
                if (manualHead) { durationHead = 0.1f; }
                body["Head"].controller.AddSubMovement(new Pose(new Vector3(), targetHeadRotation), new Vector2(1, 1), durationHead + 0.1f, durationHead, usePos: false);

                // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

                waitTimer = Mathf.Max(durationEye, durationHead);
            }

        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

        float alpha = 0.01f;
        Quaternion rot = Quaternion.Slerp(nextBaseHeadRotation, targetHeadRotation, alpha);
        if (Vector3.Angle(rot * Vector3.forward, Vector3.forward) < 20.0f) {
            nextBaseHeadRotation = rot;
        }

        // ----- ----- ----- ----- ----- ----- ----- ----- ----- -----

        if (eyeTargetOutput != null) {
            Vector3 eyeDir = (body["LeftEye"].transform.TransformDirection(Vector3.forward) + body["RightEye"].transform.TransformDirection(Vector3.forward)) * 0.5f;
            Vector3 eyePos = (body["LeftEye"].transform.position + body["RightEye"].transform.position) * 0.5f;
            eyeTargetOutput.transform.position = eyePos + eyeDir * 2.0f;
        }

	}

    // ----- ----- ----- ----- -----

    void Load() {
        FileInfo fileInfo = new FileInfo(Application.dataPath + "/../Settings/" + filename);
        if (fileInfo.Exists) {
            StreamReader reader = fileInfo.OpenText();
            string cv = reader.ReadLine();
            reader.Close();
            correctionValue = float.Parse(cv);
        }
    }

    void Save() {
        FileInfo fileInfo = new FileInfo(Application.dataPath + "/../Settings/" + filename);
        StreamWriter writer = fileInfo.CreateText();
        writer.WriteLine(correctionValue);
        writer.Close();
    }
}
