﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Quad64.src.Scripts
{
    class TextureFormats
    {
        public static Bitmap createColorTexture(Color color)
        {
            Bitmap tex = new Bitmap(1, 1);
            tex.SetPixel(0, 0, color);
            return tex;
        }

        public static string ConvertFormatToString(byte formatColorType, byte formatByteSize)
        {
            string formatStr = "";
            switch (formatColorType & 7) {
                case 0: formatStr = "RGBA"; break;
                case 1: formatStr = "YUV"; break;
                case 2: formatStr = "CI"; break;
                case 3: formatStr = "IA"; break;
                case 4: formatStr = "I"; break;
                default: formatStr = "UNKNOWN"; break;
            }
            switch (formatByteSize & 3)
            {
                case 0: formatStr += "4"; break;
                case 1: formatStr += "8"; break;
                case 2: formatStr += "16"; break;
                case 3: formatStr += "32"; break;
            }
            return formatStr;
        }

        public static byte ConvertStringToFormat(string str)
        {
            str = str.ToLower();
            if (str.Equals("rgba16"))
                return 0x10;
            else if (str.Equals("rgba32"))
                return 0x18;
            else if (str.Equals("ci4"))
                return 0x40;
            else if (str.Equals("ci8"))
                return 0x48;
            else if (str.Equals("ia4"))
                return 0x60;
            else if (str.Equals("ia8"))
                return 0x68;
            else if (str.Equals("ia16"))
                return 0x70;
            else if (str.Equals("i4"))
                return 0x80;
            else if (str.Equals("i8"))
                return 0x88;
            else if (str.Equals("1bpp")) // Not a real F3D format.
                return 0x00;

            return 0x10;
        }

        public static int getNumberOfBitsForFormat(byte format)
        {
            switch (format)
            {
                case 0x00: // Note: "1 bit per pixel" is not a Fast3D format.
                    return 1;
                case 0x40:
                case 0x60:
                case 0x80:
                    return 4;
                case 0x48:
                case 0x68:
                case 0x88:
                    return 8;
                case 0x10:
                case 0x70:
                case 0x90:
                default:
                    return 16;
                case 0x18:
                    return 32;
            }
        }

        public static byte[] encodeTexture(byte format, Bitmap texture)
        {
            switch (format)
            {
                default:
                case 0x00: // Note: "1 bit per pixel" is not a Fast3D format.
                    return encode1BPP(texture);
                case 0x10:
                    return encodeRGBA16(texture);
                case 0x18:
                    return encodeRGBA32(texture);
                case 0x60:
                    return encodeIA4(texture);
                case 0x68:
                    return encodeIA8(texture);
                case 0x70:
                    return encodeIA16(texture);
                case 0x80:
                    return encodeI4(texture);
                case 0x88:
                    return encodeI4(texture);
                case 0x40:
                case 0x48:
                    MessageBox.Show("CI texture encoding is not currently supported in this version.",
                        "Notice",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
                    return null;
            }
        }
        
        public static byte getBit(int color, int bit)
        {
            return (byte)(((color >> 24) & 0xFF) > 0 ? (1 << bit) : 0);
        }

        public static byte[] encode1BPP(Bitmap texture)
        {
            int data_size = (texture.Width * texture.Height) / 8;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size; i++)
            {
                int x = (i * 8) % texture.Width;
                int y = (i * 8) / texture.Width;

                data[i] = (byte)(
                    getBit(texture.GetPixel(x + 0, y).ToArgb(), 7) |
                    getBit(texture.GetPixel(x + 1, y).ToArgb(), 6) |
                    getBit(texture.GetPixel(x + 2, y).ToArgb(), 5) |
                    getBit(texture.GetPixel(x + 3, y).ToArgb(), 4) |
                    getBit(texture.GetPixel(x + 4, y).ToArgb(), 3) |
                    getBit(texture.GetPixel(x + 5, y).ToArgb(), 2) |
                    getBit(texture.GetPixel(x + 6, y).ToArgb(), 1) |
                    getBit(texture.GetPixel(x + 7, y).ToArgb(), 0)
                );
            }
            return data;
        }

        public static byte[] encodeRGBA16(Bitmap texture)
        {
            int data_size = (texture.Width * texture.Height) * 2;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size / 2; i++)
            {
                int x = i % texture.Width;
                int y = i / texture.Width;
                Color pix = texture.GetPixel(x, y);
                byte red = (byte)((int)((pix.R / 255.0f) * 32.0) & 0x1F);
                byte green = (byte)((int)((pix.G / 255.0f) * 32.0f) & 0x1F);
                byte blue = (byte)((int)((pix.B / 255.0f) * 32.0f) & 0x1F);
                byte alpha = (byte)(pix.A == 255 ? 1 : 0);
                
                data[i * 2] = (byte)((red << 3) | (green >> 2));
                data[(i * 2) + 1] = (byte)(((green & 3) << 6) | (blue << 1) | alpha);
            }
            return data;
        }


        public static byte[] encodeRGBA32(Bitmap texture)
        {
            int data_size = (texture.Width * texture.Height) * 4;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size / 4; i++)
            {
                int x = i % texture.Width;
                int y = i / texture.Width;
                Color pix = texture.GetPixel(x, y);

                data[(i * 4) + 0] = pix.R;
                data[(i * 4) + 1] = pix.G;
                data[(i * 4) + 2] = pix.B;
                data[(i * 4) + 3] = pix.A;
            }
            return data;
        }

        public static byte[] encodeIA4(Bitmap texture)
        {
            int data_size = (texture.Width * texture.Height) / 2;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size; i++)
            {
                int x = (i * 2) % texture.Width;
                int y = (i * 2) / texture.Width;

                Color pix1 = texture.GetPixel(x, y);
                byte pix1_avg = (byte)((((pix1.R + pix1.G + pix1.B) / 3) / 255.0f) * 8.0f);
                byte upper = (byte)((pix1_avg << 1) | (pix1.A < 255 ? 0 : 1));

                Color pix2 = texture.GetPixel(x + 1, y);
                byte pix2_avg = (byte)((((pix2.R + pix2.G + pix2.B) / 3) / 255.0f) * 8.0f);
                byte lower = (byte)((pix2_avg << 1) | (pix2.A < 255 ? 0 : 1));

                data[i] = (byte)(((upper & 0xF) << 4) | (lower & 0xF));
            }
            return data;
        }

        public static byte[] encodeIA8(Bitmap texture)
        {
            int data_size = texture.Width * texture.Height;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size; i++)
            {
                int x = i % texture.Width;
                int y = i / texture.Width;

                Color pix = texture.GetPixel(x, y);
                byte pix_avg = (byte)((((pix.R + pix.G + pix.B) / 3) / 255.0f) * 16.0f);
                byte pix_alpha = (byte)((pix.A / 255.0f) * 16.0f);

                data[i] = (byte)(((pix_avg & 0xF) << 4) | (pix_alpha & 0xF));
            }
            return data;
        }

        public static byte[] encodeIA16(Bitmap texture)
        {
            int data_size = texture.Width * texture.Height * 2;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size / 2; i++)
            {
                int x = i % texture.Width;
                int y = i / texture.Width;

                Color pix = texture.GetPixel(x, y);
                byte pix_avg = (byte)((pix.R + pix.G + pix.B) / 3);

                data[i * 2] = pix_avg;
                data[(i * 2) + 1] = pix.A; 
            }
            return data;
        }

        public static byte[] encodeI4(Bitmap texture)
        {
            int data_size = (texture.Width * texture.Height) / 2;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size; i++)
            {
                int x = (i * 2) % texture.Width;
                int y = (i * 2) / texture.Width;

                Color pix1 = texture.GetPixel(x, y);
                byte upper = (byte)((((pix1.R + pix1.G + pix1.B) / 3) / 255.0f) * 16.0f);

                Color pix2 = texture.GetPixel(x + 1, y);
                byte lower = (byte)((((pix2.R + pix2.G + pix2.B) / 3) / 255.0f) * 16.0f);

                data[i] = (byte)(((upper & 0xF) << 4) | (lower & 0xF));
            }
            return data;
        }


        public static byte[] encodeI8(Bitmap texture)
        {
            int data_size = texture.Width * texture.Height;
            byte[] data = new byte[data_size];
            for (int i = 0; i < data_size; i++)
            {
                int x = i % texture.Width;
                int y = i / texture.Width;

                Color pix = texture.GetPixel(x, y); 

                data[i] = (byte)((pix.R + pix.G + pix.B) / 3);
            }
            return data;
        }

        public static Bitmap decodeTexture(byte format, byte[] data, int width, int height, ushort[] palette, bool isPaletteRGBA16)
        {
            switch (format)
            {
                default:
                case 0x00: // Note: "1 bit per pixel" is not a Fast3D format.
                    return decode1BPP(data, width, height);
                case 0x10:
                    return decodeRGBA16(data, width, height);
                case 0x18:
                    return decodeRGBA32(data, width, height);
                case 0x40:
                    return decodeCI4(data, width, height, palette, isPaletteRGBA16);
                case 0x48:
                    return decodeCI8(data, width, height, palette, isPaletteRGBA16);
                case 0x60:
                    return decodeIA4(data, width, height);
                case 0x68:
                    return decodeIA8(data, width, height);
                case 0x70:
                    return decodeIA16(data, width, height);
                case 0x80:
                case 0x90:
                    return decodeI4(data, width, height);
                case 0x88:
                    return decodeI8(data, width, height);
            }
        }


        public static Bitmap decode1BPP(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);
            int len = (width * height) / 8;
            for (int i = 0; i < len; ++i)
            {
                for (int x = 0; x < 8; x++)
                {
                    byte intensity = (byte)((data[i] >> (7 - x)) & 1);
                    if (intensity > 0)
                        intensity = 0xFF;
                    int alpha = intensity;
                    int pos = (i * 8) + x;
                    tex.SetPixel(pos % width, pos / width, Color.FromArgb(alpha, intensity, intensity, intensity));
                }
            }

            tex.Tag = new string[] { "Format: 1BPP", "Width: " + width,
             "Height: " + height };
            return tex;
        }

        public static Bitmap decodeRGBA32(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);
            for (int i = 0; i < width * height; ++i)
            {
                byte red = data[i * 4 + 0];
                byte green = data[i * 4 + 1];
                byte blue = data[i * 4 + 2];
                byte alpha = data[i * 4 + 3]; // (Transparency)
                tex.SetPixel(i % width, i / width, Color.FromArgb(alpha, red, green, blue));
            }

            tex.Tag = new string[] { "Format: RGBA32", "Width: " + width,
             "Height: " + height };
            return tex;
        }

        public static Bitmap decodeRGBA16(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);

            for (int i = 0; i < width * height; ++i)
            {
                ushort pixel = (ushort)((data[i * 2] << 8) | data[i * 2 + 1]);
                byte red = (byte)(((pixel >> 11) & 0x1F) * 8);
                byte green = (byte)(((pixel >> 6) & 0x1F) * 8);
                byte blue = (byte)(((pixel >> 1) & 0x1F) * 8);
                byte alpha = (pixel & 1) > 0 ? (byte)0xFF : (byte)0x00; // (Transparency)
                tex.SetPixel(i % width, i / width, Color.FromArgb(alpha, red, green, blue));
            }

            tex.Tag = new string[] { "Format: RGBA16", "Width: " + width,
             "Height: " + height };
            return tex;
        }

        public static Bitmap decodeIA16(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);

            for (int i = 0; i < width * height; ++i)
            {
                ushort pixel = (ushort)((data[i * 2] << 8) | data[i * 2 + 1]);
                byte intensity = data[i * 2];
                byte alpha = data[i * 2 + 1];
                tex.SetPixel(i % width, i / width, Color.FromArgb(alpha, intensity, intensity, intensity));
            }

            tex.Tag = new string[] { "Format: IA16", "Width: " + width,
             "Height: " + height};
            return tex;
        }

        public static Bitmap decodeIA8(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);

            for (int i = 0; i < width * height; ++i)
            {
                byte intensity = (byte)(((data[i] >> 4) & 0xF) * 16);
                byte alpha = (byte)((data[i] & 0xF) * 16);
                tex.SetPixel(i % width, i / width, Color.FromArgb(alpha, intensity, intensity, intensity));
            }

            tex.Tag = new string[] { "Format: IA8", "Width: " + width,
             "Height: " + height };
            return tex;
        }
        public static Bitmap decodeIA4(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);
            int len = (width * height) / 2;
            for (int i = 0; i < len; i++)
            {
                byte twoPixels = data[i];
                byte intensity = (byte)((twoPixels >> 5) * 32);
                byte alpha = (byte)(((twoPixels >> 4) & 0x1) * 255);
                tex.SetPixel((i * 2) % width, (i * 2) / width, Color.FromArgb(alpha, intensity, intensity, intensity));
                
                intensity = (byte)(((twoPixels >> 1) & 0x7) * 32);
                alpha = (byte)((twoPixels & 0x1) * 255);
                tex.SetPixel(((i * 2) + 1) % width, ((i * 2) + 1) / width, Color.FromArgb(alpha, intensity, intensity, intensity));
            }

            tex.Tag = new string[] { "Format: IA4", "Width: " + width,
             "Height: " + height };
            return tex;
        }
        public static Bitmap decodeI8(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);

            for (int i = 0; i < width * height; ++i)
            {
                byte intensity = data[i];
                tex.SetPixel(i % width, i / width, Color.FromArgb(intensity, intensity, intensity));
            }

            tex.Tag = new string[] { "Format: I8", "Width: " + width,
             "Height: " + height };
            return tex;
        }
        public static Bitmap decodeI4(byte[] data, int width, int height)
        {
            Bitmap tex = new Bitmap(width, height);
            int len = (width * height)/2;
            for (int i = 0; i < len; i++)
            {
                byte twoPixels = data[i];
                byte intensity = (byte)((twoPixels >> 4) * 16);
                tex.SetPixel((i * 2) % width, (i * 2) / width, Color.FromArgb(intensity, intensity, intensity));

                intensity = (byte)((twoPixels & 0xF) * 16);
                tex.SetPixel(((i * 2) + 1) % width, ((i * 2) + 1) / width, Color.FromArgb(intensity, intensity, intensity));
            }

            tex.Tag = new string[] { "Format: I4", "Width: " + width,
             "Height: " + height };
            return tex;
        }

        public static Bitmap decodeCI4(byte[] data, int width, int height, ushort[] palette, bool isPaletteRGBA16)
        {
            Bitmap tex = new Bitmap(width, height);
            int len = (width * height) / 2;
            for (int i = 0; i < len; i++)
            {
                ushort pixel = palette[(data[i] >> 4) & 0xF];
                byte red = (byte)(((pixel >> 11) & 0x1F) * 8);
                byte green = (byte)(((pixel >> 6) & 0x1F) * 8);
                byte blue = (byte)(((pixel >> 1) & 0x1F) * 8);
                byte alpha = (pixel & 1) > 0 ? (byte)0xFF : (byte)0x00;
                tex.SetPixel((i * 2) % width, (i * 2) / width, Color.FromArgb(alpha, red, green, blue));

                pixel = palette[(data[i]) & 0xF];
                red = (byte)(((pixel >> 11) & 0x1F) * 8);
                green = (byte)(((pixel >> 6) & 0x1F) * 8);
                blue = (byte)(((pixel >> 1) & 0x1F) * 8);
                alpha = (pixel & 1) > 0 ? (byte)0xFF : (byte)0x00;
                tex.SetPixel(((i * 2) + 1) % width, ((i * 2) + 1) / width, Color.FromArgb(alpha, red, green, blue));
            }

            tex.Tag = new string[] { "Format: CI4", "Width: " + width,
             "Height: " + height };
            return tex;
        }

        public static Bitmap decodeCI8(byte[] data, int width, int height, ushort[] palette, bool isPaletteRGBA16)
        {
            Bitmap tex = new Bitmap(width, height);
            int len = (width * height) / 2;
            for (int i = 0; i < len; i++)
            {
                ushort pixel = palette[data[i]];
                byte red = (byte)(((pixel >> 11) & 0x1F) * 8);
                byte green = (byte)(((pixel >> 6) & 0x1F) * 8);
                byte blue = (byte)(((pixel >> 1) & 0x1F) * 8); 
                byte alpha = (pixel & 1) > 0 ? (byte)0xFF : (byte)0x00;
                tex.SetPixel(i % width, i / width, Color.FromArgb(alpha, red, green, blue));
            }

            tex.Tag = new string[] { "Format: CI8", "Width: " + width,
             "Height: " + height };
            return tex;
        }
    }
}
