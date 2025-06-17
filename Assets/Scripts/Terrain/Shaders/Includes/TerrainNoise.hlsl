#include "FastNoiseLite.hlsl"
#include "Splines.hlsl"

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
    fnl_state erosionNoise = fnlCreateState(continentalnessNoise.seed + continentalnessNoise.octaves);
    erosionNoise.frequency = 0.00005;
    erosionNoise.noise_type = FNL_NOISE_OPENSIMPLEX2S;
    erosionNoise.fractal_type = FNL_FRACTAL_FBM;
    erosionNoise.octaves = 4;
    erosionNoise.lacunarity = 2.0;
    erosionNoise.gain = 0.5;
    erosionNoise.weighted_strength = 0.0;
    float erosionValue = fnlGetNoise3D(erosionNoise, p.x, p.y, p.z);
    
    // creates peaks and valleys noise
    fnl_state PVNoise = fnlCreateState(erosionNoise.seed + erosionNoise.octaves);
    PVNoise.frequency = 0.0002;
    PVNoise.noise_type = FNL_NOISE_OPENSIMPLEX2S;
    PVNoise.fractal_type = FNL_FRACTAL_RIDGED;
    PVNoise.octaves = 4;
    PVNoise.lacunarity = 2.0;
    PVNoise.gain = 0.3;
    PVNoise.weighted_strength = -0.4;
    float PVValue = fnlGetNoise3D(PVNoise, p.x, p.y, p.z);
    
    // combines the noise values
    float values[MAX_NOISE_MAPS] = { continentalnessValue, erosionValue, PVValue };
    return sampleSplines(values, planetSplines);
}