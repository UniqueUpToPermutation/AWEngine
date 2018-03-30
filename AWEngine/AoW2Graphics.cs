using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using IlbEditorNet;

namespace AoWGraphics
{
	public class AoW2Bitmap : AoWBitmap
	{
		private bool _isMasked = false;
		private UInt32 _imageOffset = 0;
		public UInt32 ImageOffset
		{
			get { return _imageOffset; }
			set { _imageOffset = value; }
		}

		public AoW2Bitmap()
		{			
		}

		public AoW2Bitmap(AoWBitmap src)
			: base(src)
		{
		}

		public AoW2Bitmap(Bitmap original, string name)
			: base(original, name)
		{
		}

		public override bool ReadImageInfo(BinaryReader br)
		{
			return ReadImageInfoMain(br);
		}

		protected bool ReadImageInfoMain(BinaryReader br)
		{
			// Read image type:
			UInt32 dword = br.ReadUInt32();
			switch (dword)
			{

				case 0x00300001: _imageType = AoWImageType.Type01_Picture08_0x01;	break;
				case 0x00300002: _imageType = AoWImageType.Type02_RLESprite08_0x02; break;
				case 0x00300003: _imageType = AoWImageType.Type03_Sprite08_0x03;	break;
				case 0x00300010: _imageType = AoWImageType.Type16_Picture16_0x10;	break;
				case 0x00300011: _imageType = AoWImageType.Type17_RLESprite16_0x11; break;
				//case 0x12: _imageType = AoWImageType.Type18_TransparentRLESprite16_0x12; break;
				case 0x00300014: _imageType = AoWImageType.Type22_Sprite16_0x16;	break;
				case 0x00300015: 
					_imageType = AoWImageType.Type22_Sprite16_0x16;
					_isMasked = true;
					break;
				case 0x00300016: _imageType = AoWImageType.TypeAoWSM_AlphaMask; break;				
				default:
					throw new Exception(string.Format("Unsupported type (0x{0:X} of image {1})", dword, _imageNumber));
			}

			// Read what information follows according to codes
			/*
			 * 0x0A name 		- byte + n * chars
			 * 0x0B width		- dword
			 * 0x0C height		- dword
			 * 0x0D offsetX		- dword
			 * 0x0E offsety		- dword
			 * 0x0F instance number - dword
			 * 0X10 loadMode	- byte
			 * 0X11 size		- dword
			 * 0X12 totalwide	- dword	// width + offsetX
			 * 0X13 totalheight - dword	// height + offsety
			 * 0X16 hotSpotX	- dword
			 * 0X17 hotSpotY	- dword
			 * 0X18 description - byte + n * chars
			 * 0X19 blend data	- complex
			 * 0X32 pixel format - dword
			 * 0X3C clipwide	- dword
			 * 0X3D cliphigh	- dword
			 * 0X3E clipxoff	- dword
			 * 0X3F clipyoff	- dword
			 * 0X40 transparent - dword
			 * 0X41 ??? 
			 * 0x14 imageOffset - dword			 * 
			 * */
			SortedList<Byte, Byte> infoMap = new SortedList<byte,byte>();
			// Read info data lenght
			Byte infoQty = br.ReadByte();
			// Store them in a container
			for (int i = 0; i < infoQty; ++i)
			{
				Byte id = br.ReadByte();
				infoMap.Add(id, br.ReadByte());
			}

			// Read bitmap filename:
			if (infoMap.ContainsKey(0x0A))
			{
				dword = br.ReadByte();
				StringBuilder strb = new StringBuilder();
				for (UInt32 i = 0; i < dword; ++i)
					strb.Append((char)br.ReadSByte());
				_name = strb.ToString();
			}

			// Read image size:
			if (infoMap.ContainsKey(0x0B))
				_cX = br.ReadInt32();
			if (infoMap.ContainsKey(0x0C))
				_cY = br.ReadInt32();

			// Read image offset:
			if (infoMap.ContainsKey(0x0D))
				_xShift = br.ReadInt32();
			if (infoMap.ContainsKey(0x0E))
				_yShift = br.ReadInt32();

			// Read instance number:
			if (infoMap.ContainsKey(0x0F))
				_instanceNumber = br.ReadInt32();

			/* Logger.LogMessage(MsgLevel.DEBUG,
				string.Format("Image {0}({1}): '{2}', {3}, {4}x{5}", _imageNumber, _instanceNumber, _name, _imageType, _cX, _cY)); */
            Debug.WriteLine(string.Format("Image {0}({1}): '{2}', {3}, {4}x{5}", _imageNumber, _instanceNumber, _name, _imageType, _cX, _cY));

            // Read LoadMode byte:
            if (infoMap.ContainsKey(0x10))
			{
				Byte loadMode = br.ReadByte();
				switch (loadMode)
				{
					case 0: _loadMode = AoWLoadMode.lmInstant; break;
					case 1: _loadMode = AoWLoadMode.lmWhenUsed; break;
					case 2: _loadMode = AoWLoadMode.lmOnDemand; break;
					case 3: _loadMode = AoWLoadMode.lmWhenReferenced; break;
					default: _loadMode = AoWLoadMode.lmWhenUsed; break;
				}
			}

			// Read image data size:
			if (infoMap.ContainsKey(0x11))
			{
				_imageDataSize = br.ReadInt32();
				if (_imageDataSize < 0)
					throw new Exception(string.Format("Invalid data size of image {0})", _imageNumber));
			}

			// Read image size as should be visualized: dimension + offset:
			if (infoMap.ContainsKey(0x12))
			{
				dword = br.ReadUInt32();
				//Debug.Assert((int)dword >= _cX + _xShift);
			}
			if (infoMap.ContainsKey(0x13))
			{
				dword = br.ReadUInt32();
				//Debug.Assert((int)dword >= _cY + _yShift);
			}

			// Read hospots: 
			// not used in AoW1, so discard them
			if (infoMap.ContainsKey(0x16))
				dword = br.ReadUInt32();
			if (infoMap.ContainsKey(0x17))
				dword = br.ReadUInt32();

			// Read description:
			if (infoMap.ContainsKey(0x18))
			{
				dword = br.ReadByte();
				StringBuilder strb = new StringBuilder();
				for (UInt32 i = 0; i < dword; ++i)
					strb.Append((char)br.ReadSByte());
				if (strb.Length > 0)
					Trace.WriteLine(strb.ToString());
			}

			if (infoMap.ContainsKey(0x19))
			{
				// Read expecial info : 2 – no blend, 3 – blend specified
				Byte infoByte = br.ReadByte();

				// unknown
				dword = br.ReadUInt32();

				// only if infoByte == 3
				if (infoByte == 3)
				{
					Byte blendOption = br.ReadByte();
					// unknown
					dword = br.ReadByte();
				}
				// unknown
				dword = br.ReadByte();
				Byte blendMode = br.ReadByte(); ; // 0 – 4 bmOpaque, bmAlpha, bmIntesity, bmShadow, bmBrighten
				_showMode = AoWShowMode.smBlended;
				switch (blendMode)
				{
					case 0x0: // draw opaque
						_blendMode = AoWBlendMode.bmAlpha;
						_showMode = AoWShowMode.smOpaque;
						break;
					case 0x1: _blendMode = AoWBlendMode.bmAlpha; break;
					case 0x2: _blendMode = AoWBlendMode.bmIntensity; break;
					case 0x3: _blendMode = AoWBlendMode.bmShadow; break;
					case 0x4: _blendMode = AoWBlendMode.bmBrighten; break;
				}
				if (infoByte == 3)
				{
					float blendValue = br.ReadSingle();
					_blendValue = (int)(100f * blendValue); // in AoWSM it's in range 0 - 1.0f, while AoW1 in 0 - 100
				}
			} // end blend data

			
			// Read pixelFormat:
			if (infoMap.ContainsKey(0x32))
			{
				dword = br.ReadUInt32();
				if (Is8bpp())
					_numPalette = (int)dword - 1;
				else if (dword != 0x56509310u)
					throw new Exception(string.Format("Invalid PixelFormat 0x{0:X} of image {1})", dword, _imageNumber));
			}

			//  Read bounding box:
			Int32 bbWidth = infoMap.ContainsKey(0x3C) ? br.ReadInt32() : 0;
			Int32 bbHeight = infoMap.ContainsKey(0x3D) ? br.ReadInt32() : 0;
			Int32 bbXOffset = infoMap.ContainsKey(0x3E) ? br.ReadInt32() : 0;
			Int32 bbYOffset = infoMap.ContainsKey(0x3F) ? br.ReadInt32() : 0;
			_boundingBox.Set(bbYOffset, bbXOffset, bbWidth + bbXOffset - 1, bbHeight + bbYOffset - 1);
			
			// Read background colour
			if (infoMap.ContainsKey(0x40))
				if (Is8bpp())
					_originalBackgroundColour = br.ReadByte();
				else
					_originalBackgroundColour = br.ReadUInt32();									

			// Read the image offset
			if (infoMap.ContainsKey(0x14))
				_imageOffset = br.ReadUInt32();

			return true;

		} // end ReadImageInfo

