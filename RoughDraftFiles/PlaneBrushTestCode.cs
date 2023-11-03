//This script controls the actual mechanics of the Deform and Erase buttons.
//Written by Maya Daniels


//The 
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System;
using UnityEngine;

public class MeshManipulator : MonoBehaviour, IMixedRealityPointerHandler {

    [Range( 0.05f, 0.1f )]
    public float selectionRadius = 0.075f;

    [SerializeField]
    private ManipulationTypeMenuHandler manipulationTypeHandler;

    [SerializeField]
    private ToggleBrushTypeMenus brushType;

    public Mesh DeformedMesh { get; set; }
    public bool MoveAndRotateActivated { get; set; } = false;
    public bool DeformerActivated { get; set; } = true;

    private class VertexData {
        public Vector3 Position { get; set; }
        public int Index { get; set; }
         
        public VertexData ( Vector3 position, int index ) {
            Position = position;
            Index = index;
        }
    }

    private bool vertexSelected = false;
    private Vector3 selectedVertex, previousHandPosition;
    private int[] nearbyIndices;
    private Vector3[] originalVertices, transformedVertices, nearbyVertices, storedVertices, displacedVertices;
    private VertexData[] nearbyVertexData;
    private PointOctree<VertexData> octree;

    private void Start () {
        CoreServices.InputSystem.RegisterHandler<IMixedRealityPointerHandler>( this );
        
        //The mesh consists of one layer of vertices and usually the normals are pointing inwards. This part of the code reverses the normals so
        //that they are pointing outwards and then a double-sided shader is added to each of the materials to allow all areas of the brace to be seen.
        Mesh reversedMesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = reversedMesh.vertices;
        Vector3[] normals = reversedMesh.normals;
        int[] triangles = reversedMesh.triangles;

        Vector3[] reversedNormals = new Vector3[ normals.Length ];
        int[] reversedTriangles = new int[ triangles.Length ];

        for ( int i = 0; i < vertices.Length; i++ ) {
            reversedNormals[ i ] = -normals[ i ];
        }

        for ( int i = 0; i < triangles.Length; i += 3 ) {
            reversedTriangles[ i ] = triangles[ i ];
            reversedTriangles[ i + 1 ] = triangles[ i + 2 ];
            reversedTriangles[ i + 2 ] = triangles[ i + 1 ];
        }

        DeformedMesh = new() {
            vertices = vertices,
            normals = reversedNormals,
            triangles = reversedTriangles
        };

        //This part is setting the new reversed mesh to be the mesh of the brace.
        GetComponent<MeshFilter>().mesh = DeformedMesh;
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = DeformedMesh;

        originalVertices = storedVertices = DeformedMesh.vertices;

        displacedVertices = new Vector3[ originalVertices.Length ];
        transformedVertices = new Vector3[ originalVertices.Length ];
        for ( int i = 0; i < originalVertices.Length; i++ ) {
            displacedVertices[ i ] = originalVertices[ i ];
            transformedVertices[ i ] = transform.TransformPoint( originalVertices[ i ] );
        }

        //The data partitioning structure used to store all the vertices is called an octree.
        octree = new PointOctree<VertexData>( 100f, transform.position, 0.5f ); // maxSize = 100f, center = transform.position, minSize = 0.5f
        for ( int i = 0; i < transformedVertices.Length; i++ ) {
            octree.Add( new VertexData( transformedVertices[ i ], i ), transformedVertices[ i ] );
        }
    }

    private void OnDestroy () {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>( this );
    }

    //OnPointerDown means the user's index finger and thumb make contact (the down part of the click)
    public void OnPointerDown ( MixedRealityPointerEventData eventData ) {
        if ( !MoveAndRotateActivated ) {
            if ( DeformerActivated ) {
                var pointerResult = eventData.Pointer.Result;
                
                if ( pointerResult.CurrentPointerTarget == gameObject ) {
                    previousHandPosition = pointerResult.StartPoint;
                    Vector3 currentPositionOnSphere = pointerResult.Details.Point;

                    if ( brushType.SphereButtonToggled )
                        selectedVertex = GetSelectedSphereVertices( currentPositionOnSphere, selectionRadius );
                    else if ( brushType.PlaneButtonToggled )
                        selectedVertex = GetSelectedPlaneVertices( currentPositionOnSphere, selectionRadius );
                    
                    if ( float.IsPositiveInfinity( selectedVertex.x ) && float.IsPositiveInfinity( selectedVertex.y ) && float.IsPositiveInfinity( selectedVertex.z ) ) {
                        vertexSelected = false;
                    } else {
                        vertexSelected = true;
                        storedVertices = (Vector3[])displacedVertices.Clone(); //This vector is used for the undo button
                    }
                }
            }
        }
    }

    //This occurs when the user's index finger and thumb are touching and they are moving their hand
    public void OnPointerDragged ( MixedRealityPointerEventData eventData ) {
        if ( vertexSelected && manipulationTypeHandler.DeformMenuActivated ) {
            DeformMesh(eventData);
        } else if (vertexSelected && manipulationTypeHandler.EraseMenuActivated) {
            //EraseVertices();
        }
    }

    //This is when the user's index finger and thumb break contact (the up part of the click)
    public void OnPointerUp ( MixedRealityPointerEventData eventData ) {
        vertexSelected = false;
        RefreshOctree();
    }

    public void OnPointerClicked ( MixedRealityPointerEventData eventData ) { }


