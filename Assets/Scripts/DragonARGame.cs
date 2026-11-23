using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Script único del juego. Se agrega al objeto "XR Origin (AR Rig)"
/// (que ya tiene ARRaycastManager y ARPlaneManager).
/// - Toca el suelo detectado para invocar al dragón.
/// - Botón "DRACARYS" para que escupa fuego.
/// - Botón "Reiniciar" para volver a colocarlo.
/// La interfaz se crea por código (no hay que armar Canvas a mano).
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class DragonARGame : MonoBehaviour
{
    ARRaycastManager raycastManager;
    ARPlaneManager planeManager;
    DragonController dragon;
    Text infoText;
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
#endif
        BuildUI();
    }

    void Update()
    {
        if (dragon == null)
        {
            bool hasPlanes = planeManager != null && planeManager.trackables.count > 0;
            infoText.text = hasPlanes
                ? "Toca el suelo para invocar al dragón"
                : "Mueve el teléfono lentamente para detectar el suelo...\n(AR: " + ARSession.state + ")";
        }

        if (!TryGetTouch(out Vector2 pos)) return;
        if (pos.y < Screen.height * 0.22f) return; // zona de botones

        if (raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 point = hits[0].pose.position;
            if (dragon == null)
            {
                dragon = DragonController.Spawn(point);
                infoText.text = "¡Valar Morghulis! Pulsa DRACARYS";
                SetPlanesVisible(false);
            }
            else
            {
                dragon.MoveTo(point);
            }
        }
    }

    bool TryGetTouch(out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            pos = ts.primaryTouch.position.ReadValue();
            return true;
        }
        // Para probar con mouse en el editor
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame)
        {
            pos = m.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pos = Input.GetTouch(0).position;
            return true;
        }
#endif
        pos = default;
        return false;
    }

    void SetPlanesVisible(bool visible)
    {
        if (planeManager == null) return;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(visible);
        planeManager.enabled = visible;
    }

    void OnFire()
    {
        if (dragon != null) dragon.BreatheFire();
    }

    void OnReset()
    {
        if (dragon != null) Destroy(dragon.gameObject);
        dragon = null;
        SetPlanesVisible(true);
    }

    // ---------------- UI por código ----------------
    void BuildUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        var canvasGO = new GameObject("DragonUI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Texto superior
        var tGO = new GameObject("Info");
        tGO.transform.SetParent(canvasGO.transform, false);
        infoText = tGO.AddComponent<Text>();
        infoText.font = font;
        infoText.fontSize = 48;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.color = Color.white;
        infoText.raycastTarget = false;
        var tr = infoText.rectTransform;
        tr.anchorMin = new Vector2(0, 1);
        tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1);
        tr.anchoredPosition = new Vector2(0, -160);
        tr.sizeDelta = new Vector2(-60, 200);
        var outline = tGO.AddComponent<Outline>();
        outline.effectColor = Color.black;

        // Nombre y matrícula (esquina superior derecha)
        var nGO = new GameObject("Autor");
        nGO.transform.SetParent(canvasGO.transform, false);
        var nameText = nGO.AddComponent<Text>();
        nameText.font = font;
        nameText.text = "Wascar Pujols\n1-22-0075";
        nameText.fontSize = 36;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.UpperRight;
        nameText.color = Color.white;
        nameText.raycastTarget = false;
        var nr = nameText.rectTransform;
        nr.anchorMin = nr.anchorMax = new Vector2(1, 1);
        nr.pivot = new Vector2(1, 1);
        nr.anchoredPosition = new Vector2(-30, -30);
        nr.sizeDelta = new Vector2(500, 110);
        nGO.AddComponent<Outline>().effectColor = Color.black;

        MakeButton(canvasGO.transform, font, "DRACARYS", new Vector2(0.5f, 0), new Vector2(-170, 120),
            new Vector2(520, 170), new Color(0.75f, 0.12f, 0.05f), OnFire);
        MakeButton(canvasGO.transform, font, "Reiniciar", new Vector2(0.5f, 0), new Vector2(330, 120),
            new Vector2(330, 170), new Color(0.2f, 0.2f, 0.2f), OnReset);
    }

    void MakeButton(Transform parent, Font font, string label, Vector2 anchor, Vector2 pos, Vector2 size,
        Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var tGO = new GameObject("Text");
        tGO.transform.SetParent(go.transform, false);
        var txt = tGO.AddComponent<Text>();
        txt.font = font;
        txt.text = label;
        txt.fontSize = 58;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }
}
