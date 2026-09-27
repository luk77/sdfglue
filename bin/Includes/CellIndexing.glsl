//-------------------------------------------------------------------------------
// Cell indexing functions
//-------------------------------------------------------------------------------

int spiralIndex(int x, int y, bool clockwise)
{
    if (x == 0 && y == 0)
        return 0;

    if (clockwise)
        y = -y;

    int k = max(abs(x), abs(y));
    int m = (2 * k + 1) * (2 * k + 1) - 1;

    int d;

    if (y == -k)
        d = k - x;
    else if (x == -k)
        d = 2 * k + (y + k);
    else if (y == k)
        d = 4 * k + (x + k);
    else
        d = 6 * k + (k - y);

    return m - d;
}


int diamondSpiralIndex(int x, int y, bool clockwise)
{
    if (x == 0 && y == 0)
        return 0;

    if (!clockwise)
        y = -y;

    int k = abs(x) + abs(y);
    int start = 2 * k * (k - 1) + 1;

    int d;

    if (y <= 0)
        d = k - x;
    else if (x <= 0)
        d = 2 * k + y;
    else
        d = 3 * k + x;

    return start + d;
}



uint Part1By2(uint x)
{
    x &= 0x000003ffu;

    x = (x ^ (x << 16)) & 0xff0000ffu;
    x = (x ^ (x <<  8)) & 0x0300f00fu;
    x = (x ^ (x <<  4)) & 0x030c30c3u;
    x = (x ^ (x <<  2)) & 0x09249249u;

    return x;
}

uint mortonEncode(uvec3 p)
{
    return
          Part1By2(p.x)
        | (Part1By2(p.y) << 1)
        | (Part1By2(p.z) << 2);
}

uint encodeSigned(int v)
{
    return (v >= 0)
        ? uint(v) << 1
        : (uint(-v) << 1) - 1u;
}

uint mortonIndex(ivec3 p)
{
    return mortonEncode(uvec3(
        encodeSigned(p.x),
        encodeSigned(p.y),
        encodeSigned(p.z)));
}

float calculateCellIndex2d(in vec2 coords)
{
    //return coords.x + coords.y + PI * sin(coords.x * coords.y);
    //return spiralIndex(int(coords.x), int(coords.y), false);    // CCW
    return spiralIndex(int(coords.x), int(coords.y), true);     // CW
    //return diamondSpiralIndex(int(coords.x), int(coords.y), true);     // CW
}

float calculateCellIndex3d(in vec3 coords)
{
    //return coords.x + coords.y + PI * sin(coords.x * coords.y) + PI * sin(0.88 + coords.x * coords.y * coords.z);
    return mortonIndex(ivec3(coords.x, coords.y, coords.z));
}