    //This function finds the vertex closest to the hitPoint (where the cursor intersects with the brace) and fills an array with all the
    //vertices that are within a certain radius of the closest vertex. The vertices within the array are called the selected vertices.
    public Vector3 GetSelectedSphereVertices (Vector3 hitPoint, float radius) {
        nearbyVertexData = octree.GetNearby( hitPoint, radius );
        nearbyVertices = new Vector3[ nearbyVertexData.Length ];
        nearbyIndices = new int[ nearbyVertexData.Length ];
        for ( int i = 0; i < nearbyVertexData.Length; i++ ) {
            nearbyVertices[ i ] = nearbyVertexData[ i ].Position;
            nearbyIndices[ i ] = nearbyVertexData[ i ].Index;
        }

        if ( nearbyVertices.Length == 0 )
            return Vector3.positiveInfinity;

        //The following code finds the closest vertex
        Vector3 closestVertex = nearbyVertices[ 0 ];
        float minDistanceSqr = ( closestVertex - hitPoint ).sqrMagnitude;

        for ( int i = 1; i < nearbyVertices.Length; i++ ) {
            float distanceSqr = ( nearbyVertices[ i ] - hitPoint ).sqrMagnitude;

            if ( distanceSqr < minDistanceSqr ) {
                minDistanceSqr = distanceSqr;
                closestVertex = nearbyVertices[ i ];
            }
        }
        return closestVertex;
    }

    public Vector3 GetSelectedPlaneVertices (Vector3 hitPoint, float planeLength) {
        //Find the vertex closest to the hitPoint
        //Make a list of all vertices that collide with the BoxCollider?
        nearbyVertexData = octree.GetNearby( hitPoint, planeLength );
        nearbyVertices = new Vector3[ nearbyVertexData.Length ];
        nearbyIndices = new int[ nearbyVertexData.Length ];

        for ( int i = 0; i < nearbyVertexData.Length; i++ ) {
            nearbyVertices[ i ] = nearbyVertexData[ i ].Position;
            nearbyIndices[ i ] = nearbyVertexData[ i ].Index;
        }

        if ( nearbyVertices.Length == 0 )
            return Vector3.positiveInfinity;


        Vector3 closestVertex = nearbyVertices[ 0 ];
        float minDistanceSqr = ( closestVertex - hitPoint ).sqrMagnitude;

        for ( int i = 1; i < nearbyVertices.Length; i++ ) {
            float distanceSqr = ( nearbyVertices[ i ] - hitPoint ).sqrMagnitude;

            if ( distanceSqr < minDistanceSqr ) {
                minDistanceSqr = distanceSqr;
                closestVertex = nearbyVertices[ i ];
            }
        }



        return closestVertex; //placeholder value
    }

    //This functions is responsible for finding the new position of the pulled vertices based on the user's hand position and assigning a weight
    //to each selected vertex depending on their distance to the closest vertex (this creates the bell-like shape).
    public void DeformMesh ( MixedRealityPointerEventData eventData ) {
        Vector3 newHandPosition = eventData.Pointer.Position;
        Vector3 movementVector = newHandPosition - previousHandPosition;
        Vector3[] movedVertexPositions = new Vector3[ nearbyVertices.Length ];

        for ( int i = 0; i < nearbyVertices.Length; i++ ) {
            float weight = Mathf.SmoothStep( 0f, 1f, 1f - Vector3.Distance( selectedVertex, nearbyVertices[ i ] ) / selectionRadius );
            Vector3 weightedMovementVector = movementVector * weight;
            movedVertexPositions[ i ] = nearbyVertices[ i ] + weightedMovementVector;
        }

        MoveVertices( movedVertexPositions );
    }

    //This function actually moves the vertices
    public void MoveVertices ( Vector3[] newPositions ) {
        for ( int i = 0; i < newPositions.Length; i++ ) {
            int index = nearbyIndices[ i ];
            transformedVertices[ index ] = newPositions[ i ];
            displacedVertices[ index ] = transform.InverseTransformPoint( transformedVertices[ index ] );
        }
        DeformedMesh.vertices = displacedVertices;
        DeformedMesh.RecalculateNormals();
        DeformedMesh.RecalculateBounds();
    }

    public void EraseVertices ( Vector3[] erasedVertices ) {
        //Delete vertices from mesh
    }

    //This function refreshes the data structure when vertices are moved.
    private void RefreshOctree () {
        octree = new PointOctree<VertexData>( 100f, transform.position, 0.5f );

        for ( int i = 0; i < displacedVertices.Length; i++ ) {
            transformedVertices[ i ] = transform.TransformPoint( displacedVertices[ i ] );
            octree.Add( new VertexData( transformedVertices[ i ], i ), transformedVertices[ i ] );
        }

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = DeformedMesh;
        DeformedMesh.RecalculateNormals();
        DeformedMesh.RecalculateBounds();
    }

    //This function will undo the last manipulation to the mesh.
    public void UndoChange () {
        displacedVertices = storedVertices;
        DeformedMesh.vertices = displacedVertices;
        DeformedMesh.RecalculateNormals();
        DeformedMesh.RecalculateBounds();
        RefreshOctree();
    }

    //This function will reset the mesh to the initial starting mesh.
    public void ResetMesh () {
        DeformedMesh.vertices = originalVertices;
        displacedVertices = storedVertices = (Vector3[])originalVertices.Clone();
        DeformedMesh.RecalculateNormals();
        DeformedMesh.RecalculateBounds();
        RefreshOctree();
    }
}