		public override bool ReadImageData(BinaryReader br, List<ColorPalette> palettes)
		{
			switch (_imageType)
			{
				case AoWImageType.Type01_Picture08_0x01:
					if (_numPalette >= palettes.Count)
						throw new InvalidOperationException("Invalid palette index");

					_original = new Bitmap(_cX, _cY, PixelFormat.Format8bppIndexed);
					_original.Palette = palettes[_numPalette];
					ReadPlain8BitImage(br);
					break;

				case AoWImageType.Type02_RLESprite08_0x02:
					if (_numPalette >= palettes.Count)
						throw new InvalidOperationException("Invalid palette index");

					_original = new Bitmap(_cX, _cY, PixelFormat.Format8bppIndexed);
					_original.Palette = palettes[_numPalette];
					ReadRLE8BitImage(br);
					break;

				case AoWImageType.Type03_Sprite08_0x03:
					if (_numPalette >= palettes.Count)
						throw new InvalidOperationException("Invalid palette index");

					_original = new Bitmap(_cX, _cY, PixelFormat.Format8bppIndexed);
					_original.Palette = palettes[_numPalette];
					ReadMasked8BitImage(br);
					break;

				case AoWImageType.Type16_Picture16_0x10:
					_original = new Bitmap(_cX, _cY, PixelFormat.Format16bppRgb565);
					ReadPlain16BitImage(br);
					break;

				case AoWImageType.Type17_RLESprite16_0x11:
					_original = new Bitmap(_cX, _cY, PixelFormat.Format16bppRgb565);
					ReadRLE16BitImage(br);
					break;

				case AoWImageType.Type18_TransparentRLESprite16_0x12:
					_original = new Bitmap(_cX, _cY, PixelFormat.Format16bppRgb565);
					ReadRLE16BitImage(br);
					break;

				case AoWImageType.Type22_Sprite16_0x16:
					_original = new Bitmap(_cX, _cY, PixelFormat.Format16bppRgb565);
					if (_isMasked)
						ReadMasked16BitImage(br);
					else
						ReadPlain16BitImage(br);					
					break;

				case AoWImageType.TypeAoWSM_AlphaMask:
					_original = new Bitmap(_cX, _cY, PixelFormat.Format32bppArgb);
					ReadAlphaMaskedImage(br);
					break;					

				default:
					throw new NotImplementedException();
			}

			// if there are sub images, then read their data too
			foreach (AoWBitmap elem in _subImageList)
				elem.ReadImageData(br, palettes);

			return true;
		} // end ReadImageData

