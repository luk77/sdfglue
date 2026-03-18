# SdfGlue
**SdfGlue** is a tool for creating and modeling complex 3D objects using **Signed Distance Fields (SDF)**.

The main motivation behind this project was to build a convenient environment for experimenting with mathematically defined 3D forms. It also serves as a playground for exploring various rendering techniques based on **raymarching (sphere tracing)**.

A key feature of SdfGlue is its **open and extensible architecture**. The system is designed to be easily extended with custom SDF functions, object composition operators, and rendering components.

The project is heavily inspired by **Shadertoy** and the work of its community. SdfGlue is largely compatible with Shadertoy — generated code can be exported and reused within that platform.


### Build status
[![Build SdfGlue](https://github.com/luk77/sdfglue/actions/workflows/build-sdfglue.yml/badge.svg)](https://github.com/luk77/sdfglue/actions/workflows/build-sdfglue.yml)


### Features

##### Editor Features
- camera controls similar to popular 3D tools (e.g. Blender, Unity)
- rich library of SDF primitives
- real-time parameter editing with live preview
- operators for combining and transforming objects
- easy integration of custom SDF functions
- GLSL code generation preview
- material blending
- debugging tools (normals, iteration count, SDF visualization)

##### SDF Modeling Features
- hierarchical scene structure
- collection of basic primitives and experimental shapes (e.g. gyroids, fractals)
- boolean and smooth blending operators
- domain repetition support


### Gallery
TODO...


### Requirements
- **Operating System**: Windows (tested on Windows 11)
- **.NET / Runtime**: .NET (compatible with Visual Studio 2022 build environment)
- **Graphics API**: OpenGL 3.3 or higher
- **GPU**: A graphics card with OpenGL 3.3 support (integrated GPUs may work, but were not extensively tested)

⚠️ Note: The application has not been thoroughly tested across different hardware configurations. If you encounter compatibility issues, they are most likely related to GPU drivers or incomplete OpenGL support.


### Building from source
To build the application, you need **Visual Studio 2022**.

Simply clone the repository, open the solution (```src/SdfGlue.sln```) in Visual Studio, and build it.

The project has several dependencies (mainly **Dear ImGui** and **OpenTK**) which should be automatically restored via NuGet during the build process.

The application also requires additional configuration files (primarily XML and GLSL files) located in the bin directory. After building, the generated .exe and .dll files should be copied into this directory.

To automate the build and file copying process, you can use the provided script: ```src\publish.cmd```

To run the application directly from Visual Studio:
- set **SdfGlueEditor** as the Startup Project
- set the Working Directory for the **SdfGlueEditor** project to the bin/ folder


### Credits
This project was made possible thanks to several amazing tools, libraries, and communities:
- **Dear ImGui** — for providing an excellent immediate-mode GUI framework
- **OpenTK** — for OpenGL bindings and cross-platform windowing
- **Shadertoy** — for inspiration and as a reference platform for SDF and raymarching techniques
- **Inigo Quilez (iq)** — for pioneering work on SDFs, raymarching, and countless invaluable articles and examples (https://iquilezles.org/
- )
- The broader demogroup / shader community — for sharing knowledge, techniques, and inspiration

Additional inspiration and references:
- https://iquilezles.org/articles/
- https://www.shadertoy.com/
- various discussions and resources from graphics programming forums and communities


### License
This project is licensed under the **MIT License**. For details see **LICENSE.md** file.