using System;
using Unity.Mathematics;
using Google.Protobuf.Collections;
using Unity.Entities;
using UnityEngine;

namespace Oasis.Core
{
    

    [Serializable]
    public class Voxel
    {
        public int3 xyz; // world space
        public uint id;

        EntityManager _em;
        EntityManager em {
            get { 
                if (_em != default(EntityManager))
                    return _em;
                _em = World.DefaultGameObjectInjectionWorld.EntityManager;
                return _em;
            }
        }

        public Voxel north { get { return new Voxel(xyz + new int3(0, 0, 1)); } }
        public Voxel northEast { get { return new Voxel(xyz + new int3(1, 0, 1)); } }
        public Voxel east { get { return new Voxel(xyz + new int3(1, 0, 0)); } }
        public Voxel southEast { get { return new Voxel(xyz + new int3(1, 0, -1)); } }
        public Voxel south { get { return new Voxel(xyz + new int3(0, 0, -1)); } }
        public Voxel southWest { get { return new Voxel(xyz + new int3(-1, 0, -1)); } }
        public Voxel west { get { return new Voxel(xyz + new int3(-1, 0, 0)); } }
        public Voxel northWest { get { return new Voxel(xyz + new int3(-1, 0, 1)); } }

        public Voxel upNorth { get { return new Voxel(xyz + new int3(0, 1, 1)); } }
        public Voxel upNorthEast { get { return new Voxel(xyz + new int3(1, 1, 1)); } }
        public Voxel upEast { get { return new Voxel(xyz + new int3(1, 1, 0)); } }
        public Voxel upSouthEast { get { return new Voxel(xyz + new int3(1, 1, -1)); } }
        public Voxel upSouth { get { return new Voxel(xyz + new int3(0, 1, -1)); } }
        public Voxel upSouthWest { get { return new Voxel(xyz + new int3(-1, 1, -1)); } }
        public Voxel upWest { get { return new Voxel(xyz + new int3(-1, 1, 0)); } }
        public Voxel upNorthWest { get { return new Voxel(xyz + new int3(-1, 1, 1)); } }

        public Voxel downNorth { get { return new Voxel(xyz + new int3(0, -1, 1)); } }
        public Voxel downNorthEast { get { return new Voxel(xyz + new int3(1, -1, 1)); } }
        public Voxel downEast { get { return new Voxel(xyz + new int3(1, -1, 0)); } }
        public Voxel downSouthEast { get { return new Voxel(xyz + new int3(1, -1, -1)); } }
        public Voxel downSouth { get { return new Voxel(xyz + new int3(0, -1, -1)); } }
        public Voxel downSouthWest { get { return new Voxel(xyz + new int3(-1, -1, -1)); } }
        public Voxel downWest { get { return new Voxel(xyz + new int3(-1, -1, 0)); } }
        public Voxel downNorthWest { get { return new Voxel(xyz + new int3(-1, -1, 1)); } }

        // Constructors...all feed world space xyz...some assume default section
        public Voxel() { }

        public Voxel(int3 xyz)
        {
            this.xyz = xyz;
        }

