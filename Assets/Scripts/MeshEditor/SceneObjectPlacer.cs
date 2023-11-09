//This script is responsible for placing the brace and aligned image and patient model in the required position in the MeshEditor scene.
//Written by Maya Daniels

using UnityEngine;

public class SceneObjectPlacer : MonoBehaviour {

    [SerializeField]
    private GameObject image;

    [SerializeField]
    private GameObject scoliosisBrace;

    [SerializeField]
    private GameObject patientModel;

    private void Start () {
        if (ObjectPositionManager.Instance != null) {
            image.transform.SetLocalPositionAndRotation( ObjectPositionManager.Instance.objectPositions[ 0 ], ObjectPositionManager.Instance.objectRotations[ 0 ] );
            //.transform.localScale = ObjectPositionManager.Instance.objectScales[ 0 ];

            patientModel.transform.SetLocalPositionAndRotation( ObjectPositionManager.Instance.objectPositions[ 1 ], ObjectPositionManager.Instance.objectRotations[ 1 ] );
            //patientModel.transform.localScale = ObjectPositionManager.Instance.objectScales[ 1 ];
        }

        
        //patientModel.transform.SetParent( scoliosisBrace.transform );
    }
}
