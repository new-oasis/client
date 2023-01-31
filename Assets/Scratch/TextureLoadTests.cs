using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureLoadTests : MonoBehaviour
{

    void Start()
    {
        // var texture = (Texture2D)Resources.Load("1.17.1/textures/bedrock");
        // Debug.Log($"Texture found: {texture == null}");
        // Debug.Log($"Texture format: {texture.format}"); // DXT1.  Compressed

        StartCoroutine(LoadImage());
    }

    IEnumerator LoadImage()
    {
        WWW www = new WWW("file://" + Application.dataPath + "/Resources/1.17.1/textures/bedrock.png");
        yield return www;
        Texture2D texture = new Texture2D(1,1);
        www.LoadImageIntoTexture(texture);

        Debug.Log($"Texture found: {texture == null}");
        Debug.Log($"Texture format: {texture.format}"); // ARGB32
    }

}