        public Voxel GetNeighbor(Neighbor n)
        {
            switch (n)
            {
                case Neighbor.North:
                    return new Voxel(xyz + new int3(0, 0, 1));
                case Neighbor.NorthEast:
                    return new Voxel(xyz + new int3(1, 0, 1));
                case Neighbor.East:
                    return new Voxel(xyz + new int3(1, 0, 0));
                case Neighbor.SouthEast:
                    return new Voxel(xyz + new int3(1, 0, -1));
                case Neighbor.South:
                    return new Voxel(xyz + new int3(0, 0, -1));
                case Neighbor.SouthWest:
                    return new Voxel(xyz + new int3(-1, 0, -1));
                case Neighbor.West:
                    return new Voxel(xyz + new int3(-1, 0, 0));
                case Neighbor.NorthWest:
                    return new Voxel(xyz + new int3(-1, 0, 1));

                case Neighbor.Up:
                    return new Voxel(xyz + new int3(0, 1, 0));
                case Neighbor.UpNorth:
                    return new Voxel(xyz + new int3(0, 1, 1));
                case Neighbor.UpNorthEast:
                    return new Voxel(xyz + new int3(1, 1, 1));
                case Neighbor.UpEast:
                    return new Voxel(xyz + new int3(1, 1, 0));
                case Neighbor.UpSouthEast:
                    return new Voxel(xyz + new int3(1, 1, -1));
                case Neighbor.UpSouth:
                    return new Voxel(xyz + new int3(0, 1, -1));
                case Neighbor.UpSouthWest:
                    return new Voxel(xyz + new int3(-1, 1, -1));
                case Neighbor.UpWest:
                    return new Voxel(xyz + new int3(-1, 1, 0));
                case Neighbor.UpNorthWest:
                    return new Voxel(xyz + new int3(-1, 1, 1));

                case Neighbor.Down:
                    return new Voxel(xyz + new int3(0, -1, 0));
                case Neighbor.DownNorth:
                    return new Voxel(xyz + new int3(0, -1, 1));
                case Neighbor.DownNorthEast:
                    return new Voxel(xyz + new int3(1, -1, 1));
                case Neighbor.DownEast:
                    return new Voxel(xyz + new int3(1, -1, 0));
                case Neighbor.DownSouthEast:
                    return new Voxel(xyz + new int3(1, -1, -1));
                case Neighbor.DownSouth:
                    return new Voxel(xyz + new int3(0, -1, -1));
                case Neighbor.DownSouthWest:
                    return new Voxel(xyz + new int3(-1, -1, -1));
                case Neighbor.DownWest:
                    return new Voxel(xyz + new int3(-1, -1, 0));
                case Neighbor.DownNorthWest:

                default:
                    return this; // TODO should return null, throw, or default(voxel)
            }
        }

        public Voxel Adjacent(Side side)
        {
            switch (side)
            {
                case Side.North:
                    return new Voxel(xyz + new int3(0, 0, 1));
                case Side.South:
                    return new Voxel(xyz + new int3(0, 0, -1));
                case Side.East:
                    return new Voxel(xyz + new int3(1, 0, 0));
                case Side.West:
                    return new Voxel(xyz + new int3(-1, 0, 0));
                case Side.Up:
                    return new Voxel(xyz + new int3(0, 1, 0));
                case Side.Down:
                    return new Voxel(xyz + new int3(0, -1, 0));
                default:
                    return this; // TODO should return null, throw, or default(voxel)
            }
        }

    }


    public class WorldVoxel : Voxel
    {
        // public WorldVoxel(int3 xyz)
        // {
        //     return new Voxel(xyz);
        // }
        // public Voxel(int3 section, int3 xyz)
        // {
        //     this.xyz = new int3(xyz.x + section.x*16,  xyz.y + section.y*16,  xyz.z + section.z*16);
        // }
        // public Voxel(int index)
        // {
        //     int3 dims = new int3(16);
        //     this.xyz.z = index / (dims.y * dims.x);
        //     this.xyz.y = (index % (dims.y * dims.x)) / dims.x;
        //     this.xyz.x = index % dims.x;
        // }

        // public Voxel(int index, int3 dims)
        // {
        //     this.xyz.z = index / (dims.y * dims.x);
        //     this.xyz.y = (index % (dims.y * dims.x)) / dims.x;
        //     this.xyz.x = index % dims.x;
        // }


        public int3 section
        {
            get { return new int3((int)Math.Floor(xyz.x / 16.0f), (int)Math.Floor(xyz.y / 16.0f), (int)Math.Floor(xyz.z / 16.0f)); }
        }

        public int3 sectionVoxel
        {
            get
            {
                // return new int3(((xyz.x % 16 + 16) % 16), ((xyz.y % 16 + 16) % 16), ((xyz.z % 16 + 16) % 16)); // C# modulo is remainder n   // xyz
                // return new int3(((xyz.y % 16 + 16) % 16), ((xyz.z % 16 + 16) % 16), ((xyz.x % 16 + 16) % 16)); // C# modulo is remainder n  // yzx
                return new int3(((xyz.x % 16 + 16) % 16), ((xyz.z % 16 + 16) % 16), ((xyz.y % 16 + 16) % 16)); // C# modulo is remainder n  // xzy
            }
        }

        public int index
        {
            get { return sectionVoxel.ToIndex(); }
        }

    }
    
}