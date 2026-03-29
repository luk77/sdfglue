# SdfGlue
[![Build SdfGlue](https://github.com/luk77/sdfglue/actions/workflows/build-sdfglue.yml/badge.svg)](https://github.com/luk77/sdfglue/actions/workflows/build-sdfglue.yml)

**SdfGlue** is a tool for creating and modeling complex 3D objects using **Signed Distance Fields (SDF)**.

The main motivation behind this project was to build a convenient environment for experimenting with mathematically defined 3D forms. It also serves as a playground for exploring various rendering techniques based on **raymarching (sphere tracing)**.

A key feature of SdfGlue is its **open and extensible architecture**. The system is designed to be easily extended with custom SDF functions, object composition operators, and rendering components.

The project is heavily inspired by **Shadertoy** and the work of its community. SdfGlue is largely compatible with Shadertoy — generated code can be exported and reused within that platform.

### About the project status

**SdfGlue** is currently a hobby project developed and maintained by a single person. As such, you may encounter some rough edges while using it.

- Minor bugs may occur — if you run into any issues, feel free to report them in the Issues section.
- Some features are experimental and not yet fully functional (e.g. 4D raymarching, shader export to Unity).
- There is currently no formal documentation. While the core functionality should be relatively intuitive, some areas would benefit from more detailed explanations. Documentation (written or video) is planned for the future.

# Downloads
- [Latest build](https://github.com/luk77/sdfglue/releases/tag/latest)
- [All releases](https://github.com/luk77/sdfglue/releases)

# Features

### Editor Features
- camera controls similar to popular 3D tools (e.g. Blender, Unity)
- real-time parameter editing with live preview
- adjustable preview resolution
- customizable layout
- easy integration of custom SDF functions
- GLSL code generation preview
- debugging tools (normals, iteration count, SDF visualization)

### SDF Modeling Features
- rich library of SDF primitives
- hierarchical scene structure
- operators for combining and transforming objects
- boolean and smooth blending operators
- material blending


# Gallery
TODO...


# Requirements
- **Operating System**: Windows (tested on Windows 11)
- **.NET:** .NET 8.0 SDK (Windows-only)  
- **Graphics API**: OpenGL 3.3 or higher
- **GPU**: A graphics card with OpenGL 3.3 support (integrated GPUs may work, but were not extensively tested)

*Note: The application has not been thoroughly tested across different hardware configurations. If you encounter compatibility issues, they are most likely related to GPU drivers or incomplete OpenGL support.*


# Building from source
To build the application, you need **Visual Studio 2022**.

Simply clone the repository, open the solution (```src/SdfGlue.sln```) in Visual Studio, and build it.

The project has several dependencies (mainly **Dear ImGui** and **OpenTK**) which should be automatically restored via NuGet during the build process.

The application also requires additional configuration files (primarily XML and GLSL files) located in the ```bin\\``` directory. After building, the generated .exe and .dll files should be copied into this directory.

To automate the build and file copying process, you can use the provided script: ```src\publish.cmd```

To run the application directly from Visual Studio:
- set **SdfGlueEditor** as the *Startup Project*
- set the *Working Directory* for the **SdfGlueEditor** project to the ```bin/``` folder


# Credits
**SdfGlue** is created and maintained by [Łukasz Lesicki](https://github.com/luk77).

Special thanks to the following tools, resources, and communities:
- **[Dear ImGui](https://github.com/ocornut/imgui)** — immediate-mode GUI framework  
- **[OpenTK](https://opentk.net/)** — OpenGL bindings for .NET  
- **[ImGui.NET OpenTK Sample](https://github.com/NogginBops/ImGui.NET_OpenTK_Sample)** — reference for ImGui + OpenTK integration  
- **[Shadertoy](https://www.shadertoy.com/)** — inspiration and reference platform for SDF and raymarching  
- **[Inigo Quilez (iq)](https://iquilezles.org/)** — foundational work on SDFs and raymarching  
- **[LearnOpenGL](https://learnopengl.com/)** — OpenGL tutorials and rendering knowledge base  


# License
This project is licensed under the **MIT License**. See the [LICENSE.md](LICENSE.md) file for details.