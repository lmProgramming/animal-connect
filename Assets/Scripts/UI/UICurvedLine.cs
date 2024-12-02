using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICurvedArrow : MonoBehaviour
{
    public GameObject Point1;
    public Transform Point2;
    public GameObject Point3;
    public LineRenderer linerenderer;
    public int vertexCount = 12;
    public float point2YPositionBias = 2;

    // Update is called once per frame
    void Update()
    {
        Point2.transform.position = new Vector3((Point1.transform.position.x + Point3.transform.position.x) / 2, 
                                                (Point1.transform.position.y + Point3.transform.position.y) / 2 + point2YPositionBias, 
                                                (Point1.transform.position.z + Point3.transform.position.z) / 2);
        var pointList = new List<Vector3>();

        for (float ratio = 0; ratio <= 1; ratio += 1 / (float)vertexCount)
        {
            var tangent1 = Vector3.Lerp(Point1.transform.position, Point2.position, ratio);
            var tangent2 = Vector3.Lerp(Point2.position, Point3.transform.position, ratio);
            var curve = Vector3.Lerp(tangent1, tangent2, ratio);

            pointList.Add(curve);
        }

        linerenderer.positionCount = pointList.Count;
        linerenderer.SetPositions(pointList.ToArray());
    }
}
