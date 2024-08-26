# 68.0.0

### Features
* Add occlusion cutout shaders for both BiRP and URP to support occlusions for UI

# 67.0.0

### Features
* Depth API is now part of Meta Core SDK and most of the scripts and shader libraries required for Depth API now live in there. For future updates and changes, refer to the [Meta Core SDK release notes](https://developer.oculus.com/downloads/package/meta-xr-core-sdk/)
* Depth API implementation has been significantly overhauled both on the C# scripting side and the shader libraries side. More info [here](https://developer.oculus.com/documentation/unity/unity-depthapi/)
* This package, along with `com.meta.xr.depthapi.urp` are now no longer required to support depth, but will continue to supply useful shaders and will be supported alongside the Depth API implementation from Meta Core SDK.
* Both packages have been adapted to support the Depth API implementation present in Meta Core SDK.

### Improvements
* Depth textures are now supplied using a new method from the system level. This results in better looking and significantly more optimized occlusions.
* Simplified and more straightforward API.

# 61.0.0

* Update xr.oculus package dependency to v 4.2.0

### Fixes

* Add a listener to USE_SCENE permission and only enable depth once it is granted
* Fix shadergraph breaking on newer versions of Unity

# 60.0.0 (2023-12-21)

### Features

* Add hands removal feature
* Add shadergraph support by adding an occlusion subgraph

### Improvements

* Refactor the depth texture provider code
* Add an event for depth texture availability

### Bug Fixes

* Fix multiple issues when running over Meta Quest Link

# 0.2.0 (2023-11-16)

### Features

* Implement "6DOF reprojection into depth space" method
* Add Environment Depth Bias to solve z-fighting and enable flicker-free rendering of content close to surfaces.

### Bug Fixes
* Shaders now render in editor even after hitting stop
* PST now checks for the correct Unity version

## 0.1.1 (2023-10-27)

### Bug Fixes

* Remove v57 package dependency

# 0.1.0 (2023-10-11)

### Features

* Initial commit
