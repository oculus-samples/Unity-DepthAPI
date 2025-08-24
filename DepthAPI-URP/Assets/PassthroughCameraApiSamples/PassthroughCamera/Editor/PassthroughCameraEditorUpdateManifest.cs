// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Xml;
using Meta.XR.Samples;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PassthroughCameraSamples.Editor
{
    [MetaCodeSample("PassthroughCameraApiSamples-PassthroughCamera")]
    public class PassthroughCameraEditorUpdateManifest : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            UpdateAndroidManifest();
        }

        private void UpdateAndroidManifest()
        {
            var pcaManifestPermission = "horizonos.permission.HEADSET_CAMERA";
            var pcaManifestPassthroughFeature = "com.oculus.feature.PASSTHROUGH";
            var manifestFolder = Application.dataPath + "/Plugins/Android";
            try
            {
                // Load android manfiest file
                var doc = new XmlDocument();
                doc.Load(manifestFolder + "/AndroidManifest.xml");

                string androidNamepsaceURI;
                var element = (XmlElement)doc.SelectSingleNode("/manifest");
                if (element == null)
                {
                    throw new OperationCanceledException("Could not find manifest tag in android manifest.");
                }

                // Get android namespace URI from the manifest
                androidNamepsaceURI = element.GetAttribute("xmlns:android");
                if (!string.IsNullOrEmpty(androidNamepsaceURI))
                {
                    // Check if the android manifest has the Passthrough Feature enabled
                    var nodeList = doc.SelectNodes("/manifest/uses-feature");
                    var noPT = true;
                    foreach (XmlElement e in nodeList)
                    {
                        var attr = e.GetAttribute("name", androidNamepsaceURI);
                        if (attr == pcaManifestPassthroughFeature)
                        {
                            noPT = false;
                            break;
                        }
                    }
                    if (noPT)
                    {
                        throw new OperationCanceledException("To use the Passthrough Camera Access Api you need to enable Passthrough feature.");
                    }
                    else
                    {
                        // Check if the android manifest already has the Passthrough Camera Access permission
                        nodeList = doc.SelectNodes("/manifest/uses-permission");
                        foreach (XmlElement e in nodeList)
                        {
                            var attr = e.GetAttribute("name", androidNamepsaceURI);
                            if (attr == pcaManifestPermission)
                            {
                                Debug.Log("PCA Editor: Android manifest already has the proper permissions.");
                                return;
                            }
                        }

                        if (EditorUtility.DisplayDialog("Meta Passthrough Camera Access", "\"horizonos.permission.HEADSET_CAMERA\" permission IS NOT PRESENT in AndroidManifest.xml", "Add it", "Do Not Add it"))
                        {
                            element = (XmlElement)doc.SelectSingleNode("/manifest");
                            if (element != null)
                            {
                                // Insert Passthrough Camera Access permission
                                var newElement = doc.CreateElement("uses-permission");
                                _ = newElement.SetAttribute("name", androidNamepsaceURI, pcaManifestPermission);
                                _ = element.AppendChild(newElement);

                                doc.Save(manifestFolder + "/AndroidManifest.xml");
                                Debug.Log("PCA Editor: Successfully modified android manifest with Passthrough Camera Access permission.");
                                return;
                            }
                            throw new OperationCanceledException("Could not find android namespace URI in android manifest.");
                        }
                        else
                        {
                            throw new OperationCanceledException("To use the Passthrough Camera Access Api you need to add the \"horizonos.permission.HEADSET_CAMERA\" permission in your AndroidManifest.xml.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new BuildFailedException("PCA Editor: " + e.Message);
            }
        }
    }
}
