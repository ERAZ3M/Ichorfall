using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawIcon(transform.position, "sv_icon_dot4_pix16_gizmo", true);
    }
}
