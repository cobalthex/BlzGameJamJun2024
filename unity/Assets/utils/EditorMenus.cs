#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EditorMenus
{
    [MenuItem("Tools/Find Far Objects")]
    public static void FindFarObjects()
    {
        var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if ((Mathf.Abs(obj.transform.position.x) > 10000) ||
                (Mathf.Abs(obj.transform.position.y) > 5000) ||
                (Mathf.Abs(obj.transform.position.z) > 10000))
                Debug.LogWarning($"Found object {obj} at location {obj.transform.position}");
        }
    }

    [MenuItem("Tools/Reset Camera to Player")]
    public static void ResetCameraToPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var camera = player.GetComponentInChildren<Camera>();

        camera.enabled = false;
        camera.enabled = true;
    }

    [MenuItem("Tools/Debug Game _F5")]
    static void F5PlayOrStopGame()
    {
        if (!Application.isPlaying)
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);
        }
        EditorApplication.ExecuteMenuItem("Edit/Play");
    }

    static void TeleportObject(GameObject obj, Vector3 down)
    {
        Vector3 position;

        Collider collider;
        Renderer renderer;
        if ((collider = obj.GetComponent<Collider>()) != null)
        {
            var projection = Vector3.Project(collider.bounds.extents, down);
            position = collider.bounds.center - projection;
        }
        else if ((renderer = obj.GetComponent<Renderer>()) != null)
        {
            var projection = Vector3.Project(renderer.bounds.extents, down);
            position = renderer.bounds.center - projection;
        }
        else
            position = obj.transform.position;

        // note: this will only detect objects with a collider
        if (Physics.Raycast(position, down, out var hit))
        {
            obj.transform.position -= (position - hit.point);
        }
        else
        {
            Debug.LogWarning("No collider to drop onto");
        }
    }

    private static void TeleportSelectedObject(Vector3 direction)
    {
        if (Selection.activeGameObject == null)
            return;

        TeleportObject(Selection.activeGameObject, direction);
    }

    [MenuItem("Tools/Teleport Object/Drop _g")]
    public static void DropObject() => TeleportSelectedObject(Physics.gravity.normalized);

    [MenuItem("Tools/Teleport Object/Forward")]
    public static void TeleportObjectForward() => TeleportSelectedObject(Selection.activeTransform.forward);

    [MenuItem("Tools/Teleport Object/Backward")]
    public static void TeleportObjectBackward() => TeleportSelectedObject(-Selection.activeTransform.forward);

    [MenuItem("Tools/Teleport Object/Up")]
    public static void TeleportObjectUp() => TeleportSelectedObject(Selection.activeTransform.up);

    [MenuItem("Tools/Teleport Object/Down")]
    public static void TeleportObjectDown() => TeleportSelectedObject(-Selection.activeTransform.up);

    [MenuItem("Tools/Teleport Object/Right")]
    public static void TeleportObjectRight() => TeleportSelectedObject(Selection.activeTransform.right);

    [MenuItem("Tools/Teleport Object/Left")]
    public static void TeleportObjectLeft() => TeleportSelectedObject(-Selection.activeTransform.right);

    [MenuItem("Tools/Teleport Object/+X")]
    public static void TeleportObjectPX() => TeleportSelectedObject(Vector3.right);

    [MenuItem("Tools/Teleport Object/-X")]
    public static void TeleportObjectNX() => TeleportSelectedObject(Vector3.left);

    [MenuItem("Tools/Teleport Object/+Y")]
    public static void TeleportObjectPY() => TeleportSelectedObject(Vector3.up);

    [MenuItem("Tools/Teleport Object/-Y")]
    public static void TeleportObjectNY() => TeleportSelectedObject(Vector3.down);

    [MenuItem("Tools/Teleport Object/+Z")]
    public static void TeleportObjectPZ() => TeleportSelectedObject(Vector3.forward);

    [MenuItem("Tools/Teleport Object/-Z")]
    public static void TeleportObjectNZ() => TeleportSelectedObject(Vector3.back);

    [MenuItem("Tools/Teleport Object/To cursor _c")]
    public static void TeleportObjectToCursor()
    {
        if (Selection.activeTransform == null)
            return;

        var mousePosition = Event.current.mousePosition;
        mousePosition.y = SceneView.lastActiveSceneView.position.height - mousePosition.y;
        var ray = SceneView.lastActiveSceneView.camera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out var hit))
            Selection.activeTransform.position = hit.point; // todo: make smart and push the object out of the floor
    }

    [MenuItem("Tools/Teleport Object/Center on origin")]
    public static void TeleportObjectsToOrigin()
    {
        if (Selection.gameObjects.Length == 0)
            return;

        var center = Selection.gameObjects[0].transform.position;
        for (int i = 1; i < Selection.gameObjects.Length; ++i)
            center += Selection.gameObjects[i].transform.position;

        center /= Selection.gameObjects.Length;

        foreach (var obj in Selection.gameObjects)
            obj.transform.position = center - obj.transform.position;
    }
}

#endif // UNITY_EDITOR