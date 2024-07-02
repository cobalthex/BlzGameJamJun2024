using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public static class Quats
{
    public struct TwistSwing
    {
        public Quaternion twist; // can be understood as the projected quaternion on the 'twistAxis' (plane normal)
        public Quaternion swing; // this can be understood as the orientation quaternion, without any twisting
    }

    // Code based on https://www.gamedev.net/forums/topic/696882-swing-twist-interpolation-sterp-an-alternative-to-slerp/

    /// <summary>
    /// "Project" a quaternion on a normal, using Swing-twist decomposition
    /// </summary>
    public static Quaternion Project(this Quaternion q, Vector3 onNormal)
    {
        var rotationAxis = new Vector3(q.x, q.y, q.z);
        var projection = Vector3.Project(rotationAxis, onNormal);
        return new Quaternion(projection.x, projection.y, projection.z, q.w).normalized;
    }

    /// <summary>
    /// Decompose a quaternion into it's twist and swing quaternions
    /// </summary>
    /// <param name="q">The input quaternion</param>
    /// <param name="twistAxis">the axis to align the twist to, e.g. a plane normal</param>
    /// <returns>The decomposed quaternions</returns>
    public static TwistSwing DecomposeTS(this Quaternion q, Vector3 twistAxis)
    {
        var rotationAxis = new Vector3(q.x, q.y, q.z);
        var outVal = new TwistSwing();

        // simgularity
        if (rotationAxis.sqrMagnitude < float.Epsilon)
        {
            Vector3 rotatedTwistAxis = q * twistAxis;
            Vector3 swingAxis = Vector3.Cross(twistAxis, rotatedTwistAxis);

            if (swingAxis.sqrMagnitude > float.Epsilon)
            {
                float swingAngle = Vector3.Angle(twistAxis, rotatedTwistAxis);
                outVal.swing = Quaternion.AngleAxis(swingAngle, swingAxis);
            }
            else
            {
                // rotation axis parallel to twist axis
                outVal.swing = Quaternion.identity;
            }

            // always twist 180 degrees for a singularity
            outVal.twist = Quaternion.AngleAxis(180.0f, twistAxis);
        }
        else
        {
            var projection = Vector3.Project(rotationAxis, twistAxis);
            outVal.twist = new Quaternion(projection.x, projection.y, projection.z, q.w).normalized;
            outVal.swing = q * Quaternion.Inverse(outVal.twist);
        }

        return outVal;
    }

    public static Quaternion Sterp(Quaternion from, Quaternion to, Vector3 twistAxis, float t)
    {
        Quaternion deltaRotation = to * Quaternion.Inverse(from);

        var ts = DecomposeTS(deltaRotation, twistAxis);

        Quaternion twist = Quaternion.Slerp(Quaternion.identity, ts.twist, t);
        Quaternion swing = Quaternion.Slerp(Quaternion.identity, ts.swing, t);

        return from * twist * swing; // from * twist * swing?
    }

    /// <summary>
    /// Get the full angle (0-2pi) between two quaternions, with respect to a specific plane of rotation
    /// </summary>
    /// <returns>The delta angle</returns>
    public static float AngleBetween(Quaternion a, Quaternion b, Vector3 withRespectToPlaneNormal)
    {
        var delta = Quaternion.Inverse(a) * b;
        var projection = Vector3.Project(new Vector3(delta.x, delta.y, delta.z), withRespectToPlaneNormal);
        return 2 * Mathf.Atan2(
            projection.magnitude,
            delta.w);

        // var qrel = Project(delta, withRespectToPlaneNormal);
        // return 2 * Mathf.Atan2(
        //     new Vector3(qrel.x, qrel.y, qrel.z).magnitude,
        //     delta.w);
    }
}

public static class Vecs
{
    public static float ScalarProject(Vector3 a, Vector3 b)
    {
        return Vector3.Dot(a, b) / Vector3.Dot(b, b);
    }

    public static Vector3 Multiply(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.x * b.x,
            a.y * b.y,
            a.z * b.z
        );
    }

    public static Vector3 Divide(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.x / b.x,
            a.y / b.y,
            a.z / b.z
        );
    }

    // pass in fov?
    public static bool IsInFrontOf(Vector3 v, Vector3 point, Vector3 direction)
    {
        var dot = Vector3.Dot(v - point, direction);
        return dot > 0;
    }
}

public static class Meshes
{
    public struct PositionOnMesh
    {
        public int m_vertex;
        public int m_direction; // +1 for moving towards increasing vertex indices, -1 for decreasing
        public float m_relativePosition;

        public override string ToString()
        {
            return $"Closest:{m_vertex}{(m_direction >= 0 ? "+1" : "-1")} Rel:{m_relativePosition}";
        }
    }

    public static PositionOnMesh FindPositionOnMesh(Mesh mesh, Transform meshTransform, Vector3 point)
    {
        for (int vertex = 0; vertex < mesh.vertexCount - 1; ++vertex)
        {
            var a = meshTransform.TransformPoint(mesh.vertices[vertex]);
            var b = meshTransform.TransformPoint(mesh.vertices[vertex + 1]);
            var segment = b - a;
            var segmentLen = segment.magnitude;
            var dot = Vector3.Dot(point - a, segment);
            if (dot < 1)
            {
                return new PositionOnMesh
                {
                    m_vertex = vertex,
                    m_direction = dot >= 0 ? 1 : -1,
                    m_relativePosition = dot / segmentLen,
                };
            }
        }

        // todo: set direction to (pos - 0 vertex) . center
        return new PositionOnMesh
        {
            m_vertex = 0,
            m_relativePosition = 0,
            m_direction = 1,
        };
    }
}