using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CameraController : MonoBehaviour {
    private bool isCameraEnabled = true;
    private float interval = 1.0f; // Interval in seconds

    void Start () {
        StartCoroutine( ToggleCamera() );
    }

    IEnumerator ToggleCamera () {
        while ( true ) {
            yield return new WaitForSeconds( interval );

            if ( isCameraEnabled ) {
                //CameraDevice.Instance.Stop();
                //CameraDevice.Instance.Deinit();
                VuforiaBehaviour.Instance.enabled = false;
            } else {
                //CameraDevice.Instance.Init();
                //CameraDevice.Instance.Start();
                VuforiaBehaviour.Instance.enabled = false;
            }

            isCameraEnabled = !isCameraEnabled;
        }
    }
}

