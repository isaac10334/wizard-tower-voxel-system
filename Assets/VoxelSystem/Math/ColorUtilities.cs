using System;
using UnityEngine;

namespace VoxelSystem.Colors
{
    public class ColorUtilities
    {
        // Global palette for 8-bit colors, each entry is a 32-bit uint representing an RGBA color
        private static readonly uint[] GlobalPalette = new uint[256];

        // Convert an 8-bit color index to a 32-bit RGBA color
        public static uint Convert8BitTo32Bit(byte colorIndex)
        {
            return GlobalPalette[colorIndex];
        }

        // Convert a 32-bit RGBA color to an 8-bit color index (simple nearest match, not optimal)
        public static byte Convert32BitTo8Bit(uint rgba)
        {
            byte closestIndex = 0;
            uint closestDistance = uint.MaxValue;

            for (byte i = 0; i < GlobalPalette.Length; i++)
            {
                uint paletteColor = GlobalPalette[i];
                uint distance = ColorDistance(rgba, paletteColor);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        // Convert a 16-bit color (5-6-5 format) to a 32-bit RGBA color
        public static uint Convert16BitTo32Bit(ushort color)
        {
            byte r = (byte)((color >> 11) & 0x1F);
            byte g = (byte)((color >> 5) & 0x3F);
            byte b = (byte)(color & 0x1F);

            // Scale up to 8-bit values
            uint red = (uint)((r << 3) | (r >> 2));
            uint green = (uint)((g << 2) | (g >> 4));
            uint blue = (uint)((b << 3) | (b >> 2));
            uint alpha = 0xFF; // Assume fully opaque

            return (red << 24) | (green << 16) | (blue << 8) | alpha;
        }

        // Convert a 32-bit RGBA color to a 16-bit color (5-6-5 format)
        public static ushort Convert32BitTo16Bit(uint rgba)
        {
            byte r = (byte)((rgba >> 24) & 0xFF);
            byte g = (byte)((rgba >> 16) & 0xFF);
            byte b = (byte)((rgba >> 8) & 0xFF);

            // Scale down to 5 or 6 bits
            ushort red = (ushort)((r >> 3) & 0x1F);
            ushort green = (ushort)((g >> 2) & 0x3F);
            ushort blue = (ushort)((b >> 3) & 0x1F);

            return (ushort)((red << 11) | (green << 5) | blue);
        }

        // Calculate the 'distance' between two colors for the 8-bit conversion
        private static uint ColorDistance(uint color1, uint color2)
        {
            byte r1 = (byte)((color1 >> 24) & 0xFF);
            byte g1 = (byte)((color1 >> 16) & 0xFF);
            byte b1 = (byte)((color1 >> 8) & 0xFF);

            byte r2 = (byte)((color2 >> 24) & 0xFF);
            byte g2 = (byte)((color2 >> 16) & 0xFF);
            byte b2 = (byte)((color2 >> 8) & 0xFF);

            uint r = (uint)System.Math.Abs(r1 - r2);
            uint g = (uint)System.Math.Abs(g1 - g2);
            uint b = (uint)System.Math.Abs(b1 - b2);

            return r * r + g * g + b * b;
        }

        // Convert a 32-bit RGBA color to Unity's Color32
        public static Color32 Convert32BitToColor32(uint rgba)
        {
            byte r = (byte)((rgba >> 24) & 0xFF);
            byte g = (byte)((rgba >> 16) & 0xFF);
            byte b = (byte)((rgba >> 8) & 0xFF);
            byte a = (byte)(rgba & 0xFF);

            return new Color32(r, g, b, a);
        }

        // Convert Unity's Color32 to a 32-bit RGBA color
        public static uint ConvertColor32To32Bit(Color32 color)
        {
            return ((uint)color.r << 24) | ((uint)color.g << 16) | ((uint)color.b << 8) | color.a;
        }

        // Convert an 8-bit color index to Unity's Color32
        public static Color32 Convert8BitToColor32(byte colorIndex)
        {
            uint rgba = Convert8BitTo32Bit(colorIndex);
            return Convert32BitToColor32(rgba);
        }

        // Convert Unity's Color32 to an 8-bit color index
        public static byte ConvertColor32To8Bit(Color32 color)
        {
            uint rgba = ConvertColor32To32Bit(color);
            return Convert32BitTo8Bit(rgba);
        }

        // Convert a 16-bit color (5-6-5 format) to Unity's Color32
        public static Color32 Convert16BitToColor32(ushort color)
        {
            uint rgba = Convert16BitTo32Bit(color);
            return Convert32BitToColor32(rgba);
        }

        // Convert Unity's Color32 to a 16-bit color (5-6-5 format)
        public static ushort ConvertColor32To16Bit(Color32 color)
        {
            uint rgba = ConvertColor32To32Bit(color);
            return Convert32BitTo16Bit(rgba);
        }

        static ColorUtilities()
        {
            for (int i = 0; i < GlobalPalette.Length; i++)
            {
                // This is just to fill the palette with some values for demonstration purposes
                GlobalPalette[i] = (uint)((i << 24) | (i << 16) | (i << 8) | 0xFF);
            }
        }
    }
}