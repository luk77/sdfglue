//---------------------------------------------------------------------------
// Math constants
//---------------------------------------------------------------------------
#define PI                      3.14159265
#define TWO_PI                  (2*PI)
#define TAU                     (2*PI)

// Golden ratio (https://en.wikipedia.org/wiki/Golden_ratio)
#define PHI                     (sqrt(5)*0.5 + 0.5)

// 'Infinite' distance returned by SDF functions (e.g. an empty object). Independent of the renderer's MAX_DIST
// (a compilation parameter of renderers that ray march), but greater than any reasonable MAX_DIST.
// The value of this parameter, if too large, causes noticeable artifacts. The value below was chosen experimentally.
#define SDFG_FAR_DIST           (5000.0)
