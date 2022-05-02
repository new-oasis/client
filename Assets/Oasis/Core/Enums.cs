using System;

namespace Oasis.Core
{
    

    public enum Side { East, Up, North, West, Down, South }; // %3 => xyz;  // Voxels have sides
    public enum Face { Right, Top, Front, Left, Bottom, Back };  // Blocks have faces.  
    public enum Axis { X, Y, Z };
    public enum Corner {NorthEast, NorthWest, SouthEast, SouthWest};
    public enum Quadrant {NorthWest, NorthEast, SouthEast, SouthWest, UpperNorth, UpperEast, UpperWest, UpperSouth, LowerNorth, LowerEast, LowerWest, LowerSouth, None}


    public enum Facing { North, East, South, West }

    public enum Direction { Up, Down, North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest, None }

    public enum Neighbor { 
        Up, UpNorth, UpNorthEast, UpEast, UpSouthEast, UpSouth, UpSouthWest, UpWest, UpNorthWest,
        Down, DownNorth, DownNorthEast, DownEast, DownSouthEast, DownSouth, DownSouthWest, DownWest, DownNorthWest,
        North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest,
        None }
    
    public enum BlockType { air, cube, model, liquid }
    public enum TextureType { Opaque, AlphaClip, Transparent, Shader, None }


    public static class Extensions
    {
        public static Side Opposite(this Side side)
        {
            if (side.Equals(Side.Up))
                return Side.Down;
            if (side.Equals(Side.Down))
                return Side.Up;

            if (side.Equals(Side.North))
                return Side.South;
            if (side.Equals(Side.South))
                return Side.North;

            if (side.Equals(Side.East))
                return Side.West;
            if (side.Equals(Side.West))
                return Side.East;

            return Side.Up;
        }
    }
    
}