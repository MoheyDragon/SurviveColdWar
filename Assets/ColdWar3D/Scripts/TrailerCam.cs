using UnityEngine;

// Trailer-only camera rig. When added, it overrides the board camera every LateUpdate and glides
// between two framings. Remove the component to hand the camera back to WarTable.
public class TrailerCam : MonoBehaviour
{
    public Vector3 fromPos, toPos;
    public Vector3 fromEuler, toEuler;
    public float fromFov = 26f, toFov = 26f;
    public float duration = 4f;
    public float delay;
    float time;
    Camera cam;

    public static TrailerCam Shot(Vector3 fromPos, Vector3 fromEuler, Vector3 toPos, Vector3 toEuler, float duration, float fromFov = 26f, float toFov = 26f)
    {
        var existing = Object.FindFirstObjectByType<TrailerCam>();
        if (existing != null)
            Destroy(existing);
        var go = new GameObject("TrailerCam");
        var shot = go.AddComponent<TrailerCam>();
        shot.fromPos = fromPos; shot.toPos = toPos;
        shot.fromEuler = fromEuler; shot.toEuler = toEuler;
        shot.duration = duration; shot.fromFov = fromFov; shot.toFov = toFov;
        return shot;
    }

    public static void Release()
    {
        foreach (var shot in Object.FindObjectsByType<TrailerCam>(FindObjectsSortMode.None))
            Destroy(shot.gameObject);
        if (Camera.main != null)
            Camera.main.fieldOfView = 26f;
    }

    // Follow mode: the camera sits at target + offset (offset glides fromPos -> toPos) and looks at the target.
    public Transform follow;
    // How tightly the camera keeps up with a moving target (higher = less lag).
    public static float Sharpness = 6f;
    public Vector3 lookBias;
    Vector3 smoothTarget;
    bool hasTarget;

    public static string Follow(string objectName, Vector3 fromOffset, Vector3 toOffset, float duration, float fov, Vector3 lookBias)
    {
        Transform newest = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.InstanceID))
            if (t.name == objectName && t.gameObject.activeInHierarchy && (newest == null || t.GetInstanceID() < newest.GetInstanceID()))
                newest = t;
        if (newest == null) return "not found " + objectName;
        var shot = Shot(fromOffset, Vector3.zero, toOffset, Vector3.zero, duration, fov, fov);
        shot.follow = newest;
        shot.lookBias = lookBias;
        return "following " + newest.name + " at " + newest.position;
    }

    void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;
        time += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((time - delay) / Mathf.Max(0.01f, duration));
        // Smooth ease in and out, like a dolly move.
        t = t * t * t * (t * (6f * t - 15f) + 10f);
        if (follow != null)
        {
            Vector3 target = follow.position;
            smoothTarget = hasTarget ? Vector3.Lerp(smoothTarget, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * Sharpness)) : target;
            hasTarget = true;
            Vector3 pos = smoothTarget + Vector3.LerpUnclamped(fromPos, toPos, t);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(smoothTarget + lookBias - pos));
            cam.fieldOfView = fromFov;
            return;
        }
        cam.transform.SetPositionAndRotation(
            Vector3.LerpUnclamped(fromPos, toPos, t),
            Quaternion.Slerp(Quaternion.Euler(fromEuler), Quaternion.Euler(toEuler), t));
        cam.fieldOfView = Mathf.Lerp(fromFov, toFov, t);
    }
}
