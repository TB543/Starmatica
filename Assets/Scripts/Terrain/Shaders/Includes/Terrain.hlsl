#include "FastNoiseLite.hlsl"

// struct to represent a spline with points represented by (key, value) and count to
// denote number of points, only 16 points allowed
struct Spline
{
    float keys[16];
    float values[16];
    int count;
};

// spline for continentalness
static const Spline continentalnessSpline = {
    { -1, -.15, -.1, 0, .15, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { -1, -1, -.1, 0, .1, .1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    6
};

// spline for erosion
static const Spline erosionSpline =
{
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    1
};

// spline for peaks and valleys
static const Spline PVSpline =
{
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    1
};

/*
* Samples a piecewise linear spline defined by key-value pairs.
* note: max number of points on spline is 16
*
* @param value The input value to evaluate the spline at.
* @param spline the spline to sample
*
* @return The interpolated value at the given input based on the spline.
*/
float sampleSpline(float value, Spline spline)
{
    // clamp to the ends of the spline
    if (value <= spline.keys[0])
        return spline.values[0];
    if (value >= spline.keys[spline.count - 1])
        return spline.values[spline.count - 1];
    
    // iterate over every point
    for (int i = 0; i < spline.count - 1; i++) {
        float lowerKey = spline.keys[i];
        float higherKey = spline.keys[i + 1];

        // finds the 2 points the value is in between
        if (value >= lowerKey && value <= higherKey) {
            float lowerValue = spline.values[i];
            float higherValue = spline.values[i + 1];
            float normDist = (value - lowerKey) / (higherKey - lowerKey);
            return lerp(lowerValue, higherValue, normDist);
        }
    }
    return 0; // this line will never be reached
}

/*
* gets the height value for a given point and seed for a planet using various noise maps
*
* @param p that point to sample
* @param seed the seed for the planet
*
* @return the noise value at that given point
*/
float planetHeight(float3 p, int seed)
{
    // creates continentalness noise
    fnl_state continentalnessNoise = fnlCreateState(seed);
    continentalnessNoise.frequency = .0001;
    continentalnessNoise.noise_type = FNL_NOISE_OPENSIMPLEX2S;
    continentalnessNoise.fractal_type = FNL_FRACTAL_FBM;
    continentalnessNoise.octaves = 5;
    continentalnessNoise.lacunarity = 2.0;
    continentalnessNoise.gain = 0.5;
    continentalnessNoise.weighted_strength = 0.0;
    float continentalnessValue = fnlGetNoise3D(continentalnessNoise, p.x, p.y, p.z);
    
    // creates erosion noise
    fnl_state erosionNoise = fnlCreateState(seed);
    erosionNoise.frequency = 0.00005;
    erosionNoise.noise_type = FNL_NOISE_OPENSIMPLEX2S;
    erosionNoise.fractal_type = FNL_FRACTAL_FBM;
    erosionNoise.octaves = 4;
    erosionNoise.lacunarity = 2.0;
    erosionNoise.gain = 0.5;
    erosionNoise.weighted_strength = 0.0;
    float erosionValue = fnlGetNoise3D(erosionNoise, p.x, p.y, p.z);
    
    // creates peaks and valleys noise
    fnl_state PVNoise = fnlCreateState(seed);
    PVNoise.frequency = 0.0002;
    PVNoise.noise_type = FNL_NOISE_OPENSIMPLEX2S;
    PVNoise.fractal_type = FNL_FRACTAL_RIDGED;
    PVNoise.octaves = 4;
    PVNoise.lacunarity = 2.0;
    PVNoise.gain = 0.3;
    PVNoise.weighted_strength = -0.4;
    float PVValue = fnlGetNoise3D(PVNoise, p.x, p.y, p.z);
    
    // combines noise values to create terrain
    float base = sampleSpline(continentalnessValue, continentalnessSpline);
    // todo apply other noise values
    
    return base;
}