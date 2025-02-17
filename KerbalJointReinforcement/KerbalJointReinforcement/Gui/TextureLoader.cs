﻿using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace KerbalJointReinforcement
{
#if IncludeAnalyzer

	public class TextureLoader
	{
		// Use System.IO.File to read a file into a texture in RAM. Path is relative to the DLL
		// Do it this way so the images are not affected by compression artifacts or Texture quality settings
		// <param name="tex">Texture to load</param>
		// <param name="fileName">Filename of the image in side the Textures folder</param>
		internal static bool LoadImageFromFile(Texture2D tex, string fileName)
		{
			string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string pathPluginTextures = string.Format("{0}/../Textures", pluginPath);
			bool blnReturn = false;
			try
			{
				if(File.Exists(string.Format("{0}/{1}", pathPluginTextures, fileName)))
				{
					tex.LoadImage(File.ReadAllBytes(string.Format("{0}/{1}", pathPluginTextures, fileName)));
					blnReturn = true;
				}
				else
				{
					throw new FileNotFoundException();
				}
			}
			catch(Exception ex)
			{
				Logger.Log(string.Format("Failed to load the texture: {0}/{1} ({2})",
					pathPluginTextures, fileName, ex.Message), Logger.Level.Error);
			}

			return blnReturn;
		}

		// creates the solid texture of given size and Color
		private static Texture2D CreateTextureFromColor(int width, int height, Color col)
		{
			var pix = new Color[width * height];

			for(int i = 0; i < pix.Length; i++)
				pix[i] = col;

			var result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();

			return result;
		}
	}

#endif
}