		public override bool WriteImage(BinaryWriter infoStream, BinaryWriter imageStream)
		{
			throw new Exception("The method or operation is not implemented.");
		}		

	} // end AoW1Bitmap


	public class AoW2ImageLibrary : AoWImageLibrary
	{
		public override bool OpenIlb(string fileName, List<AoWBitmap> imageList)
		{
			using (FileStream fs = File.OpenRead(fileName))
			{
				BinaryReader br = new BinaryReader(fs);

				// first byte is the number of following word infos
				UInt32 wordFields = br.ReadByte();
				// the format of this header is weird:
				// because the choose to use word,
				// they may be too little to store address offset so dwords may be required.
				// In this case wordFields is 0x8n, and an additional dword follows
				// to specify the number of dword*2 infos
				UInt32 dwordFields = 0;
				if (wordFields > 0x80)
				{
					wordFields -= 0x80;
					dwordFields = br.ReadUInt32();
				}

				// the infos are made by two fields
				// 1 - an id
				// 2 - the offset you find their data AFTER this list of info
				SortedList<UInt32, UInt32> infoMap = new SortedList<UInt32, UInt32>();
				
				// Store them in a container
				for (int i = 0; i < wordFields; ++i)
				{
					UInt32 id = br.ReadByte();
					infoMap.Add(id, br.ReadByte());
				}
				for (int i = 0; i < dwordFields; ++i)
				{
					UInt32 id = br.ReadUInt32();
					infoMap.Add(id, br.ReadUInt32());
				}

				/*
				 * 0x0A ???
				 * 0x0B max number of elements - dword
				 * 0x0C palettes		-  ?
				 * 0x32+ image header	- vary
				 */
				if (infoMap.ContainsKey(0x0A))
				{
					UInt32 unknown = br.ReadUInt32();
					infoMap.Remove(0x0A);
				}

				// read the max number of elements
				// The images may have discontinued id (i.e.: 0 - 1 -3 - 5)
				// so the length of the list that should contain them all 
				// should be large to contains gap,
				// then the max is the following dword
				if (infoMap.ContainsKey(0x0B))
				{ 
					UInt32 lastElemNumber = br.ReadUInt32(); 
					infoMap.Remove(0x0B);
				}

				List<ColorPalette> palettes = new List<ColorPalette>();
				if (infoMap.ContainsKey(0x0C))
				{
					// read unknown data
					// For now I've no clue on meaning of interposed data,
					// but should be the palettes
					UInt32 unknownLength = infoMap.Values[1] - infoMap[0x0C];
					br.ReadBytes(0x6); // seem useless
					UInt32 numPalette = br.ReadByte();
					for (UInt32 i = 0; i < numPalette; ++i)
					{
						br.ReadBytes(0xF); // seem useless
						palettes.Add(AoW1Header.ReadPalette(br));
					}
					infoMap.Remove(0x0C);
				}

				// read image headers
				AoWBitmap model = PersistentData.Data.AoW1Default;

				foreach (UInt32 id in infoMap.Keys)
				{
					AoWBitmap elem = new AoW2Bitmap(model);
					elem.ImageNumber = (int)(id - 0x32u);					
					if (!elem.ReadImageInfo(br))
					{
						Debug.Assert(false);
					}
					// be sure there is enough space
					while (elem.ImageNumber >= imageList.Count)
						imageList.Add(null);
					imageList[elem.ImageNumber] = elem;
				}

				// read image data
				long startOffset = br.BaseStream.Position;
				foreach (AoW2Bitmap elem in imageList)
				{
					if (elem != null)
					{
						if (br.BaseStream.Position != startOffset + (long)elem.ImageOffset)
							br.BaseStream.Position = startOffset + (long)elem.ImageOffset;					
						elem.ReadImageData(br, palettes);
					}
				}

				// now convert to AoW1 Type
				for (int i = 0; i < imageList.Count; ++i)
				{
					AoW2Bitmap elem = (AoW2Bitmap)imageList[i];
					if (elem != null)
					{
						// the copy constructor make all the works
						imageList[i] = new AoW1Bitmap(elem);
						elem.Dispose();
					}
				}
			}
			return true;
		} // end OpenIlb

		public override bool MakeIlb(string filename, List<AoWBitmap> imageList)
		{
			throw new Exception("The method or operation is not implemented.");
		}

	} // AoW1ImageLibrary
}
