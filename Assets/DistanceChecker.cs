using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DistanceChecker : MonoBehaviour
{
    public RMObjectComponent objectToCompare;

    public float distance;

    private void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if (!isActiveAndEnabled) return;

        Gizmos.color = Color.red;

        if (!objectToCompare) return;

        Vector3 p = this.transform.position;

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(Vector3.zero, 1);

        Gizmos.matrix = objectToCompare.transform.localToWorldMatrix;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(Vector3.zero, 1);

        /*Gizmos.matrix = objectToCompare.transform.worldToLocalMatrix;
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(p, 0.02f);*/

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(p, 0.03f);

        var bufferData = objectToCompare.GetBufferData();

        //Vector3 lp = objectToCompare.transform.InverseTransformPoint(p);
        Vector3 lp = bufferData.worldToObjectMatrix * new Vector4(p.x, p.y, p.z, 1.0f);
        float lpl = lp.magnitude;
        Vector3 lpn = lp / Mathf.Max(lpl, 0.000001f);

        Vector3 modifiedlp = new Vector3(lp.x * objectToCompare.transform.lossyScale.x, lp.y * objectToCompare.transform.lossyScale.y, lp.z * objectToCompare.transform.lossyScale.z);
        Vector3 modifiedlp2 = new Vector3(lp.x * objectToCompare.transform.lossyScale.x, lp.y * objectToCompare.transform.lossyScale.y, lp.z * objectToCompare.transform.lossyScale.z);
        float mlpl = modifiedlp.magnitude;

        modifiedlp.x = Mathf.Abs(modifiedlp.x);
        modifiedlp.y = Mathf.Abs(modifiedlp.y);
        modifiedlp.z = Mathf.Abs(modifiedlp.z);

        //lp = new Vector3(lp.x * objectToCompare.transform.lossyScale.x, lp.y * objectToCompare.transform.lossyScale.y, lp.z * objectToCompare.transform.lossyScale.z);

        /*Gizmos.color = Color.blue;
        Gizmos.DrawSphere(lp, 0.01f);*/

        Vector3 foci = new Vector3(
            Mathf.Abs(Mathf.Sqrt(Mathf.Pow(Mathf.Max(objectToCompare.transform.lossyScale.y, objectToCompare.transform.lossyScale.z), 2) - Mathf.Pow(Mathf.Min(objectToCompare.transform.lossyScale.y, objectToCompare.transform.lossyScale.z), 2))),
            Mathf.Abs(Mathf.Sqrt(Mathf.Pow(Mathf.Max(objectToCompare.transform.lossyScale.x, objectToCompare.transform.lossyScale.z), 2) - Mathf.Pow(Mathf.Min(objectToCompare.transform.lossyScale.x, objectToCompare.transform.lossyScale.z), 2))),
            Mathf.Abs(Mathf.Sqrt(Mathf.Pow(Mathf.Max(objectToCompare.transform.lossyScale.y, objectToCompare.transform.lossyScale.x), 2) - Mathf.Pow(Mathf.Min(objectToCompare.transform.lossyScale.y, objectToCompare.transform.lossyScale.x), 2))));

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(foci.x * (objectToCompare.transform.lossyScale.y > objectToCompare.transform.lossyScale.z ? Vector3.up : Vector3.forward), 0.02f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(foci.y * (objectToCompare.transform.lossyScale.x > objectToCompare.transform.lossyScale.z ? Vector3.right : Vector3.forward), 0.02f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(foci.z * (objectToCompare.transform.lossyScale.y > objectToCompare.transform.lossyScale.x ? Vector3.up : Vector3.right), 0.02f);

        Vector3 abslp = new Vector3(Mathf.Abs(lp.x), Mathf.Abs(lp.y), Mathf.Abs(lp.z));
        Vector3 cc = abslp;

        float minS = Mathf.Min(objectToCompare.transform.lossyScale.x, objectToCompare.transform.lossyScale.y, objectToCompare.transform.lossyScale.z);
        float maxS = Mathf.Max(objectToCompare.transform.lossyScale.x, objectToCompare.transform.lossyScale.y, objectToCompare.transform.lossyScale.z);

        if (objectToCompare.transform.lossyScale.x == minS)
        {
            cc.x = 0;
            cc.y = foci.z;
            cc.z = foci.y;
        }
        else if (objectToCompare.transform.lossyScale.y == minS)
        {
            cc.y = 0;
            cc.x = foci.z;
            cc.z = foci.x;
        }
        else
        {
            cc.z = 0;
            cc.x = foci.z;
            cc.y = foci.x;
        }

        Vector3 cclp = new Vector3(
                    Mathf.Min(1.0f, abslp.x),
                    Mathf.Min(1.0f, abslp.y),
                    Mathf.Min(1.0f, abslp.z));
        Vector3 ccn = cclp.normalized;
        cc = new Vector3(ccn.x * cc.x, ccn.y * cc.y, ccn.z * cc.z);

        Vector3 fromCCToLpD = (modifiedlp - cc).normalized;

        Vector3 O = cc;
        Vector3 D = fromCCToLpD;

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(O, 0.03f);

        Gizmos.DrawLine(O, modifiedlp);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(O, O + D);

        float a = 1.0f;
        float b = 2.0f * Vector3.Dot(O, D);
        float c = Vector3.Dot(O, O) - 1.0f;

        float d = (-b + Mathf.Sqrt(b * b - 4.0f * a * c) / (2.0f * a)) + Vector3.Dot(O, D);

        Vector3 P = O + D * d;

        ccn = P;

        Debug.Log($"cclp: {cclp} cc: {cc} foci: {foci} d: {d}");

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(ccn, 0.03f);

        Vector3 lnp = ccn;

        //if (mlpl < 1.0f) lnp = lp.normalized;
        Vector3 wcds = modifiedlp - new Vector3(lnp.x * objectToCompare.transform.lossyScale.x, lnp.y * objectToCompare.transform.lossyScale.y, lnp.z * objectToCompare.transform.lossyScale.z);
        distance = wcds.magnitude * ((lpl < 1) ? -1 : 1);

        Vector3 r = objectToCompare.transform.lossyScale;
        Vector3 mp = modifiedlp2;
        float k0 = div(mp, r).magnitude;
        float k1 = div(mp, mul(r,r)).magnitude;
        distance = k0 * (k0 - 1.0f) / k1;

        /*Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lp, lds);*/


        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.0f, 0.5f, 0.5f, 0.7f);
        Gizmos.DrawSphere(lnp, 0.02f);

        Gizmos.color = new Color(0.0f, 0.5f, 0.0f, 0.7f);
        Gizmos.DrawSphere(modifiedlp, 0.02f);

        Gizmos.color = new Color(0.8f, 0.0f, 0.0f, 0.7f);
        //Gizmos.DrawSphere(wnp, 0.02f);


        Gizmos.color = new Color(0.0f, 0.0f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(p, distance);

        Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, objectToCompare.transform.lossyScale);
        Gizmos.matrix = scaleMatrix;
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        Gizmos.DrawWireSphere(Vector3.zero, 1);

        Gizmos.matrix = Matrix4x4.identity;

        /*Gizmos.color = new Color(0.5f, 0.0f, 0.0f, 0.3f);
        Gizmos.DrawWireSphere(lp, lpl - 1);*/

        Gizmos.matrix = Matrix4x4.identity;

        //Gizmos.DrawWireSphere(wcds, 0.02f);

        //Debug.Log($"D: {distance} lds: {lds} foci: {foci}");
    }

    Vector3 div(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }

    Vector3 mul(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}
