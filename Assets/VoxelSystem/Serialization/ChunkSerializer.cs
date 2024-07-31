using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Serializing;
using K4os.Compression.LZ4.Encoders;
using Unity.Collections;
using UnityEngine;
using VoxelSystem;

namespace VoxelSystem
{
    public static class ChunkSerializer
    {
        //Write each axis of a Vector2.
        public static void WriteChunk(this Writer writer, Chunk chunk)
        {
            writer.WriteAABB(chunk.Aabb);
            writer.WriteInt32((int)chunk.AreaInformation);

            if (chunk.AreaInformation == RegionInformation.Modified)
            {
                writer.WriteArray(chunk.GetColors().ToArray());
            }
        }

//Read and return a Vector2.
        public static Chunk ReadChunk(this Reader reader)
        {
            var chunk = new Chunk(Allocator.Domain);

            chunk.Aabb = reader.ReadAABB();

            chunk.AreaInformation = (RegionInformation)reader.ReadInt32();

            // TODO figure out how to read colors array - probably need to send the length.
            if (chunk.AreaInformation == RegionInformation.Modified)
            {
                var colors = Array.Empty<ushort>();
                reader.ReadArray<ushort>(ref colors);
                chunk.SetColors(colors);
            }

            return chunk;
        }

        public static void WriteRegion(this Writer writer, Region region)
        {
            writer.WriteInt32(region.Origin.x);
            writer.WriteInt32(region.Origin.y);
            writer.WriteInt32(region.Origin.z);
            
            writer.WriteInt32(region.LODLevel.Value);
        }

        public static Region ReadRegion(this Reader reader)
        {
            var originX = reader.ReadInt32();
            var originY = reader.ReadInt32();
            var originZ = reader.ReadInt32();

            var lodLevelValue = reader.ReadInt32();
            return new Region()
            {
                Origin = new Unity.Mathematics.int3(originX, originY, originZ),
                LODLevel = new LODLevel(lodLevelValue)
            };
        }

        public static void WriteAABB(this Writer writer, AABB aabb)
        {
            writer.WriteInt32(aabb.Min.x);
            writer.WriteInt32(aabb.Min.y);
            writer.WriteInt32(aabb.Min.z);

            writer.WriteInt32(aabb.Max.x);
            writer.WriteInt32(aabb.Max.y);
            writer.WriteInt32(aabb.Max.z);
        }

        public static AABB ReadAABB(this Reader reader)
        {
            var minx = reader.ReadInt32();
            var miny = reader.ReadInt32();
            var minz = reader.ReadInt32();

            var maxx = reader.ReadInt32();
            var maxy = reader.ReadInt32();
            var maxz = reader.ReadInt32();

            return new AABB()
            {
                Min = new Unity.Mathematics.int3(minx, miny, minz),
                Max = new Unity.Mathematics.int3(maxx, maxy, maxz),
            };
        }
    }
}