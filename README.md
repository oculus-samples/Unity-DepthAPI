![Depth API](./Media/DepthOppie.png)
# Depth API
Depth API is a new feature that exposes to applications real-time, per-eye, per-frame, environment depth estimates from the headset’s point of view.
This repository shows how Depth API can be used to implement dynamic occlusions.
This technique is essential for rendering believable visual interactions between virtual and physical content in your apps. This enables a more coherent sense of reality for your users.

You can learn more about Depth API [here](https://developer.oculus.com/experimental/scene-depth-unity/).

## Health & Safety Guidelines
While building mixed reality experiences, we highly recommend evaluating your content to offer your users a comfortable and safe experience. Please refer to the [Mixed Reality H&S Guidelines](https://developer.oculus.com/resources/mr-health-safety-guideline/) before designing and developing your app using this sample project or any of our Presence Platform Features.

# Dynamic Occlusion
This repository contains two occlusion implementations: hard occlusion and soft occlusion.

* Hard occlusion is cheaper to compute, but has a jagged edge and more visible temporal instability.
* Soft occlusion is visually more appealing, but requires more GPU.

![Occlusion Modes](./Media/OcclusionModes.gif "Hard and Soft Occlusion Modes")

You can learn more about occlusion and general guidelines for when and how to use the feature [here](https://developer.oculus.com).

## DepthAPI Requirements
* **2022.3.1 and higher**
* **2023.2 and higher**
* **Meta XR Core SDK (v60.0.0 or above)** by using one of the following methods:
  * You may get it from [the asset store](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169#releases).
  * Import the **com.meta.xr.sdk.core (v60.0.0 or above)** package from Unity's package manager. Follow the instructions on importing individual SDKs in Unity outlined [on the official oculus documentation](https://developer.oculus.com/documentation/unity/unity-package-manager/).
* **Unity XR Oculus** package version - **4.2.0-exp-env-depth.2**
* Oculus Quest 3

# Getting started with samples

### Using github
First, ensure you have Git LFS installed by running this command:
```sh
git lfs install
```

Then, clone this repo using the **Code** button above, or this command:
```sh
git clone https://github.com/oculus-samples/Unity-DepthAPI
```

### Unity Projects
[Use our Unity Set Up guide to setup Unity](https://developer.oculus.com/documentation/unity/book-unity-gsg/). See the Requirements section above for minimum versions of engine supported.

Open one of two sample projects, DepthAPI-BiRP or DepthAPI-URP, located in the cloned Unity-DepthAPI folder.
Both implement the same examples, and exist to showcase different rendering pipelines that Unity offers: Built-in and URP respectively.
Use [Unity's render pipeline overview article](https://docs.unity3d.com/Manual/render-pipelines-overview.html) to understand differences.

### Scenes
The scenes are located in the same path for both sample projects: `./Assets/DepthAPISample/Scenes`.

#### OcclusionToggler
This scene showcases the general setup of the occlusion feature and how to switch between different modes.

When the scene is built for device, users can press the A button on the right controller to toggle between three modes: NoOcclusion, HardOcclusion and SoftOcclusion.

<img src="./Media/OcclusionToggle.gif" alt="Occlusion Mode Per Object" width="600" height="600" />

#### PerObjectOcclusion
In this scene, we show a particular setup where each object has different behaviour for occlusions.

<img src="./Media/occlusionPerObject.gif" alt="Occlusion Mode Per Object" width="600" height="600" />

#### SceneAPIPlacement
This example covers the solution for z-fighting between virtual objects and environment depth when they are placed close to real surfaces. The scene utilizes Scene API to enable the precise placement of virtual posters on detected walls. This is a worst case scenario for z-fighting and the best example to show how it's mitigated.
Supplied shaders contain a float _EnvironmentDepthBias property that can be changed from the code and the SceneAPIPlacement scene showcases how to use it.
Depth API measurements have an inherent error that also scales with distance. The environment depth bias formula is implemented accordingly. This means that the value that is supplied to _EnvironmentDepthBias stands for virtual depth offset by 1 unit distance away from the camera. Offset is calculated towards the camera; a higher value means the virtual object will be brought closer to the camera. _EnvironmentDepthBias is scaling linearly with metric distance. We recommend using the value of 0.06 but the value might depend on the type of content that is placed.

<img src="./Media/SceneAPIPlacement.gif" alt="SceneAPIPlacement" width="600" height="600" />

#### HandsRemoval
This scene showcases the removal of hands from the depth map. In their stead we use hand tracking to render OVRHands and we use them as a mask.

<img src="./Media/HandsRemoval.gif" alt="handsRemoval" width="600" height="600" />

# Using the `com.meta.xr.depthapi` package

## Getting started with Depth API

### 1.Prerequisites

Make sure you have the supported Unity setup listed above.

Ensure Passthrough is working in your project by following [these instructions](https://developer.oculus.com/documentation/unity/unity-passthrough-gs/).

### 2.Importing Depth API package

To import the Depth API package in Unity, follow these steps:

* For BiRP, the following package needs to be [imported from git url](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```
https://github.com/oculus-samples/Unity-DepthAPI.git?path=/Packages/com.meta.xr.depthapi
```

* If your project uses URP, import an additional package for access to a separate set of shaders. The URL for this package is:
```
https://github.com/oculus-samples/Unity-DepthAPI.git?path=/Packages/com.meta.xr.depthapi.urp
```
> Note: **Please note, for URP, both packages are required.**

### 3.Project Setup Tool

Depth API has several requirements that need to be met before it can work:
* Graphics API needs to be set to Vulkan.
* Stereo Rendering Mode needs to be set to Multiview.
* The Passthrough feature needs to be enabled and integrated into every scene that will use Depth API.
* Android Manifest needs the USE_SCENE permission to be enabled by setting scene support to **Required** in OVRManager.

To aid with this, you can use the Project Setup Tool (PST). This will detect any problems and/or recommendations and provides an easy way to easily fix them. To access this tool you have two options:

* In the bottom right corner of the editor, there is a small Oculus icon. Clicking on it will bring up a menu that lets you access the PST. It also has a notification badge whenever any issues are detected to let you know that a fix is required.

![PST_Tool.png](./Media/PST.png)

* You can also access PST from Unity’s top menu "Oculus > Tools > Project Setup Tool"

Once open, you will be presented with a menu that displays all issues and recommendations for solutions. All outstanding issues need to be fixed before the Depth API can work. **Recommended Items** should be applied as well.

![PST_Window.png](Media/PST_Window.png)

### 4.Adding occlusions to a scene

To add occlusions to your scene we’ve supplied a prefab named **EnvironmentDepthOcclusion** for ease of use. Drag and drop it onto your scene. This prefab is found under `Packages/Depth API/Runtime/Core/Prefabs`.

Once you do this, hit Ctrl-S to save your scene. You now need to add the **Passthrough Feature** to your scene (if you don’t already have it). You may use PST to handle this process automatically by hitting **Fix All** once more. Passthrough is essential for Depth API to function. More info on passthrough can be found in [the official documentation](https://developer.oculus.com/documentation/unity/unity-passthrough-gs/).


### 5.Adding occlusion shaders to our objects

Depth API comes with shaders that need to be applied to materials of objects on which we want occlusions to be applied to. The implementation differs between [rendering pipelines](https://docs.unity3d.com/Manual/render-pipelines.html). This repository includes shaders for URP and BiRP. Continue with the one that is relevant to your project.

#### **For BiRP**
Let’s consider the objects in your scene. If you wish to have them be occluded we need to apply the appropriate shader to their materials. If you have an object with a **Standard** shaded material on it, simply change the shader in the material to **OcclusionStandard**.You can find this shader under **Meta/Depth/BiRP**` when selecting the shader.

![OccludedStandardShader.png](Media/OccludedStandardShader.png)


These are the shaders that come pre-packaged with Depth API for BiRP:

| Unity shader          | Depth API shader               |
|-----------------------|--------------------------------|
| Standard              | Occlusion Standard             |
| ParticleStandardUnlit | OcclusionParticleStandardUnlit |

#### **For URP**
Consider the objects in your scene. If we wish to have them be occluded we need to apply the appropriate shader to their materials. If you have an object that has Unity’s **Lit** shaded material in your scene simply change the shader to the Depth API variant shader called **Occlusion Lit**. You may find this shader under **Meta/Depth/URP/** when selecting the shader.

![OccludedLitShader.png](Media/OccludedLitShader.png)

These are the shaders that come prepackaged with Depth API for URP:

| Unity Shader                          | Depth API shader                                |
|---------------------------------------|-------------------------------------------------|
| Lit                                   | Occlusion Lit                                   |
| Unlit                                 | Occlusion Unlit                                 |
| Simple Lit                            | Occlusion Simple Lit                            |
| Baked Lit                             | Occlusion Baked Lit                             |
| Particles / Unlit (/Lit / Simple Lit) | Occlusion Particles / Unlit (/Lit / Simple Lit) |

### 6.Enabling/configuring occlusions

The **EnvironmentDepthOcclusion** object we added in the previous steps has a component that lets you set occlusion types in your project.

![alt_text](Media/EnablingOcclusions.png "image_tooltip")

### 7. Using Environment depth bias to solve z-fighting in occlusion shaders

Provided BiRP and URP occlusion shaders have a property that controls environment depth bias. As provided shaders reuse Unity's material editors, the property can only be changed through scripts. The package provides a utility script 'OcclusionDepthBias.cs' that lets you easily change the EnvironmentDepthBias property on any game object by simply calling its public DepthBiasAdjust() function.
![alt_text](Media/OcclusionDepthBias.png "image_tooltip")

Alternativelly, you may set the value of "_EnvironmentDepthBias" from any material that has an occlusion shader on it.

```C#
material.SetFloat("_EnvironmentDepthBias", DepthBiasValue);
```
### 8. Using hands removal

The api supports removing hands from the depth map (i.e. your hands will not occlude virtual content from the wrists up). To use this functionality, simply call the `RemoveHands()` function from the depth texture provider.

```C#
        private EnvironmentDepthTextureProvider _depthTextureProvider;

        private void Awake()
        {
            // remove hands from depth map
            _depthTextureProvider.RemoveHands(true);

            // restore hands in depth map
            _depthTextureProvider.RemoveHands(false);
        }
```

### 9. Implementing Occlusion in custom shaders

If you have your own custom shaders you can convert them to occluded versions by applying some small changes to them.

For BiRP, use the following include statement:
```ShaderLab
#include "Packages/com.meta.xr.depthapi/Runtime/BiRP/EnvironmentOcclusionBiRP.cginc"
```
For URP:
```ShaderLab
#include "Packages/com.meta.xr.depthapi/Runtime/URP/EnvironmentOcclusionURP.hlsl"
```
#### Step 1. Add occlusion keywords
```Shaderlab
// DepthAPI Environment Occlusion
#pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION
```
#### Step 2. If the struct already contains world coordinates - skip this step, otherwise, use a special macro to declare the field:
```ShaderLab
struct v2f
{
   float4 vertex : SV_POSITION;


   float4 someOtherVarying : TEXCOORD0;


   META_DEPTH_VERTEX_OUTPUT(1) // the number should stand for the previous TEXCOORD# + 1


   UNITY_VERTEX_INPUT_INSTANCE_ID
   UNITY_VERTEX_OUTPUT_STEREO // required for stereo
};
```
#### Step 3. If the struct already contains world coordinates - skip this step. If not, use a special macro:
```Shaderlab
v2f vert (appdata v) {
   v2f o;

   UNITY_SETUP_INSTANCE_ID(v);
   UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); // required to support stereo

   // v.vertex (object space coordinate) might have a different name in your vert shader
   META_DEPTH_INITIALIZE_VERTEX_OUTPUT(o, v.vertex);

   return o;
}
```
#### Step 4. Calculate occlusions in fragment shader
```Shaderlab
half4 frag(v2f i) {
   UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

   // this is something your shader will return without occlusions
   half4 fragmentShaderResult = someColor;

   // Third field is for environment depth bias. 0.0 means the occlusion will be calculated with depths as they are.
   META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(i, fragmentShaderResult, 0.0);

   return fragmentShaderResult;
}
```
If you already have a world position varyings being passed to your fragment shader, you can use this macro:
```Shaderlab
META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(yourWorldPosition, fragmentShaderResult, 0.0);
```
### 10. Shadergraph

Depth API supports adding occlusions via shadergraph. A subgraph is provided in the API, called 'OcclusionSubGraph', that exposes occlusion value. This subgraph will output the value 0 if the object is occluded and 1 otherwise.

![alt_text](Media/OcclusionSubGraph.png "OcclusionSubgraph")

To aid in its usage, the URP sample project provides some example uses cases:

#### 1. LitOccluded
This is a simple shadergraph that uses the aforementioned subgraph to implement occlusions. It works by multiplying the final color's alpha value with the occlusion value. The result will be either the original alpha color if not occluded or it will be 0 if the object is occluded. To occlude the object, we feed in this value in the final alpha value of the fragment shader, enable alpha culling and set the threshold to 0.

![alt_text](Media/LitOccludedShaderGraph.png "LitOccludedShaderGraph")

#### 2. Stylized shaders
Once an object is occluded we can alternatively apply various effects to it rather than clip it from view. For instance, the stylized occlusion shadergraph example makes occluded items "wavy" when occluded.

![alt_text](Media/StylizedOcclusion.gif "StylizedOcclusion")


### 11.Testing
Build the app and install it on a Quest 3. Notice the objects with occluded shaders will have occlusions.

![alt_text](Media/DepthAPICube.gif)

## Licenses
The Meta License applies to the SDK and supporting material. The MIT License applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Meta License applies.
