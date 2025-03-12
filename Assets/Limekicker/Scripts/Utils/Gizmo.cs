using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gizmo : MonoBehaviour
{
    public enum GizmoTypes
    {
        Cube = 0,
        Sphere = 1,
        Icon = 2,
        Arrow = 3
    };
    public bool scaleWithObject = true;
    public GizmoTypes gizmoType;
    public Texture2D sprite;
    public float size = 1f;
    public Color color = new Color(1, 0, 0);
    public bool wireframe = false;

    private void OnDrawGizmos()
    {
        float scalex = 1f, scaley = 1f, scalez = 1f;
        Gizmos.color = color;

        if(scaleWithObject)
        {
            scalex = transform.localScale.x;
            scaley = transform.localScale.y;
            scalez = transform.localScale.z;
        }

        switch (gizmoType)
        {
            case GizmoTypes.Cube:
                if (!wireframe)
                    Gizmos.DrawCube(transform.position, new Vector3(size * scalex, size * scaley, size * scalez));
                else Gizmos.DrawWireCube(transform.position, new Vector3(size * scalex, size * scaley, size * scalez));
                break;
            case GizmoTypes.Sphere:
                if (!wireframe)
                    Gizmos.DrawSphere(transform.position, size * scalex);
                else Gizmos.DrawWireSphere(transform.position, size * scalex);
                break;
            case GizmoTypes.Icon:
                if(sprite != null)
                    Gizmos.DrawIcon(transform.position, sprite.name, true, Color.white);
                break;
            case GizmoTypes.Arrow:
                Arrow(transform.position, transform.forward * scalex, scalex * 0.25f);
                break;
        }
    }

    private void Arrow(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }
}
