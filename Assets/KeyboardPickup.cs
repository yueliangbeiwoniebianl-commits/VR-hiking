using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardPickup : MonoBehaviour
{
    [Header("原来的 XR 摄像机")]
    public Camera playerCamera;

    [Header("拿取设置")]
    public float pickupRange = 5f;
    public float holdDistance = 3f;

    [Header("每次按 N 旋转的角度")]
    public float rotationStep = 20f;

    [Header("介绍文字大小")]
    public int titleFontSize = 26;
    public int descriptionFontSize = 20;

    [Header("高亮框")]
    public Color highlightColor = Color.yellow;
    public float outlineWidth = 3f;
    public float outlinePadding = 8f;

    private PickupInfo selectedItem;
    private Rigidbody heldBody;
    private Renderer[] heldRenderers;

    private bool originalGravity;
    private bool originalKinematic;
    private Quaternion rotationOffset;

    private GUIStyle crosshairStyle;
    private GUIStyle titleStyle;
    private GUIStyle descriptionStyle;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (playerCamera == null)
            return;

        if (heldBody == null)
            FindTarget();
        else
            selectedItem = null;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // M：拿起或放下
        if (keyboard.mKey.wasPressedThisFrame)
        {
            if (heldBody != null)
                Drop();
            else
                PickUp();
        }

        // N：拿起后，每次旋转指定角度
        if (heldBody != null &&
            keyboard.nKey.wasPressedThisFrame)
        {
            rotationOffset =
                Quaternion.AngleAxis(rotationStep, Vector3.up)
                * rotationOffset;
        }
    }

    void FindTarget()
    {
        selectedItem = null;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            pickupRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
            return;

        PickupInfo info =
            hit.collider.GetComponentInParent<PickupInfo>();

        if (info != null &&
            info.GetComponent<Rigidbody>() != null)
        {
            selectedItem = info;
        }
    }

    void PickUp()
    {
        if (selectedItem == null)
        {
            Debug.Log("请用准星对准可以拿取的棋子。");
            return;
        }

        Rigidbody body =
            selectedItem.GetComponent<Rigidbody>();

        if (body == null)
            return;

        // 避免拿到玩家自身
        if (playerCamera.transform.IsChildOf(body.transform))
            return;

        heldBody = body;
        heldRenderers =
            body.GetComponentsInChildren<Renderer>();

        originalGravity = body.useGravity;
        originalKinematic = body.isKinematic;

        // 保留拿起时相对摄像机的朝向
        rotationOffset =
            Quaternion.Inverse(playerCamera.transform.rotation)
            * body.rotation;

        body.useGravity = false;
        body.isKinematic = true;

        // 拿起后隐藏介绍和高亮框
        selectedItem = null;
    }

    void LateUpdate()
    {
        if (heldBody == null || playerCamera == null)
            return;

        Transform cam = playerCamera.transform;
        Transform item = heldBody.transform;

        // 跟随摄像机朝向，并保留 N 键旋转
        item.rotation = cam.rotation * rotationOffset;

        Vector3 targetCenter =
            cam.position +
            cam.forward * Mathf.Max(0.1f, holdDistance);

        // 按模型实际外观中心定位，补偿原点偏移
        if (TryGetBounds(heldRenderers, out Bounds bounds))
        {
            item.position += targetCenter - bounds.center;
        }
        else
        {
            item.position = targetCenter;
        }
    }

    void Drop()
    {
        if (heldBody == null)
            return;

        heldBody.useGravity = originalGravity;
        heldBody.isKinematic = originalKinematic;

        heldBody = null;
        heldRenderers = null;
        selectedItem = null;
    }

    void OnDisable()
    {
        Drop();
        selectedItem = null;
    }

    bool TryGetBounds(
        Renderer[] renderers,
        out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        if (renderers == null)
            return false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
                continue;

            // 只统计模型，不统计粒子、线条等
            if (!(renderer is MeshRenderer) &&
                !(renderer is SkinnedMeshRenderer))
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    void PrepareStyles()
    {
        if (crosshairStyle != null)
            return;

        crosshairStyle = new GUIStyle(GUI.skin.label);
        crosshairStyle.fontSize = 30;
        crosshairStyle.alignment =
            TextAnchor.MiddleCenter;
        crosshairStyle.normal.textColor = Color.red;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        titleStyle.wordWrap = true;

        descriptionStyle = new GUIStyle(GUI.skin.label);
        descriptionStyle.normal.textColor = Color.white;
        descriptionStyle.wordWrap = true;
    }

    void OnGUI()
    {
        if (playerCamera == null)
            return;

        PrepareStyles();

        titleStyle.fontSize = titleFontSize;
        descriptionStyle.fontSize = descriptionFontSize;

        Rect view = playerCamera.pixelRect;
        float centerX = view.center.x;
        float centerY = Screen.height - view.center.y;

        // 始终显示红色准星
        GUI.Label(
            new Rect(centerX - 20, centerY - 20, 40, 40),
            "+",
            crosshairStyle);

        // 拿着棋子时隐藏介绍和高亮
        if (heldBody != null || selectedItem == null)
            return;

        DrawHighlight(selectedItem);

        float width = Mathf.Min(620f, view.width - 40f);

        if (width <= 0f)
            return;

        string title = selectedItem.itemName;
        string description =
            selectedItem.description + "\n\nPress M to pick up";

        float titleHeight = titleStyle.CalcHeight(
            new GUIContent(title), width - 32f);

        float descriptionHeight =
            descriptionStyle.CalcHeight(
                new GUIContent(description), width - 32f);

        float height =
            titleHeight + descriptionHeight + 40f;

        float x = centerX - width / 2f;
        float y =
            Screen.height - view.yMin - height - 25f;

        FillRect(
            new Rect(x, y, width, height),
            new Color(0f, 0f, 0f, 0.85f));

        GUI.Label(
            new Rect(
                x + 16, y + 12,
                width - 32, titleHeight),
            title,
            titleStyle);

        GUI.Label(
            new Rect(
                x + 16, y + 20 + titleHeight,
                width - 32, descriptionHeight),
            description,
            descriptionStyle);
    }

    void DrawHighlight(PickupInfo item)
    {
        Renderer[] renderers =
            item.GetComponentsInChildren<Renderer>();

        if (!TryGetBounds(renderers, out Bounds bounds))
            return;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        float left = float.PositiveInfinity;
        float right = float.NegativeInfinity;
        float top = float.PositiveInfinity;
        float bottom = float.NegativeInfinity;

        // 把模型包围盒的八个角投影到屏幕
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z);

            Vector3 screen =
                playerCamera.WorldToScreenPoint(corner);

            if (screen.z <= playerCamera.nearClipPlane)
                return;

            float guiY = Screen.height - screen.y;

            left = Mathf.Min(left, screen.x);
            right = Mathf.Max(right, screen.x);
            top = Mathf.Min(top, guiY);
            bottom = Mathf.Max(bottom, guiY);
        }

        left -= outlinePadding;
        right += outlinePadding;
        top -= outlinePadding;
        bottom += outlinePadding;

        float width = right - left;
        float height = bottom - top;
        float thickness = Mathf.Max(1f, outlineWidth);

        FillRect(
            new Rect(left, top, width, thickness),
            highlightColor);

        FillRect(
            new Rect(
                left, bottom - thickness,
                width, thickness),
            highlightColor);

        FillRect(
            new Rect(left, top, thickness, height),
            highlightColor);

        FillRect(
            new Rect(
                right - thickness, top,
                thickness, height),
            highlightColor);
    }

    void FillRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = previous;
    }
}