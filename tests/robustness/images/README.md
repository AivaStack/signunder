# Robustness test images

Photographs from NASA's image library (images.nasa.gov), downloaded 13 October 2026.
NASA's media guidelines say its imagery is generally not copyrighted (*from memory*,
UNVERIFIED for each item); the credit lines are as the library gives them.

| File | NASA id | Title | Credit |
|---|---|---|---|
| `earth.jpg` | as17-148-22727 | The Earth seen from Apollo 17 | NASA |
| `aurora.jpg` | iss072e159172 | The aurora borealis blankets the Earth | NASA (ISS Expedition 72) |
| `earth-observation.jpg` | iss052e004998 | Earth observation taken by the Expedition 52 crew | NASA / Jack Fischer |
| `mars.jpg` | PIA08712 | Shadows Draw Attention to Features of Mars Landscape | NASA/JPL-Caltech/Cornell |

They are real photographs, not model output. `robustness.py` adds three synthetic
images (a gradient, a text graphic and a flat colour) at run time.
