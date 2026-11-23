using UnityEngine;

// Trailer-only: per-frame overrides used while filming.
public class TrailerHelper : MonoBehaviour
{
    public bool hideLose;
    // Hides drones, satellites and space rockets so only the nuclear missiles fly.
    public bool hideFlyers;
    // Hides the screen-space canvas (news banners, menus) for clean shots.
    public bool hideCanvas;
    // Hides both info centers (the small station with the floating "i").
    public bool hideInfo;
    public Transform orbiter;
    public Vector3 orbitCenter;
    public float orbitRadius, orbitHeight, orbitSpeed, orbitAngle;

    static readonly string[] Flyers = { "Drone(Clone)", "Satellite", "Rocket" };

    void LateUpdate()
    {
        CountryManager cm = CountryManager.instance;
        if (hideLose && cm != null && cm.LoseMenu != null && cm.LoseMenu.activeSelf)
            cm.LoseMenu.SetActive(false);

        if (hideFlyers)
        {
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (System.Array.IndexOf(Flyers, t.name) >= 0 && t.gameObject.activeSelf)
                    t.gameObject.SetActive(false);
            }
        }

        if (hideInfo)
        {
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == "InfoCenter" && t.gameObject.activeSelf)
                    t.gameObject.SetActive(false);
        }

        if (orbiter != null)
        {
            orbitAngle += orbitSpeed * Time.deltaTime;
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 position = orbitCenter + new Vector3(Mathf.Cos(rad) * orbitRadius, orbitHeight, Mathf.Sin(rad) * orbitRadius);
            // Face along the circle (counter-clockwise when the speed is positive), banking a little into the turn.
            Vector3 heading = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * Mathf.Sign(orbitSpeed);
            orbiter.SetPositionAndRotation(position, Quaternion.LookRotation(heading) * Quaternion.Euler(0f, 0f, 12f * Mathf.Sign(orbitSpeed)));
        }

        if (hideCanvas)
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.enabled)
                    canvas.enabled = false;
        }
    }
}
