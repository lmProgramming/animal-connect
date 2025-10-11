using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class UICurvedArrow : MonoBehaviour
    {
        [FormerlySerializedAs("Point1")] public GameObject point1;
        [FormerlySerializedAs("Point2")] public Transform point2;
        [FormerlySerializedAs("Point3")] public GameObject point3;
        public LineRenderer linerenderer;
        public int vertexCount = 12;
        public float point2YPositionBias = 2;

        // Update is called once per frame
        private void Update()
        {
            point2.transform.position = new Vector3((point1.transform.position.x + point3.transform.position.x) / 2,
                (point1.transform.position.y + point3.transform.position.y) / 2 + point2YPositionBias,
                (point1.transform.position.z + point3.transform.position.z) / 2);
            var pointList = new List<Vector3>();

            for (float ratio = 0; ratio <= 1; ratio += 1 / (float)vertexCount)
            {
                var tangent1 = Vector3.Lerp(point1.transform.position, point2.position, ratio);
                var tangent2 = Vector3.Lerp(point2.position, point3.transform.position, ratio);
                var curve = Vector3.Lerp(tangent1, tangent2, ratio);

                pointList.Add(curve);
            }

            linerenderer.positionCount = pointList.Count;
            linerenderer.SetPositions(pointList.ToArray());
        }
    }
}