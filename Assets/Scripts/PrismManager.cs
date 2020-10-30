using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;
public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;

    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism,bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }
    
    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions

    private IEnumerable<PrismCollision> PotentialCollisions()
    {


        var masterlist = new List<Tuple<Prism, float, string>>();

        #region Check on x axis
        foreach (Prism shape in prisms)
        {
            var minimum = shape.points.Aggregate<Vector3>((a, b) => a.x < b.x ? a : b).x;
            var maximum = shape.points.Aggregate<Vector3>((a, b) => a.x > b.x ? a : b).x;

            var tupleMin = Tuple.Create<Prism, float, string>(shape, minimum, "min");
            var tupleMax = Tuple.Create<Prism, float, string>(shape, maximum, "max");

            masterlist.Add(tupleMin);
            masterlist.Add(tupleMax);


        }
        List<Tuple<Prism, float, string>> orderedlist = masterlist.OrderBy(term => term.Item2).ToList();

        var sweeplist = new List<Tuple<Prism, float, string>>();


        sweeplist.Add(orderedlist[0]);


        for (int i = 1; i < orderedlist.Count; i++)
        {
            var term = orderedlist[i];
            if (term.Item3.Equals("max"))
            {
                Object itemToRemove = null;
                foreach (var sweeplistterm in sweeplist)
                {
                    if (sweeplistterm.Item1 == term.Item1)
                    {
                        itemToRemove = sweeplistterm;
                    }
                }
                sweeplist.Remove((Tuple<Prism, float, string>)itemToRemove);
            }
            else if (term.Item3.Equals("min"))
            {
                foreach (var sweeplistterm in sweeplist)
                {
                    var checkPrisms = new PrismCollision();
                    checkPrisms.a = term.Item1;
                    checkPrisms.b = sweeplistterm.Item1;
                    yield return checkPrisms;
                }
                sweeplist.Add(term);
            }
        }

        //Check on Z axis
        masterlist.Clear();
        #endregion

        #region Check on z axis
        foreach (Prism shape in prisms)
        {
            var minimum = shape.points.Aggregate<Vector3>((a, b) => a.z < b.z ? a : b).z;
            var maximum = shape.points.Aggregate<Vector3>((a, b) => a.z > b.z ? a : b).z;

            var tupleMin = Tuple.Create<Prism, float, string>(shape, minimum, "min");
            var tupleMax = Tuple.Create<Prism, float, string>(shape, maximum, "max");

            masterlist.Add(tupleMin);
            masterlist.Add(tupleMax);


        }
        orderedlist = masterlist.OrderBy(term => term.Item2).ToList();

        sweeplist = new List<Tuple<Prism, float, string>>();


        sweeplist.Add(orderedlist[0]);


        for (int i = 1; i < orderedlist.Count; i++)
        {
            var term = orderedlist[i];
            if (term.Item3.Equals("max"))
            {
                Object itemToRemove = null;
                foreach (var sweeplistterm in sweeplist)
                {
                    if (sweeplistterm.Item1 == term.Item1)
                    {
                        itemToRemove = sweeplistterm;
                    }
                }
                sweeplist.Remove((Tuple<Prism, float, string>)itemToRemove);
            }
            else if (term.Item3.Equals("min"))
            {
                foreach (var sweeplistterm in sweeplist)
                {
                    var checkPrisms = new PrismCollision();
                    checkPrisms.a = term.Item1;
                    checkPrisms.b = sweeplistterm.Item1;
                    yield return checkPrisms;
                }
                sweeplist.Add(term);
            }
        }
        #endregion



        yield break;
    }


    #region Check Collisions Helper Functions

    private Vector3 tripleCrossProduct(Vector3 a, Vector3 b, Vector3 c)
    {

        var axb = Vector3.Cross(a, b);
        var priorxc = Vector3.Cross(axb, c);

        return priorxc;
    }

    private List<Vector3> calculateMinkowskiDifference(Prism prismA, Prism prismB)
    {
        var minkowskiDifference = new List<Vector3>();

        foreach (var pointA in prismA.points)
            foreach (var pointB in prismB.points)
                minkowskiDifference.Add(pointA - pointB);


        return minkowskiDifference;
    }


    private Vector3 supportFunction(List<Vector3> minkowskiDifference, Vector3 supportAxis, List<Vector3> simplex)
    {
        var filtermink = minkowskiDifference.Except(simplex).ToList();
        return filtermink.Aggregate((a, b) => Vector3.Dot(a, supportAxis) > Vector3.Dot(b, supportAxis) ? a : b);

    }

    private bool GJK(List<Vector3> minkowskiDifference, Prism prismA, Prism prismB)
    {
        var simplex = new List<Vector3>();

        //Create simplex triangle by first picking an arbitrary value, picking the second point using the support fxn, and picking the third point by using the support fxn w/ the orthogonal vector of simplex

        //Assign direction to an arbitrary direction
        Vector3 direction = Vector3.zero;
        while (true){
            switch (simplex.Count)
            {
                case 0:
                    direction = Vector3.forward;
                    break;
                case 1:
                    direction *= -1;

                    break;
                case 2:
                    var firstPointToSecondPoint = simplex[1] - simplex[0];
                    var firstPointToOrigin = -1 * simplex[0];

                    var perpLine = tripleCrossProduct(firstPointToSecondPoint, firstPointToOrigin, firstPointToSecondPoint);

                    direction = perpLine;

                    break;
                case 3:
                    var v1 = simplex[0].normalized - simplex[2].normalized;
                    var v2 = simplex[1].normalized - simplex[2].normalized;
                    var toOrigin = -1 * simplex[2].normalized;

                    var v1Perp = tripleCrossProduct(v2, v1, v1);
                    var v2Perp = tripleCrossProduct(v1, v2, v2);

                    if (Vector3.Dot(v1Perp, toOrigin) > 0)
                    {
                        simplex.RemoveAt(1);
                        direction = v1Perp;
                    }
                    else if (Vector3.Dot(v2Perp, toOrigin) > 0)
                    {
                        simplex.RemoveAt(0);
                        direction = v2Perp;
                    }
                    else
                    {
                        bool testingEPA = EPA(simplex, minkowskiDifference, prismA, prismB);
                        return true;
                    }
                    break;
            }

            simplex.Add(supportFunction(minkowskiDifference, direction, simplex));
            if (Vector3.Dot(simplex[simplex.Count - 1], direction) < 0)
                return false;


        }

            
        
    }

    private float PointToLine(Vector3 p, Vector3 a, Vector3 b)
    {
        var newVec = p - a;
        var dir = b - a;
        var tangent = Vector3.Cross(dir, Vector3.up).normalized;

        var result = Vector3.Dot(newVec, tangent) / (newVec.magnitude) * newVec.magnitude;

        return result;
    }

    private Vector3 PointToLineTangent(Vector3 p, Vector3 a, Vector3 b)
    {
        var newVec = p - a;
        var dir = b - a;
        var tangent = Vector3.Cross(dir, Vector3.up).normalized;

        var result = Vector3.Dot(newVec, tangent) / (newVec.magnitude) * newVec.magnitude;

        return tangent * result;
    }

    private int MinIndex(List<float> list)
    {
        int index = 0;
        foreach (float element in list)
        {
            if (element == list.Min())
            {
                return index;
            }
        }

        return -1;
    }

    private bool EPA(List<Vector3> simplex, List<Vector3> minkowskiDifference, Prism prismA, Prism prismB)
    {
        print(simplex.Count);
        if (PointToLine(simplex[0], simplex[1], simplex[2]) > 0)
        {
            var temp = simplex[0];
            simplex[0] = simplex[1];
            simplex[1] = temp;
        }

        var distToSimplexSegments = new List<float>();
        for (int s = 0; s < simplex.Count; s++)
        {
            var a = simplex[s];
            var b = simplex[(s + 1) % simplex.Count];
            distToSimplexSegments.Add(Mathf.Abs(PointToLine(Vector3.zero, a, b)));
        }

        var minIndex = MinIndex(distToSimplexSegments);
        var minDist = distToSimplexSegments.Min();

        for (int i = 0; ; i++)
        {

            var a = simplex[minIndex];
            var b = simplex[(minIndex + 1) % simplex.Count];

            print(simplex.Count);
            Debug.DrawLine(a, b, Color.cyan, UPDATE_RATE);


            var dir = b - a;
            var tangent = Vector3.Cross(dir, Vector3.up);
            var orientation = -Mathf.Sign(Vector3.Dot(tangent, -simplex[minIndex]));
            var supportAxis = tangent * orientation;
            var supportPoint = minkowskiDifference.Aggregate((c, d) => Vector3.Dot(c, supportAxis) > Vector3.Dot(b, supportAxis) ? c : d);

            if (simplex.Contains(supportPoint))
            {
                break;
            }
            else
            {
                var ind = (minIndex + 1) % simplex.Count;
                simplex.Insert(ind, supportPoint);
                distToSimplexSegments.Insert(ind, float.MaxValue);

                minIndex = MinIndex(distToSimplexSegments);

                for (int s = minIndex; s <= minIndex + 1; s++)
                {
                    var e = simplex[s % simplex.Count];
                    var f = simplex[(s + 1) % simplex.Count];

                    distToSimplexSegments[s % simplex.Count] = Mathf.Abs(PointToLine(Vector3.zero, a, b));
                }

                minIndex = MinIndex(distToSimplexSegments);
                minDist = distToSimplexSegments[minIndex];
            }

        }

        for (int s = 0; s < simplex.Count; s++)
        {
            print(simplex.Count);
            Debug.DrawLine((simplex[s]), simplex[(s + 1) % simplex.Count], Color.yellow, UPDATE_RATE);
        }
        print(simplex.Count);
        Debug.DrawLine(Vector3.zero, (simplex[minIndex] + simplex[(minIndex + 1) % simplex.Count]) / 2, Color.white, UPDATE_RATE);

        var tan = PointToLineTangent(Vector3.zero, simplex[minIndex], simplex[(minIndex + 1) % simplex.Count]);
        Debug.DrawLine(prismA.transform.position, prismA.transform.position - tan, Color.red, UPDATE_RATE);
        Debug.DrawLine(prismB.transform.position, prismB.transform.position + tan, Color.red, UPDATE_RATE);

        return false;
    }


    #endregion




    private bool CheckCollision(PrismCollision collision)
    {
        var prismA = collision.a;
        var prismB = collision.b;


        // Calculate Minkowski Difference

        var minkowskiDifference = calculateMinkowskiDifference(prismA, prismB);

        //Run GJK Algorithm
        var isCollision = GJK(minkowskiDifference, prismA, prismB);

        collision.penetrationDepthVectorAB = Vector3.zero;

        return isCollision;
    }

    #endregion




    #region Private Functions


    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        for (int i = 0; i < collision.a.pointCount; i++)
        {
            collision.a.points[i] += pushA;
        }
        for (int i = 0; i < collision.b.pointCount; i++)
        {
            collision.b.points[i] += pushB;
        }
        //prismObjA.transform.position += pushA;
        //prismObjB.transform.position += pushB;

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }
    
    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();
        
        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion

    #region Utility Classes

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
