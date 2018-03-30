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
	public static class AoW1Constants
	{
		// this constants should be used for the clip_x fields for these special libraries
		public const UInt32 clip_X_TCMap =		0x00465EEC;
		public const UInt32 clip_X_ShieldsM =	0x00466014;
		public const UInt32 clip_X_Item =		0x00466214;
		public const UInt32 clip_X_Mountain =	0x00466214;		
		public const UInt32 clip_X_Structure =	0x00466214;		
	} // end AoWConstants

	public abstract class AoW1Header
	{
		public const UInt32 ilbIdentifier = 0x424C4904; // 4ILB
		public UInt32 unknown1 = 0x00;		// set in constructor
		public float version;				// version 4 as float
		public UInt32 length;				// length of this header
		public UInt32 numPalette = 0x0;

		public abstract void Write(BinaryWriter bw);
		public abstract void Read(BinaryReader br, List<AoWBitmap> imageList);		

		public static ColorPalette ReadPalette(BinaryReader br)
		{
			// Make a new Bitmap object to get its Palette.
			Bitmap dumb = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
			ColorPalette palette = dumb.Palette;   // Grab the palette
			dumb.Dispose();           // cleanup the source Bitmap

			// read the type of palette (always RGB):
			UInt32 dword = br.ReadUInt32();
			if (dword != 0x88801B18)
				throw new Exception(string.Format("Wrong palette type (0x{0:X})", dword));

			// read all 256 entries
			for (int i = 0; i < 256; ++i)
			{
				int r = br.ReadByte();
				int g = br.ReadByte();
				int b = br.ReadByte();
				br.ReadByte();
				palette.Entries[i] = Color.FromArgb(r, g, b);
			}
			return palette;
		}
	}

	public class AoW1HeaderV3 : AoW1Header
	{
		public const float VERSION = 3.0f;	// version 4 as float
		public const UInt32 LENGTH = 16;	// length of this header

		public AoW1HeaderV3()
		{
			version = VERSION;
			length = LENGTH;
		}

		public override void Write(BinaryWriter bw)
		{
			bw.Write(ilbIdentifier);
			bw.Write(unknown1);
			bw.Write(version);
			bw.Write(length);
			bw.Write(numPalette);	
		}

		public override void Read(BinaryReader br, List<AoWBitmap> imageList)
		{
			// length of this header
			UInt32 dword = br.ReadUInt32();
			if (dword != length)
				throw new Exception(string.Format("Wrong header v3 length ({0})", dword));

			numPalette = br.ReadUInt32();
			Debug.Assert(numPalette == 0);

			// read info data
			AoWBitmap model = PersistentData.Data.AoW1Default;
			AoWBitmap elem = null;
			while (true)
			{
				elem = new AoW1Bitmap(model);
				if (!elem.ReadImageInfo(br))
				{ 
					// End description section
					Debug.Assert(elem.ImageNumber == -1); // 0xFFFFFFFF);
					break;
				}
				// be sure there is enough space
				while (elem.ImageNumber >= imageList.Count)
					imageList.Add(null);
				imageList[elem.ImageNumber] = elem;
			}
			// no ReadImageData, because header v3 should have no distinct sections
		}
	} // end AoW1HeaderV3

	public class AoW1HeaderV4 : AoW1Header
	{
		public const float VERSION = 4.0f;	// version 4 as float
		public const UInt32 LENGTH = 24;	// length of this header
		public UInt32 imageDataOffset;		// The size of everything before the actual image data
		public UInt32 ilbFileSize;			// The size of this entire file

		public AoW1HeaderV4()
		{
			version = VERSION;
			length = LENGTH;
		}

		public override void Write(BinaryWriter bw)
		{
			bw.Write(ilbIdentifier);
			bw.Write(unknown1);
			bw.Write(version);
			bw.Write(length);
			bw.Write(imageDataOffset);
			bw.Write(ilbFileSize);
			bw.Write(numPalette);
		}

		public override void Read(BinaryReader br, List<AoWBitmap> imageList)
		{
			// length of this header
			UInt32 dword = br.ReadUInt32();
			if (dword != length)
				throw new Exception(string.Format("Wrong header v4 length ({0})", dword));

			imageDataOffset = br.ReadUInt32();
			ilbFileSize = br.ReadUInt32();
			Trace.WriteLine(string.Format("Ilb File Size {0}", ilbFileSize));

			numPalette = br.ReadUInt32();
			List<ColorPalette> palettes = new List<ColorPalette>();
			for (UInt32 i = 0; i < numPalette; ++i)
				palettes.Add(ReadPalette(br));
			Trace.WriteLine(string.Format("Read {0} palettes", palettes.Count));
			// Logger.LogMessage(MsgLevel.DEBUG, string.Format("Ilb contains {0} palettes", palettes.Count));					
            Debug.WriteLine(string.Format("Ilb contains {0} palettes", palettes.Count));

            // read info data
            AoWBitmap model = PersistentData.Data.AoW1Default;
			
			AoWBitmap elem = null;
			while (true)
			{
				elem = new AoW1Bitmap(model);
				if (!elem.ReadImageInfo(br))
				{ 
					// End description section
					if (elem.ImageNumber == -1) // 0xFFFFFFFF);
						break;
					else
					{
						// read a blank image probably
						br.ReadUInt32(); // throw away next dword
						continue;
					}
				}
				// be sure there is enough space
				while (elem.ImageNumber >= imageList.Count)
					imageList.Add(null);
				imageList[elem.ImageNumber] = elem;
			}		

			foreach (AoWBitmap elem1 in imageList)
			{
				if (elem1 != null)
					elem1.ReadImageData(br, palettes);
			}
		}		

	} // end AoW1HeaderV4

	[System.SerializableAttribute()]
	public class AoW1Bitmap : AoWBitmap
	{
		public AoW1Bitmap()
		{
		}

		#region ICloneable Members

		public override object Clone()
		{
			return new AoW1Bitmap(this);
		}

		#endregion

        public AoW1Bitmap(AoWBitmap src)
            : base(src)
        {
        }

		public AoW1Bitmap(Bitmap original, string name)
			: base(original, name)
			{
			}


		public override bool ReadImageInfo(BinaryReader br)
		{
			// this is read only by non-subimages

			// Read the unique image number
			_imageNumber = br.ReadInt32();
			if (_imageNumber == -1) // 0xFFFFFFFF); end
				return false;

			if (ReadImageInfoMain(br))
			{
				// Read end , always -1
				UInt32 dword = br.ReadUInt32();
				if (dword != 0xFFFFFFFFu)
					throw new Exception(string.Format("Invalid EndOfData 0x{0:X} of image {1})", dword, _imageNumber));
				return true;
			}
			return false;
		}

		protected bool ReadImageInfoMain(BinaryReader br)	
		{
			bool isComposite = false;
			// Read image type:
			UInt32 dword = br.ReadUInt32();
			if (dword == 0x100)
			{
				isComposite = true;
				Trace.WriteLine("Found a composite image");
                //Logger.LogMessage(MsgLevel.DEBUG, "Found a composite image");
                Debug.WriteLine("Found a composite image");
				dword = br.ReadUInt32();
			}
			else if (dword == 0x0)
			{
				Trace.WriteLine(string.Format("Found end of composite image ({0})", _imageNumber));
				// Logger.LogMessage(MsgLevel.DEBUG, string.Format("Found end of composite image ({0})", _imageNumber));
                Debug.WriteLine(string.Format("Found end of composite image ({0})", _imageNumber));
                _imageNumber = 0;
				return false;
			}

			switch (dword)
			{
				case 0x01: _imageType = AoWImageType.Type01_Picture08_0x01;		break;
				case 0x02: _imageType = AoWImageType.Type02_RLESprite08_0x02;	break;
				case 0x03: _imageType = AoWImageType.Type03_Sprite08_0x03;		break;
				case 0x10: _imageType = AoWImageType.Type16_Picture16_0x10;		break;
				case 0x11: _imageType = AoWImageType.Type17_RLESprite16_0x11;	break;
				case 0x12: _imageType = AoWImageType.Type18_TransparentRLESprite16_0x12; break;
				case 0x16: _imageType = AoWImageType.Type22_Sprite16_0x16;		break;
				default:
					throw new Exception(string.Format("Found unsupported type of image (0x{0:X})", dword));
			}

			// Read record format:
			byte subId = br.ReadByte();
			switch (subId)
			{
					// convert subtype 1 to 2
				case 1: _subType = AoWImageSubType.SubType02; break;
				case 2: _subType = AoWImageSubType.SubType02; break;
				case 3: _subType = AoWImageSubType.SubType03; break;
				default:
					throw new Exception(string.Format("Found unsupported subtype (0x{0:X}) of image {1}", subId, _imageNumber));
			}

			// Read bitmap filename:
			dword = br.ReadUInt32();
			StringBuilder strb = new StringBuilder();
			for (UInt32 i = 0; i < dword; ++i)
				strb.Append((char)br.ReadSByte());
			_name = strb.ToString();

			// Read image size:
			_cX = br.ReadInt32();
			_cY = br.ReadInt32();
			
			// Read image offset:
			_xShift = br.ReadInt32();
			_yShift = br.ReadInt32();

			// Read instance number:
			_instanceNumber = br.ReadInt32();

			/*Logger.LogMessage(MsgLevel.DEBUG,
				string.Format("Image {0}({1}): '{2}', {3}, sub {4}, {5}x{6}", _imageNumber, _instanceNumber, _name, _imageType, subId, _cX, _cY));*/
            Debug.WriteLine(string.Format("Image {0}({1}): '{2}', {3}, sub {4}, {5}x{6}", _imageNumber, _instanceNumber, _name, _imageType, subId, _cX, _cY));

			// Read LoadMode byte:
			dword = br.ReadByte();
			switch (dword)
			{
				case 0: _loadMode = AoWLoadMode.lmInstant; break;
				case 1: _loadMode =  AoWLoadMode.lmWhenUsed; break;
				case 2: _loadMode = AoWLoadMode.lmOnDemand; break;
				case 3: _loadMode = AoWLoadMode.lmWhenReferenced; break;
				default: _loadMode = AoWLoadMode.lmWhenUsed; break;
			}		

			// Read image data size:
			_imageDataSize = br.ReadInt32();
			if (_imageDataSize < 0)
				throw new Exception(string.Format("Invalid data size of image {0})", _imageNumber));

			// Read image data offset:
			// Not here if InfoByte is 1
			if (subId != 1)
				dword = br.ReadUInt32(); // I think I'll not use it

			// Read image size again:
			dword = br.ReadUInt32();
			//Debug.Assert((int)dword == _cX + _xShift);
			dword = br.ReadUInt32();
			//Debug.Assert((int)dword == _cY + _yShift);

			if (Is8bpp())
			{
				// Read unknownB byte:
				dword = br.ReadByte();
				Trace.WriteLine(string.Format("unknownB (0x{0:X})of image {1}", dword, _imageNumber));

				if (_subType == AoWImageSubType.SubType03)
				{
					// Read unknownC int:
					dword = br.ReadUInt32();
					Trace.WriteLine(string.Format("unknownC (0x{0:X})of image {1}", dword, _imageNumber));
					// Read unknownD int:
					dword = br.ReadUInt32();
					Trace.WriteLine(string.Format("unknownD (0x{0:X})of image {1}", dword, _imageNumber));
				}
			}
			else
			{
				if (_subType == AoWImageSubType.SubType03)
				{
					// Read drawMode:
					UInt32 drawMode = br.ReadUInt32();
					// Read blendValue:
					_blendValue = br.ReadInt32();

					UInt32 showMode = drawMode & 0x000000FF;
					UInt32 blendMode = (drawMode & 0x0000FF00) >> 8;
					switch (showMode)
					{
						case 0x0: _showMode = AoWShowMode.smOpaque; break;
						case 0x1: _showMode = AoWShowMode.smTransparent; break;
						case 0x2: _showMode = AoWShowMode.smBlended; break;
						default:
							throw new Exception(string.Format("Invalid ShowMode 0x{0:X} of image {1})", showMode, _imageNumber));
					}
					switch (blendMode)
					{
						case 0x0: _blendMode = AoWBlendMode.bmUser; break;
						case 0x1: _blendMode = AoWBlendMode.bmAlpha; break;
						case 0x2: _blendMode = AoWBlendMode.bmBrighten; break;
						case 0x3: _blendMode = AoWBlendMode.bmIntensity; break;
						case 0x4: _blendMode = AoWBlendMode.bmShadow; break;
						case 0x5: _blendMode = AoWBlendMode.bmLinearAlpha; break;
					}
				}
			}
			if (Is8bpp())
			{
				// Read palette index
				if (subId != 1)
					_numPalette = br.ReadInt32();
			}
			else
			{
				// Read pixelFormat:
				dword = br.ReadUInt32();
				if (dword != 0x56509310u)
					throw new Exception(string.Format("Invalid PixelFormat 0x{0:X} of image {1})", dword, _imageNumber));				
			}

			// See if there should be a bounding box present.
			switch (_imageType)
			{
				case AoWImageType.Type02_RLESprite08_0x02:
				case AoWImageType.Type03_Sprite08_0x03:
				case AoWImageType.Type17_RLESprite16_0x11:
				case AoWImageType.Type18_TransparentRLESprite16_0x12:
				case AoWImageType.Type22_Sprite16_0x16:
					{
						// Read bounding box:
						Int32 bbWidth = br.ReadInt32();
						Int32 bbHeight = br.ReadInt32();
						Int32 bbXOffset = br.ReadInt32();
						Int32 bbYOffset = br.ReadInt32();
						_boundingBox.Set(bbYOffset, bbXOffset, bbWidth + bbXOffset - 1, bbHeight + bbYOffset - 1);
						
						// Read background colour
						_originalBackgroundColour = br.ReadUInt32(); //RGB565 or index						
					}
					break;
			}

			// See if there should be clip_x
			switch (_imageType)
			{
				case AoWImageType.Type02_RLESprite08_0x02:
					// Read clip_x, seem useless, set to 0
					dword = br.ReadUInt32();
					Trace.WriteLine(string.Format("clip_x (0x{0:X})of image {1}", dword, _imageNumber));
					break;
				case AoWImageType.Type17_RLESprite16_0x11:
				case AoWImageType.Type18_TransparentRLESprite16_0x12:
					{
						dword = br.ReadUInt32();
						switch (dword)
						{
							case 0: _clipXHack = AoWClipXHack.None; break;
							//case AoW1Constants.clip_X_Structure:	_clipXHack = AoWClipXHack.AsStructure; break;
							//case AoW1Constants.clip_X_Mountain:	_clipXHack = AoWClipXHack.AsMountain; break;
							case AoW1Constants.clip_X_Item:			_clipXHack = AoWClipXHack.AsItem; break;
							case AoW1Constants.clip_X_ShieldsM:		_clipXHack = AoWClipXHack.AsShieldsM; break;
							case AoW1Constants.clip_X_TCMap:		_clipXHack = AoWClipXHack.AsTCMap; break;
							default: 
								_clipXHack = AoWClipXHack.None;
								// Logger.LogMessage( MsgLevel.DEBUG, string.Format("unknown clip_x (0x{0:X8})of image {1}", dword, _imageNumber));
                                Debug.WriteLine(string.Format("unknown clip_x (0x{0:X8})of image {1}", dword, _imageNumber));
                                break;
						}						
					}
					break;
			}

			// Read image data if subtype == 1
			if (subId == 1)
			{
				List<ColorPalette> palettes = null;
				if (Is8bpp())
				{
					// I think there should be the palette
					palettes = new List<ColorPalette>();
					palettes.Add(AoW1Header.ReadPalette(br));
				}
				ReadImageData(br, palettes);
			}

			if (isComposite)
			{
				// read composite images
				// read info data
				AoW1Bitmap elem = null;
				while (true)
				{
					elem = new AoW1Bitmap();
					elem.ImageNumber = _imageNumber;
					if (!elem.ReadImageInfoMain(br))
					{
						// End composite section
						Debug.Assert(elem.ImageNumber == 0);
						break;
					}
					_subImageList.Add(elem);
				}
			}
			
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
					ReadPlain16BitImage(br);
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
			// write the unique image number
			infoStream.Write((UInt32)_imageNumber);
			// if composite, write 0x100
			if (_subImageList.Count > 0)
				infoStream.Write(0x100u);

			WriteImageData(imageStream);
			WriteImageInfo(infoStream);

			if (_subImageList.Count > 0)
			{
				// if there are sub images, then write their data too
				foreach (AoWBitmap elem in _subImageList)
				{
					AoW1Bitmap elem1 = (AoW1Bitmap)elem;
					elem1.WriteImageData(imageStream);
					elem1.WriteImageInfo(infoStream);
				}
				
				// write end of composite sequence
				infoStream.Write(0x0u);			
			}
			// Write end , always -1
			infoStream.Write(0xFFFFFFFFu);

			return true;
		} // end WriteToStreams

		// select the right way to save the image bytes
		private bool WriteImageData(BinaryWriter stream)
		{
			switch (_imageType)
			{
				case AoWImageType.Type01_Picture08_0x01:
					if (_resized.PixelFormat != PixelFormat.Format8bppIndexed)
						throw new InvalidOperationException("Image set to Picture08 but PixelFormat is not Format8bppIndexed)");
					_boundingBox.Set(0, 0, 0, 0);
					WritePlain8BitImage(stream);
					return true;

				case AoWImageType.Type02_RLESprite08_0x02:
					if (_resized.PixelFormat != PixelFormat.Format8bppIndexed)
						throw new InvalidOperationException("Image set to RLESprite08 but PixelFormat is not Format8bppIndexed");

					WriteRLE8BitImage(stream);
					return true;

				case AoWImageType.Type03_Sprite08_0x03:
					if (_resized.PixelFormat != PixelFormat.Format8bppIndexed)
						throw new InvalidOperationException("Image set to Picture08 but PixelFormat is not Format8bppIndexed)");
					
					WriteMasked8BitImage(stream);
					return true;

				case AoWImageType.Type16_Picture16_0x10:
					if (_resized.PixelFormat != PixelFormat.Format16bppRgb565)
						throw new InvalidOperationException("Image set to Picture16 but PixelFormat is not Format16bppRgb565");
					_boundingBox.Set(0, 0, 0, 0);
					WritePlain16BitImage(stream);
					return true;	

				case AoWImageType.Type17_RLESprite16_0x11:
					if (_resized.PixelFormat != PixelFormat.Format16bppRgb565)
						throw new InvalidOperationException("Image set to RLESprite16 but PixelFormat is not Format16bppRgb565");
					WriteRLE16BitImage(stream);
					return true;

				case AoWImageType.Type18_TransparentRLESprite16_0x12:
					if (_resized.PixelFormat != PixelFormat.Format16bppRgb565)
						throw new InvalidOperationException("Image set to TransparentRLESprite16 but PixelFormat is not Format16bppRgb565");
					WriteRLE16BitImage(stream);
					return true;

				case AoWImageType.Type22_Sprite16_0x16:
					if (_resized.PixelFormat != PixelFormat.Format16bppRgb565)
						throw new InvalidOperationException("Image set to Sprite16 but PixelFormat is not Format16bppRgb565");
					WritePlain16BitImage(stream);
					return true;

				default:
					throw new NotImplementedException();					
			}
		} // end WriteImageData

		private void WriteImageInfo(BinaryWriter infoStream)
		{			
			UInt32 imageType = 0;
			switch (_imageType)
			{
				case AoWImageType.Type01_Picture08_0x01:
					imageType = 0x01; break;
				case AoWImageType.Type02_RLESprite08_0x02:
					imageType = 0x02; break;
				case AoWImageType.Type03_Sprite08_0x03:
					imageType = 0x03; break;
				case AoWImageType.Type16_Picture16_0x10:
					imageType = 0x10; break;
				case AoWImageType.Type17_RLESprite16_0x11:
					imageType = 0x11; break;
				case AoWImageType.Type18_TransparentRLESprite16_0x12:
					imageType = 0x12; break;
				case AoWImageType.Type22_Sprite16_0x16:
					imageType = 0x16; break;
			}
			// Write image type:
			infoStream.Write(imageType);

			// Write record format:
			int subId = (_subType == AoWImageSubType.SubType02) ? 2 : 3;
			infoStream.Write((Byte)subId);
						
			// Write bitmap filename:
			UInt32 stringLength = (UInt32)_name.Length;
			infoStream.Write(stringLength);
			for (int i = 0; i < stringLength; ++i)
				infoStream.Write((SByte)_name[i]);

			// Write image size:
			infoStream.Write((UInt32)_resized.Width);
			infoStream.Write((UInt32)_resized.Height);

			// Write image offset:
			infoStream.Write((UInt32)_xShift);
			infoStream.Write((UInt32)_yShift);

			// Write instance number:
			infoStream.Write((UInt32)_instanceNumber);

			// Write LoadMode byte:
			Byte loadMode = 0;
			switch (_loadMode)
			{					
				case AoWLoadMode.lmInstant: loadMode = 0; break;
				case AoWLoadMode.lmWhenUsed: loadMode = 1; break;
				case AoWLoadMode.lmOnDemand: loadMode = 2; break;
				case AoWLoadMode.lmWhenReferenced: loadMode = 3; break;
			}
			infoStream.Write(loadMode);

			// Write image data size:
			infoStream.Write((UInt32)_imageDataSize);

			// Write image data offset:
			// Not here if InfoByte is 1, however I write only type 2 or 3
			infoStream.Write((UInt32)_imageDataOffset);

			// Write image size again:
			infoStream.Write((UInt32)(_resized.Width + _xShift));
			infoStream.Write((UInt32)(_resized.Height + _yShift));

			if (Is8bpp())
			{
				// Write unknownB byte:
				infoStream.Write((Byte)0);
				
				// 
				if (_subType == AoWImageSubType.SubType03)
				{
					// Write unknownC int:
					infoStream.Write((UInt32)1);
					// Write unknownD int:
					infoStream.Write((UInt32)1);					
				}
				
			}
			else
			{
				if (_subType == AoWImageSubType.SubType03)
				{
					UInt32 drawMode = 0;
					switch (_showMode)
					{
						case AoWShowMode.smOpaque: drawMode = 0x0; break;
						case AoWShowMode.smTransparent: drawMode = 0x1; break;
						case AoWShowMode.smBlended: drawMode = 0x2; break;
					}
					switch (_blendMode)
					{
						case AoWBlendMode.bmUser: drawMode += 0x0 << 8; break;
						case AoWBlendMode.bmAlpha: drawMode += 0x1 << 8; break;
						case AoWBlendMode.bmBrighten: drawMode += 0x2 << 8; break;
						case AoWBlendMode.bmIntensity: drawMode += 0x3 << 8; break;
						case AoWBlendMode.bmShadow: drawMode += 0x4 << 8; break;
						case AoWBlendMode.bmLinearAlpha: drawMode += 0x5 << 8; break;
					}
					// Write drawMode:
					infoStream.Write(drawMode);
					// Write blendValue:
					infoStream.Write((UInt32)_blendValue);				
				}				
			}

			if (Is8bpp())
			{
				// Write palette index (note that I use only one palette):
				infoStream.Write((UInt32)0);
			}
			else
			{
				// Write pixelFormat:
				infoStream.Write(0x56509310u); // always 565
			}

			// See if there should be a bounding box present.
			switch (_imageType)
			{
				case AoWImageType.Type02_RLESprite08_0x02:
				case AoWImageType.Type03_Sprite08_0x03:
				case AoWImageType.Type17_RLESprite16_0x11:
				case AoWImageType.Type18_TransparentRLESprite16_0x12:
				case AoWImageType.Type22_Sprite16_0x16:
					{
						UInt32 bbXOffset = (UInt32)_boundingBox.Left;
						UInt32 bbYOffset = (UInt32)_boundingBox.Top;
						UInt32 bbWidth = (UInt32)(_boundingBox.Right - _boundingBox.Left + 1);
						UInt32 bbHeight = (UInt32)(_boundingBox.Bottom - _boundingBox.Top + 1);

						// Write bounding box:
						infoStream.Write(bbWidth);
						infoStream.Write(bbHeight);
						infoStream.Write(bbXOffset);
						infoStream.Write(bbYOffset);

						// Write background colour
						UInt32 back = GetBackgroundColorForWrite();
						infoStream.Write((UInt32)back);
					}
					break;
			}

			// See if there should be clip_x
			switch (_imageType)
			{
				case AoWImageType.Type02_RLESprite08_0x02:				
					// Write clip_x, seem useless, set to 0
					infoStream.Write(0u);
					break;
				case AoWImageType.Type17_RLESprite16_0x11:
				case AoWImageType.Type18_TransparentRLESprite16_0x12:
					{
						UInt32 clipX = 0u;
						switch (_clipXHack)
						{
							case AoWClipXHack.AsItem: clipX = AoW1Constants.clip_X_Item; break;
							case AoWClipXHack.AsMountain: clipX = AoW1Constants.clip_X_Mountain; break;
							case AoWClipXHack.AsShieldsM: clipX = AoW1Constants.clip_X_ShieldsM; break;
							case AoWClipXHack.AsStructure: clipX = AoW1Constants.clip_X_Structure; break;
							case AoWClipXHack.AsTCMap: clipX = AoW1Constants.clip_X_TCMap; break;
						}
						infoStream.Write(clipX);
					}
					break;
			}

		} // end WriteToStream

	} // end AoW1Bitmap


	public class AoW1ImageLibrary : AoWImageLibrary
	{
		public override bool OpenIlb(string fileName, List<AoWBitmap> imageList)
		{
			using (FileStream fs = File.OpenRead(fileName))
			{
				BinaryReader br = new BinaryReader(fs);

				UInt32 dword;
				// check magic number
				dword = br.ReadUInt32();
				if (dword != AoW1HeaderV4.ilbIdentifier)
					return false;
					
				// unknown1
				dword = br.ReadUInt32();
				Trace.WriteLine(string.Format("Header unknown1 0x{0:X}", dword));
				// float version					
				float version = br.ReadSingle();
				Trace.WriteLine(string.Format("Header version {0}", version));
				// Logger.LogMessage(MsgLevel.DEBUG, string.Format("Header version {0}", version));
                Debug.WriteLine(string.Format("Header version {0}", version));
                if (version == AoW1HeaderV3.VERSION)
				{
					AoW1HeaderV3 header = new AoW1HeaderV3();
					header.Read(br, imageList);
				}
				else if (version == AoW1HeaderV4.VERSION)
				{
					AoW1HeaderV4 header = new AoW1HeaderV4();
					header.Read(br, imageList);
				}
				else throw new Exception(string.Format("Unsupported ilb version ({0})", version));
			}
			return true;
		} // end OpenIlb

		public override bool MakeIlb(string fileName, List<AoWBitmap> imageList)
		{
			AoWBitmap first = null;
			AoWBitmap first8bbp = null;
			foreach (AoWBitmap elem in imageList)
				if (elem != null)
				{
					if (first == null)
						first = elem;
					if (elem.Resized.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						first8bbp = elem;
						break;
					}
				}
			if (first == null)
				return false;

			try
			{
				using (MemoryStream tempMemory = new MemoryStream())
				{
					using (MemoryStream imageMemory = new MemoryStream())
					{
						BinaryWriter tempStream = new BinaryWriter(tempMemory);
						BinaryWriter imageStream = new BinaryWriter(imageMemory);

						// set the number of palettes (0 - 1 for this editor):
						UInt32 nrOfPalettes = first8bbp != null ? 1u : 0u;
						// write the palette
						if (nrOfPalettes > 0)
						{
							// write the type of palette (always RGB):
							tempStream.Write(0x88801B18);
							ColorPalette palette = first8bbp.Resized.Palette;
							foreach (Color c in palette.Entries)
							{
								tempStream.Write((Byte)c.R);
								tempStream.Write((Byte)c.G);
								tempStream.Write((Byte)c.B);
								tempStream.Write((Byte)0);
							}
							// be sure to write 256 entries
							for (int i = palette.Entries.Length; i < 256; ++i)
								tempStream.Write(0);
						}

						// Write image info data and bitmap data to separate streams:						
						int imageNumber = 0;
						foreach (AoWBitmap elem in imageList)
						{
							if (elem != null)
							{
								if (elem.ImageNumber != imageNumber)
									elem.ImageNumber = imageNumber;
								elem.WriteImage(tempStream, imageStream);
							}
							++imageNumber;
						}
						tempStream.Write(0xFFFFFFFF); // End description section

						AoW1HeaderV4 header = new AoW1HeaderV4();
						// guess this may be true
						header.unknown1 = (nrOfPalettes > 0) ? 0x0040DB00u : 0x0040F000u;
						header.imageDataOffset = header.length + 4u + (UInt32)tempStream.BaseStream.Length;
						header.ilbFileSize = header.imageDataOffset + (UInt32)imageStream.BaseStream.Length;
						header.numPalette = nrOfPalettes;

						using (FileStream fs = new FileStream(fileName, FileMode.Create))
						{
							BinaryWriter bw = new BinaryWriter(fs);

							// Assemble the library file:
							// First: header record ...
							header.Write(bw);						

							// Second: palettes and image info data ...
							tempMemory.WriteTo(fs);
							// Finally: image bitmap data ...
							imageMemory.WriteTo(fs);

							// Logger.LogMessage(MsgLevel.INFO, string.Format("{0} - Made. ({1})", Path.GetFileName(fileName), fileName));
                            Debug.WriteLine(string.Format("{0} - Made. ({1})", Path.GetFileName(fileName), fileName));
                            return true;
						}
					}
				}				
			}
			catch (Exception ex)
			{
                //Logger.LogMessage(MsgLevel.FAULT, ex.Message);
                //Logger.LogMessage(MsgLevel.FAULT, string.Format("{0} - Failure. ({1})", Path.GetFileName(fileName), fileName));		
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(string.Format("{0} - Failure. ({1})", Path.GetFileName(fileName), fileName));
            }
			return false;
		}
		
	} // AoW1ImageLibrary
}