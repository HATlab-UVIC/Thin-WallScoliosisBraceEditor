//This script is responsible for saving the positions of all three objects in the ObjectAlignment scene and loading the MeshEditor scene.
//Written by Maya Daniels

using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadNewScene : MonoBehaviour {

    [SerializeField]
    private GameObject image;

    [SerializeField]
    private GameObject patientModel;

    [SerializeField]
    private Transform brace;


    public void OnYesClicked () {
        SaveObjectTransforms();
        SceneManager.LoadScene( "MeshEditor" );  
    }

    private void SaveObjectTransforms () {
        ObjectPositionManager.Instance.objectPositions[ 0 ] = image.transform.position;
        ObjectPositionManager.Instance.objectPositions[ 1 ] = patientModel.transform.position;

        ObjectPositionManager.Instance.objectRotations[ 0 ] = image.transform.localRotation;
        ObjectPositionManager.Instance.objectRotations[ 1 ] = patientModel.transform.localRotation;

        ObjectPositionManager.Instance.objectScales[ 0 ] = image.transform.localScale;
        ObjectPositionManager.Instance.objectScales[ 1 ] = patientModel.transform.localScale;
    }
}
