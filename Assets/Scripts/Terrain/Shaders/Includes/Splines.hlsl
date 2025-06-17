// constants to denote array sizes
static const int MAX_SPLINE_POINTS = 16;
static const int MAX_SPLINES = 1;
static const int MAX_NOISE_MAPS = 3;

// struct to represent a spline with points represented by (key, value *XOR* childIndex) and count to
// denote number of points, only 16 points allowed.
struct Spline
{
    float keys[MAX_SPLINE_POINTS]; // noise value of the point 
    float values[MAX_SPLINE_POINTS]; // resulting height value of the point
    int childIndices[MAX_SPLINE_POINTS]; // -1 when no child
    int count; // still need array of size MAX_SPLINE_POINTS, so anything past this number will be ignored
};

// a struct to represent a stack frame and some constants to represent the status
static const int GET_BOTH_VALUES = 0;
static const int SET_LOW_VALUE = 1;
static const int SET_HIGH_VALUE = 2;
struct Frame
{
    int splineIndex;
    int lowerPointIndex;
    float normDist;
    int status;
    float lowerValue;
    float higherValue;
};

/*
* pushes a frame onto the stack and initializes it
* 
* @parm noiseValues the array of noise values used to for the frame calculations
* @parm splines the array of splines used for the frame calculations
* @parm stack the stack of frames
* @parm top the index of the top of the stack
* @parm splineIndex the index of the spline to use for the frame calculations
*/
void pushFrame(float noiseValues[MAX_NOISE_MAPS], Spline splines[MAX_SPLINES], inout Frame stack[MAX_NOISE_MAPS], inout int top, int splineIndex)
{
    // gets/sets data in the struct
    stack[++top].splineIndex = splineIndex;
    stack[top].status = GET_BOTH_VALUES;
    float noise = noiseValues[top];
    Spline spline = splines[stack[top].splineIndex];

    // handles noise value lower then lowest spline point
    if (noise <= spline.keys[0]) {
        stack[top].lowerPointIndex = 0;
        stack[top].normDist = 0;
        return;
    }
    
    // handles noise value larger than highest spline point
    if (noise >= spline.keys[spline.count - 1]) {
        stack[top].lowerPointIndex = spline.count - 2;
        stack[top].normDist = 1;
        return;
    }

    // iterate over every point
    for (int i = 0; i < spline.count - 1; i++) {
        float lowerKey = spline.keys[i];
        float higherKey = spline.keys[i + 1];

        // finds the 2 points the value is in between
        if (noise >= lowerKey && noise <= higherKey) {
            stack[top].lowerPointIndex = i;
            stack[top].normDist = (noise - lowerKey) / (higherKey - lowerKey);
            return;
        }
    }
}

/*
* processes a single frame. will either spawn new frames or pop a frame from the stack
* 
* @parm noiseValues the array of noise values used to for the frame calculations
* @parm splines the array of splines used for the frame calculations
* @parm stack the stack of frames
* @parm top the index of the top of the stack
*/
void processFrame(float noiseValues[MAX_NOISE_MAPS], Spline splines[MAX_SPLINES], inout Frame stack[MAX_NOISE_MAPS], inout int top)
{
    // attempts to calculate both values
    Spline spline = splines[stack[top].splineIndex];
    if (stack[top].status == GET_BOTH_VALUES) {
        if (spline.childIndices[stack[top].lowerPointIndex] == -1) {
            stack[top].lowerValue = spline.values[stack[top].lowerPointIndex];
            stack[top].higherValue = spline.values[stack[top].lowerPointIndex + 1];
            top--;
            return;
        }
        
        // recursive case: find lower value
        pushFrame(noiseValues, splines, stack, top, spline.childIndices[stack[top].lowerPointIndex]);
        stack[top - 1].status = SET_LOW_VALUE;
        return;
    }
    
    // sets the frames low value to the previously completed frame
    Frame completedFrame = stack[top + 1];
    if (stack[top].status == SET_LOW_VALUE) {
        stack[top].lowerValue = lerp(completedFrame.lowerValue, completedFrame.higherValue, completedFrame.normDist);
        
        // create the frame for the high value
        pushFrame(noiseValues, splines, stack, top, spline.childIndices[stack[top].lowerPointIndex + 1]);
        stack[top - 1].status = SET_HIGH_VALUE;
        return;
    }
    
    // sets the higher value of the previously completed frame
    stack[top].higherValue = lerp(completedFrame.lowerValue, completedFrame.higherValue, completedFrame.normDist);
    top--;
}

/*
* samples all the splines in *recursively* and interpolates the noise values with the results
* 
* @parm noiseValues the array of noise values used to for the calculations
* @parm splines the array of splines used for the calculations
*
* @return the interpolated value of all the noise applied to the splines
*/
float sampleSplines(float noiseValues[MAX_NOISE_MAPS], Spline splines[MAX_SPLINES])
{
    // initializes the stack and the first frame
    Frame stack[MAX_NOISE_MAPS];
    int top = -1;
    for (int i = 0; i < MAX_NOISE_MAPS; i++) {
        stack[i] = (Frame) 0;
    }
    pushFrame(noiseValues, splines, stack, top, 0);
    
    // runs until stack is depleted and returns the calculated result
    while (top >= 0)
        processFrame(noiseValues, splines, stack, top);
    return lerp(stack[0].lowerValue, stack[0].higherValue, stack[0].normDist);
}






// array of all splines, childIndices will refer to a child within this array
static const Spline planetSplines[MAX_SPLINES] =
{
    // continentalness spline
    {
        { -1, -.15, -.1, 0, .15, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // keys
        { -1, -1, -.1, 0, .1, .1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // values
        { -1, -1, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // child indices
        6 // count
    },
    
    // erosion splines
    
    // peaks and valleys splines
};