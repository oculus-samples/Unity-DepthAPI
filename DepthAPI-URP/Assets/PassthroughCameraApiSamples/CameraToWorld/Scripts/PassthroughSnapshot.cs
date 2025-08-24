/*
 *  PassthroughSnapshot.cs
 *  Attach this to any active GameObject in the scene that also contains
 *  – or has a reference to – a WebCamTextureManager component.
 *
 *  Requires:
 *  • Quest 3 / 3 S running Horizon OS 74+
 *  • android.permission.CAMERA  +  horizonos.permission.HEADSET_CAMERA
 *    (already handled by the sample’s manifest) :contentReference[oaicite:0]{index=0}
 */

using System;
using System.Collections;
using System.IO;
using PassthroughCameraSamples;
using UnityEngine;

public class PassthroughSnapshot : MonoBehaviour
{
    [Tooltip("Reference the WebCamTextureManager in your scene. " +
             "If left null, the script will try to find one at runtime.")]
    public WebCamTextureManager webcamManager;

    [Header("Input")]
    [SerializeField] private OVRInput.RawButton _saveSnapshotButton = OVRInput.RawButton.A;

    private void Update()
    {
        if (OVRInput.GetDown(_saveSnapshotButton)) SaveCurrentFrame();
    }

    public void SaveCurrentFrame()
    {
        // Copy pixels into a Texture2D
        Texture2D tex = new Texture2D(webcamManager.WebCamTexture.width, webcamManager.WebCamTexture.height, TextureFormat.RGBA32, false);
        tex.SetPixels32(webcamManager.WebCamTexture.GetPixels32());
        tex.Apply(false, false);

        // Encode to PNG
        byte[] png = tex.EncodeToPNG();
        Destroy(tex);

        // Build a timestamped filename inside persistentDataPath
        string filename = $"PassthroughSnapshot_{webcamManager.Eye}_{Time.frameCount}.png";
        string fullPath = Path.Combine(Application.persistentDataPath, filename);

        // Write the file
        File.WriteAllBytes(fullPath, png);

        Debug.Log($"PassthroughSnapshot: Saved to {fullPath}");
    }
}
