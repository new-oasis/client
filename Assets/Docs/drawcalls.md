# Targets
- mobile < 200 
- desktop < 2000


## Main tread
Generate high-level-rendering-commands.

WaitForPresentOnGfxThread marker is CPU waiting on GPU to finish rendering.

## Render thread
Generate low-level-graphics-commands


## Classic
### Stats Panel
- Batches == draw calls
- Set Pass == expensive draw calls
### Optimizing 
- Same material
- if static => Use static batching
- if same mesh => GPU instancing
- dynamic batch'able?


https://thegamedev.guru/static/6800712e82b3e1cbfe792e56379efbee/c935f/Unity-Draw-Call-Reduction-Diagram-1.webp

## TODO
Create shared mesh if many faces.
Use MeterialProperty with face texture indexes.