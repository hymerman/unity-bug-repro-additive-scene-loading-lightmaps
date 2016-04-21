# unity-bug-repro-additive-scene-loading-lightmaps
Repro project for a series of Unity bugs to do with additive scene loading and lightmap/lighting problems

## What is this?
This is a project to reproduce a bug in Unity to do with additive scene loading and lightmap/lighting problems, in standalone builds.

This project contains three small scenes with different lightmap data, and a little GUI with buttons to load and unload these scenes additively, and set them as the active scene.

Each scene is at a different point in space and the main camera is set up to frame them all, to show the problems clearly.

## Repro steps
- Build this Unity project in Windows Standalone (any bitness, dev or not). Versions 5.3.4f1 and 5.4.0b14 tested.
- Run it
- Click the buttons in this order:
  1. Load A
  2. Load B
  3. Set B active
  4. Load C
  5. Set C active

## Expected behaviour?
After step 5 I would expect that scene C would be shown and have correct lighting, and scenes A and B would be visible but may or may not have correct lighting (but preferably correct!).

## Actual behaviour
Scene C is visible but seems to be using the wrong lightmaps, as its plane is textured with a bunch of red squares and the objects are all flat (and wrong!) colours.

Scenes A and B are also messed up (with nasty-looking corruption) but I don't have much expectation that they would look ok, if they're not active and they have different lighting to scene C.

It *is* possible to get the correct scene lighting but I can't figure out the logic behind the ordering of loading/unloading/setting active to achieve this - I just arrive at it after random clicking.

All of this works fine in the editor - it's only in standalone builds that it goes wrong.

My actual case involves much more complex scenes, and the corruption is much worse - it doesn't just look like the wrong lightmaps are used, but that the lightmaps themselves are corrupted - there are many fully saturated green and white blobs which don't appear in any of the lightmap images.
