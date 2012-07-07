sailcs-codebase
===============
Archive of C# code that was part of my System for Automated Interactive
Lighting (SAIL) [dissertation](https://etda.libraries.psu.edu/catalog/9361)
work (2009).

Licenses
--------
- original code is licensed under the
  [MIT License](http://www.opensource.org/licenses/mit-license.html) and
  is Copyright (c) 2009-2012 Joseph A. Zupko.
- Environment and character art in [`sail_demo/media/1930_room`](https://github.com/jzupko/sailcs-codebase/tree/main/sail_demo/media/1930_room)
  is used with permission and is Copyright (c) 2009 [David Milam](http://www.sfu.ca/~dma35/cv.html).

Overview
--------
This codebase includes the demo implementation described and used as part of my
System for Automated Interactive Lighting (SAIL) [dissertation](https://etda.libraries.psu.edu/catalog/9361)
work. This is the version of SAIL that was used for experiment sessions.
It is implemented in C#.

This codebase is only known to compile with
`Visual Studio 2008 Express Edition` under `Windows XP 32-bit`.

Modules
-------
- [`doc`](https://github.com/jzupko/sailcs-codebase/tree/main/doc) - Doxygen generated docs will be created when the Release build is
        compiled. Output is placed in this folder.
- [`jz/physics`](https://github.com/jzupko/sailcs-codebase/tree/main/jz/physics) - unfinished sequential impulse physics library.
               Used by `sail_demo` for collision.
- [`jz_physics_test`](https://github.com/jzupko/sailcs-codebase/tree/main/jz_physics_test) - test application for `jz/physics`.
- [`sail`](https://github.com/jzupko/sailcs-codebase/tree/main/sail) - image processing and learning components of my dissertation research
         SAIL.
- [`sail_demo`](https://github.com/jzupko/sailcs-codebase/tree/main/sail_demo) - demo application, demos SAIL and also implements the artifact
              demo used as part of qualitative interviews in my dissertation.
- [`sail_object_generator`](https://github.com/jzupko/sailcs-codebase/tree/main/sail_object_generator) - incomplete application that was going to be used to
                          generate random objects for testing.
- [`sail_trainer`](https://github.com/jzupko/sailcs-codebase/tree/main/sail_trainer) - application that is used to generate the data set that is
                 used by SAIL at runtime to light an object.
- [`siat_xna/siat`](https://github.com/jzupko/sailcs-codebase/tree/main/siat_xna/siat) - core math and utility code.
- [`siat_xna/siat_cb`](https://github.com/jzupko/sailcs-codebase/tree/main/siat_xna/siat_cb) - unfinished "content builder", meant to allow for building
                     content without using a Visual Studio project.
- [`siat_xna/siat_xna_cp`](https://github.com/jzupko/sailcs-codebase/tree/main/siat_xna/siat_xna_cp) - content processing, includes an implementation of a
                         COLLADA DOM and processor to convert scene data from
                         [COLLADA 1.4.1](http://www.khronos.org/collada/) into
                         a binary format used by `siat_xna/siat_xna_engine`.
- [`siat_xna/siat_xna_engine/render`](https://github.com/jzupko/sailcs-codebase/tree/main/siat_xna/siat_xna_engine/render) - real-time rendering, including dynamic
                                    lighting (both forward and deferred),
                                    materials, and effects.
- [`siat_xna/siat_xna_engine/scene`](https://github.com/jzupko/sailcs-codebase/tree/main/siat_xna/siat_xna_engine/scene) - real-time scene management, including a
                                   scene graph, hardware occlusion query based
                                   kd-Tree visibility management, portal-cell
                                   visibility management, cameras, sky boxes,
                                   and animated and static meshes.
