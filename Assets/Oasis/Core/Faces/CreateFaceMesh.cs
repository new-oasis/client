using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class CreateFaceMesh : MonoBehaviour
{


    void Start()
    {

        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0)
        };

        mesh.triangles = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };

        // mesh.normals = new Vector3[4]
        // {
        //     -Vector3.forward,
        //     -Vector3.forward,
        //     -Vector3.forward,
        //     -Vector3.forward
        // };
        mesh.RecalculateNormals();

        mesh.uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        // AssetDatabase.CreateAsset( mesh, "Assets/mesh_face_1x1" );
        // AssetDatabase.SaveAssets();

        GetComponent<MeshFilter>().mesh = mesh;
    }


}
