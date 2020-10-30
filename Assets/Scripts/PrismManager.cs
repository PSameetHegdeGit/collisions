﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        for (int i = 0; i < prisms.Count; i++) {
            for (int j = i + 1; j < prisms.Count; j++) {
                var checkPrisms = new PrismCollision();
                checkPrisms.a = prisms[i];
                checkPrisms.b = prisms[j];

                yield return checkPrisms;
            }
        }

        yield break;
    }


    #region Check Collisions Helper Functions

    private Vector3 tripleCrossProduct(Vector3 a, Vector3 b, Vector3 c)
    {
        var axb = Vector3.Cross(a, b);
        var cxprior = Vector3.Cross(c, axb);

        return cxprior;
    }

    private List<Vector3> calculateMinkowskiDifference(Prism prismA, Prism prismB)
    {
        var minkowskiDifference = new List<Vector3>();

        foreach (var pointA in prismA.points)
            foreach (var pointB in prismB.points)
                minkowskiDifference.Add(pointA - pointB);


        return minkowskiDifference;
    }


    private Vector3 supportFunction(List<Vector3> minkowskiDifference, Vector3 supportAxis)
    { 
 
        return minkowskiDifference.Aggregate((a, b) => Vector3.Dot(a, supportAxis) > Vector3.Dot(b, supportAxis) ? a : b);
 
    }

    private bool GJK(List<Vector3> minkowskiDifference)
    {
        var simplex = new List<Vector3>();

        //Create simplex triangle by first picking an arbitrary value, picking the second point using the support fxn, and picking the third point by using the support fxn w/ the orthogonal vector of simplex

        //Assign direction to an arbitrary direction
        Vector3 direction = Vector3.zero;
        while (true){
            switch (simplex.Count)
            {
                case 0:
                    simplex.Add(supportFunction(minkowskiDifference, Vector3.forward));
                    direction = Vector3.forward;

                    if (Vector3.Dot(simplex[simplex.Count - 1] - Vector3.zero, direction) <= 0)
                        return false;

                    break;
                case 1:
                    direction *= -1;

                    simplex.Add(supportFunction(minkowskiDifference, direction));

                    if (Vector3.Dot(simplex[simplex.Count - 1] - Vector3.zero, direction) <= 0)
                        return false;

                    break;
                case 2:
                    var firstPointToSecondPoint = simplex[1] - simplex[0];
                    var firstPointToOrigin = -1 * simplex[0];

                    var perpLine = tripleCrossProduct(firstPointToSecondPoint, firstPointToOrigin, firstPointToSecondPoint);

                    simplex.Add(supportFunction(minkowskiDifference, perpLine));

                    Debug.DrawLine(simplex[0], simplex[1], Color.white);

                    direction = perpLine;

                    Debug.DrawLine(simplex[0], simplex[1], Color.white);
                    Debug.DrawLine(simplex[0], simplex[2], Color.white);
                    Debug.DrawLine(simplex[1], simplex[2], Color.white);

                    if (Vector3.Dot(simplex[simplex.Count - 1] - Vector3.zero, direction) <= 0)
                        return false;

                    break;
                case 3:
                    var v1 = simplex[0] - simplex[2];
                    var v2 = simplex[1] - simplex[2];
                    var toOrigin = -1 * simplex[2];

                    var v1Perp = tripleCrossProduct(v2, v1, v1);
                    var v2Perp = tripleCrossProduct(v1, v2, v2);

                    if (Vector3.Dot(v1Perp, toOrigin) > 0)
                    {
                        //direction = v1Perp;
                        simplex.Remove(simplex[1]);
                    }
                    else if (Vector3.Dot(v2Perp, toOrigin) > 0)
                    {
                        //direction = v2Perp;
                        simplex.Remove(simplex[0]);
                    }
                    else
                    {
                        return true;
                    }
                    break;
            }

            
           

        }

            
        
    }


    #endregion




    private bool CheckCollision(PrismCollision collision)
    {
        var prismA = collision.a;
        var prismB = collision.b;


        // Calculate Minkowski Difference

        var minkowskiDifference = calculateMinkowskiDifference(prismA, prismB);

        //Run GJK Algorithm
        var isCollision = GJK(minkowskiDifference);

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
