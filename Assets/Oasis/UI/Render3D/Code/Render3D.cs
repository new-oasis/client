using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;
using System.Threading.Tasks;

public class Render3D : MonoBehaviour
{
    private static Render3D _instance;
    public static Render3D Instance { get { return _instance; } }

    public static bool busy;
    public Camera cam;
    EntityManager em;

    public RenderTexture renderTexture;


    public IEnumerator Snapshot(Entity entity, System.Action<Texture2D> callback)
    {
        yield return WaitIfBusy();
        EntityHelpers.SetLayers(entity, "Render3D");
        yield return null;
        Texture2D texture = Snapshot();
        callback(texture);
        Render3D.busy = false;
    }

    IEnumerator WaitIfBusy()
    {
        while (Render3D.busy)
            yield return null;

        Render3D.busy = true;
        yield return null;
    }

    public Texture2D Snapshot()
    {
        cam.targetTexture = renderTexture;
        cam.Render();

        // Copy temp renderTexture
        RenderTexture saveActive = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;
        Texture2D texture = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.ARGB32, false, true);
        texture.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = saveActive;

        // Relase temporary
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(cam.targetTexture);
        return texture;
    }

    void SetPosition(GameObject go)
    {
        go.transform.position = new Vector3(0, 0, 0);
    }

    void PositionCamera(Bounds bounds)
    {
        float cameraDistance = 2.0f; // Constant factor

        //
        Vector3 objectSizes = bounds.max - bounds.min;

        float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);

        // Visible height 1 meter in front
        float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView);

        // Combined wanted distance from the object
        float distance = cameraDistance * objectSize / cameraView;

        // Estimated offset from the center to the outside of the object
        distance += 0.5f * objectSize;

        cam.transform.position = bounds.center - distance * cam.transform.forward;
    }


    private void Awake()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
    }


}
