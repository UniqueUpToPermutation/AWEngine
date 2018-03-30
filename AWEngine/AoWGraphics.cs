using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

using IlbEditorNet;

namespace AoWGraphics
{
	public class AowRect
	{
		int top;
		int left;
		int right;
		int bottom;

		public int Top
		{
			get { return top; }
			set { top = value; }
		}
		public int Left
		{
			get { return left; }
			set { left = value; }
		}
		public int Right
		{
			get { return right; }
			set { right = value; }
		}
		public int Bottom
		{
			get { return bottom; }
			set { bottom = value; }
		}

		public int Width
		{
			get { return right - left + 1; }			
		}
		public int Height
		{
			get { return bottom - top + 1; }
		}	

		public AowRect()
		{
		}

		public AowRect(int top, int left, int right, int bottom)
		{
			Set(top, left, right, bottom);
		}

		public void Set(int top, int left, int right, int bottom)
		{
			this.top = top;
			this.left = left;
			this.right = right;
			this.bottom = bottom;
		}

		public void Set(AowRect rect)
		{
			this.top = rect.top;
			this.left = rect.left;
			this.right = rect.right;
			this.bottom = rect.bottom;
		}

		public bool IsValid()
		{
			return right > left && bottom > top;
		}
	}
    
	public enum AoWImageType
	{
		Type02_RLESprite08_0x02,
		Type16_Picture16_0x10,
		Type17_RLESprite16_0x11,
		Type18_TransparentRLESprite16_0x12,
		Type22_Sprite16_0x16,
		Type01_Picture08_0x01,
		Type03_Sprite08_0x03,
		TypeAoWSM_AlphaMask
		/*,
		unused
		BitMask_Type19_0x13,
		Shadow_Type20_0x14,
		TransparentPicture16_Type21_0x15
		 */
	}

	public enum AoWImageSubType
	{
		SubType02,
		SubType03
	}

	public enum AoWLoadMode
	{
		lmInstant, 
		lmWhenUsed, 
		lmOnDemand, 
		lmWhenReferenced
	}

	public enum AoWShowMode
	{
		smOpaque,
		smTransparent,
		smBlended
	}

	public enum AoWBlendMode
	{
		bmUser,
		bmAlpha,
		bmBrighten,
		bmIntensity,
		bmShadow,
		bmLinearAlpha
	}

	public enum AoWClipXHack
	{
		None,
		AsShieldsM,
		AsItem,
		AsMountain,
		AsTCMap,
		AsStructure
	}

    public enum ScalingType
    {
        None,
        Proportional,
        Fixed
    }

	public enum AoWAutoShift
	{
		None,
		BottomRight,
		BottomLeft,
		TopRight		
	}

	public abstract class  AoWBitmap : IDisposable, ICloneable
	{	
		// used by editor
		protected Bitmap _original = null;
		protected Bitmap _resized = null;
		protected Int32 _cX;
		protected Int32 _cY;

		protected ScalingType _scalingType = ScalingType.None;
		protected InterpolationMode _interpolationMode = InterpolationMode.High;
		protected float _scalingFactor = 1.0f;
        protected Int32 _scaleXTo;
        protected Int32 _scaleYTo;
		protected bool _dropTransparent = false;
		protected Int32 _dropThreshold = 16;
		
		public Bitmap Original
		{
			get { return _original; }
			set { _original = value; }
		}
		public Bitmap Resized
		{
			get { return _resized; }
			set { _resized = value; }
		}
		public Int32 CX
		{
			get { return _cX; }
			set { _cX = value; }
		}
		public Int32 CY
		{
			get { return _cY; }
			set { _cY = value; }
		}
		
        public ScalingType ScalingType
        {
            get { return _scalingType; }
            set 
			{ 
				_scalingType = value;
				if (_scalingType == ScalingType.None || _scalingType == ScalingType.Fixed)
					_scalingFactor = 1.0f;
			}
        }
        public InterpolationMode InterpolationMode
        {
            get { return _interpolationMode; }
            set { _interpolationMode = value; }
        }
        public float ScalingFactor
        {
            get { return _scalingFactor; }
            set { _scalingFactor = value; }
        }
        public Int32 ScaleXTo
        {
            get { return _scaleXTo; }
            set { _scaleXTo = value; }
        }
        public Int32 ScaleYTo
        {
            get { return _scaleYTo; }
            set { _scaleYTo = value; }
        }
        public bool DropTransparent
		{
			get { return _dropTransparent; }
			set { _dropTransparent = value; }
		}
		public Int32 DropThreshold
        {
            get { return _dropThreshold; }
            set { _dropThreshold = value; }
        }
				
		// used to encode
		protected AoWImageType _imageType = AoWImageType.Type02_RLESprite08_0x02;
		protected AoWImageSubType _subType = AoWImageSubType.SubType03;
		protected String _name;
		protected Int32 _imageNumber;
		protected Int32 _instanceNumber;
		protected Int32 _imageDataSize;
		protected Int32 _imageDataOffset;
		protected Int32 _numPalette;
		protected AowRect _boundingBox = new AowRect();		
		protected UInt32 _originalBackgroundColour = 0;
		protected UInt32 _resizedBackgroundColour = 0;

		protected Int32 _xShift;
		protected Int32 _yShift;
		protected AoWAutoShift _autoShift = AoWAutoShift.None;
		protected AoWLoadMode _loadMode = AoWLoadMode.lmWhenUsed;
		// used for blend
		protected AoWShowMode _showMode = AoWShowMode.smOpaque;
		protected AoWBlendMode _blendMode = AoWBlendMode.bmAlpha;
		protected Int32 _blendValue = 0;

		// force this value for type 17 and 18
		// because I don't  know its meaning
		protected AoWClipXHack _clipXHack = AoWClipXHack.None;

		// subimage list
		protected List<AoWBitmap> _subImageList = new List<AoWBitmap>();
		
		public AoWImageType ImageType
		{
			get { return _imageType; }
			set { _imageType = value; }
		}
		public AoWImageSubType SubType
		{
			get { return _subType; }
			set { _subType = value; }
		}
		public String Name
		{
			get { return _name; }
			set { _name = value; }
		}
		public Int32 ImageNumber
		{
			get { return _imageNumber; }
			set { _imageNumber = value; }
		}
		public Int32 InstanceNumber
		{
			get { return _instanceNumber; }
			set { _instanceNumber = value; }
		}
		public Int32 ImageDataSize
		{
			get { return _imageDataSize; }
			set { _imageDataSize = value; }
		}
		public Int32 ImageDataOffset
		{
			get { return _imageDataOffset; }
			set { _imageDataOffset = value; }
		}
		public Int32 NumPalette
		{
			get { return _numPalette; }
			set { _numPalette = value; }
		}		
		public AowRect BoundingBox
		{
			get { return _boundingBox; }
			set { _boundingBox = value; }
		}
		public UInt32 OriginalBackgroundColour
		{
			get { return _originalBackgroundColour; }
			set { _originalBackgroundColour = value; }
		}
		public UInt32 ResizedBackgroundColour
		{
			get { return _resizedBackgroundColour; }
			set { _resizedBackgroundColour = value; }
		}
		public Int32 XShift
		{
			get { return _xShift; }
			set { _xShift = value; }
		}
		public Int32 YShift
		{
			get { return _yShift; }
			set { _yShift = value; }
		}

		public AoWLoadMode LoadMode
		{
			get { return _loadMode; }
			set { _loadMode = value; }
		}
		public AoWShowMode ShowMode
		{
			get { return _showMode; }
			set { _showMode = value; }
		}
		public AoWBlendMode BlendMode
		{
			get { return _blendMode; }
			set { _blendMode = value; }
		}
		public Int32 BlendValue
		{
			get { return _blendValue; }
			set { _blendValue = value; }
		}
		public AoWClipXHack ClipXHack
		{
			get { return _clipXHack; }
			set { _clipXHack = value; }
		}
		public AoWAutoShift AutoShift
		{
			get { return _autoShift; }
			set { _autoShift = value; }
		}
		
		public List<AoWBitmap> SubImageList
		{
			get { return _subImageList; }
		}

		// state utility
		public bool Is8bpp()
		{
			return _imageType == AoWImageType.Type01_Picture08_0x01
				|| _imageType == AoWImageType.Type02_RLESprite08_0x02
				|| _imageType == AoWImageType.Type03_Sprite08_0x03;
		}

		public bool IsPlain()
		{
			return _imageType == AoWImageType.Type16_Picture16_0x10
				|| _imageType == AoWImageType.Type01_Picture08_0x01;
		}

		// constructors
		public AoWBitmap()
		{
			_name = string.Empty;
		}

		#region ICloneable Members

		public virtual object Clone()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		public AoWBitmap(AoWBitmap src)
		{
			_name = string.Empty;
			if (src != null)
			{
				if (src._original != null)
					_original = (Bitmap)src._original.Clone();
				if (src._resized != null)
					_resized = (Bitmap)src._resized.Clone();
				_cX = src._cX;
				_cY = src._cY;

				_imageType = src.ImageType;
				_subType = src.SubType;
				_name = src._name;
				_imageNumber = src._imageNumber;
				_instanceNumber = src._instanceNumber;
				_imageDataSize = src._imageDataSize;
				_numPalette = src._numPalette;
				_boundingBox.Set(src._boundingBox);
				_originalBackgroundColour = src._originalBackgroundColour;
				_resizedBackgroundColour = src._resizedBackgroundColour;
				_xShift = src._xShift;
				_yShift = src._yShift;
				_autoShift = src._autoShift;
				_loadMode = src._loadMode;
				_showMode = src.ShowMode;
				_blendMode = src.BlendMode;
				_blendValue = src.BlendValue;
				_clipXHack = src.ClipXHack;

				_scalingType = src._scalingType;
				_interpolationMode = src._interpolationMode;
				_scalingFactor = src._scalingFactor;
				_scaleXTo = src._scaleXTo;
				_scaleYTo = src._scaleYTo;
				_dropTransparent = src._dropTransparent;
				_dropThreshold = src._dropThreshold;
				
				if (src._subImageList.Count > 0)
				{
					foreach (AoWBitmap subElem in src._subImageList)
						_subImageList.Add((AoWBitmap)subElem.Clone());
				}
			}           
        }

		public AoWBitmap(Bitmap original, string name)
		{
            AoWBitmap aow = PersistentData.Data.AoW1Default;
            _imageType = aow.ImageType;
            _subType = aow.SubType;
			_loadMode = aow._loadMode;
            _showMode = aow.ShowMode;
            _blendMode = aow.BlendMode;
            _blendValue = aow.BlendValue;
            _clipXHack = aow.ClipXHack;

            _scalingType = aow._scalingType;
            _interpolationMode = aow._interpolationMode;
            _scalingFactor = aow._scalingFactor;
            _scaleXTo = aow._scaleXTo;
            _scaleYTo = aow._scaleYTo;
			_dropTransparent = aow._dropTransparent;
            _dropThreshold = aow._dropThreshold;			

			Reset(original, name);
		}

		public void Reset(Bitmap original, string name)
		{
			if (_original != null)
			{
				_original.Dispose();
				_original = null;
			}
			if (_resized != null)
			{
				_resized.Dispose();
				_resized = null;
			}
			_name = name.Substring(name.LastIndexOf(@"\") + 1); ;
			_original = original;

			_boundingBox.Set(0, 0, 0, 0);
			_originalBackgroundColour = ToRGB888(_original.GetPixel(0, 0));
			_resizedBackgroundColour = ToRGB888(PersistentData.Data.AoW1Default.ResizedBackgroundColour);
		}		

		#region IDisposable Pattern

		bool disposed = false;
		// Implement IDisposable.
		public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

		~AoWBitmap()      
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
					if (_original != null)
					{
						_original.Dispose();
						_original = null;
					}
					if (_resized != null)
					{
						_resized.Dispose();
						_resized = null;
					}
					foreach (AoWBitmap elem in _subImageList)
						elem.Dispose();
				}
				disposed = true;
			}
		}
		#endregion

		public abstract bool ReadImageInfo(BinaryReader stream);
		public abstract bool ReadImageData(BinaryReader stream, List<ColorPalette> palettes);
		
		public abstract bool WriteImage(BinaryWriter infoStream, BinaryWriter imageStream);

		// Resizing function: remove alpha transpareny, apply filters, resize 
		public bool Resize()
        {
            if (_resized != null)
            {
                _resized.Dispose();
                _resized = null;
            }

			if (_original == null)
				return false;

			_cX = _original.Width;
			_cY = _original.Height;

			float resH = _original.HorizontalResolution;
			float resV = _original.VerticalResolution;

			// better copy original transparent color because ApplyFiltering may change it
			UInt32 back = _originalBackgroundColour;

			// always make a copy at 32bit
			Bitmap bmp1 = new Bitmap(_cX, _cY);
			bmp1.SetResolution(resH, resV);
			using (Graphics gr = Graphics.FromImage(bmp1))
			{
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(ToARGB8888(back))))
				{
					gr.FillRectangle(brush, 0, 0, _cX, _cY);
				}
				gr.DrawImage(_original, new Rectangle(0, 0, _cX, _cY), new Rectangle(0, 0, _cX, _cY), GraphicsUnit.Pixel);
			}	

			// if the original image have transparency, may be better to drop it, on user request
			if (_dropTransparent)
				DropTraparency(bmp1, _dropThreshold, back);
			
			/*if (PersistentData.Data.ApplyFiltering)
			{
				
				// apply the color modification
				List<AForge.Imaging.Filters.IFilter> filterList = PersistentData.Data.FilterList;
				if (filterList.Count > 0)
				{
					// save the position of the transparent color 
					int tX = 0, tY = 0;
					if (!IsPlain())			
						FindFirstTransparent(bmp1, back, out tX, out tY);
				
					foreach (AForge.Imaging.Filters.IFilter filter in filterList)
					{
						// apply filter to the image
						Bitmap bmp2 = filter.Apply(bmp1);
						bmp1.Dispose();
						bmp1 = bmp2;
					}
					bmp1.SetResolution(resH, resV);

					if (!IsPlain()) // update the background color
						back = ToRGB888(bmp1.GetPixel(tX, tY));
				}
			}*/

			if (!IsPlain())
			{
				// make transparent the background color
				bmp1.MakeTransparent(Color.FromArgb(ToARGB8888(back)));
				bmp1.SetResolution(resH, resV);		
			}

			switch (_scalingType)
			{
				case ScalingType.Fixed:
					{
						_cX = _scaleXTo;
						_cY = _scaleYTo;

						// create the target bmp
						_resized = new Bitmap(_cX, _cY);
						_resized.SetResolution(resH, resV);
						using (Graphics gr = Graphics.FromImage(_resized))
						{
							gr.InterpolationMode = _interpolationMode;
							gr.DrawImage(bmp1, new Rectangle(0, 0, _cX, _cY), new Rectangle(0, 0, bmp1.Width, bmp1.Height), GraphicsUnit.Pixel);
						}						
					}
					break;

                case ScalingType.Proportional:
                    {
                        int tempW = _cX, tempH = _cY;
						ApplyScaleFactor(ref tempW, ref tempH); // this overwrite _cX, _cY
						if (tempW != bmp1.Width || tempH != bmp1.Height)
                        {
							// ApplyScaleFactor signals that the original should be padded
							Bitmap  bmp2 = new Bitmap(tempW, tempH);
							bmp2.SetResolution(resH, resV);
							using (Graphics gr = Graphics.FromImage(bmp2))
                            {
								gr.DrawImage(bmp1, 0, 0);
                            }
							if (!IsPlain())
							{
								// repeat this on new image
								bmp2.MakeTransparent(Color.FromArgb(ToARGB8888(back)));
								// MakeTransparent cause change in resolution
								bmp2.SetResolution(resH, resV);
							}
							bmp1.Dispose();
							bmp1 = bmp2;
                        }
                        
                        // create the target bmp
                        _resized = new Bitmap(_cX, _cY);
						_resized.SetResolution(resH, resV);                    
						using (Graphics gr = Graphics.FromImage(_resized))
                        {
							gr.InterpolationMode = _interpolationMode;
                            gr.ScaleTransform(_scalingFactor, _scalingFactor, MatrixOrder.Append);
                            gr.DrawImage(bmp1, 0, 0);
                        }						                       
                    }
                    break;

                case ScalingType.None:
                default:
                    {
                        // create the target bmp
                        _resized = new Bitmap(_cX, _cY);
						_resized.SetResolution(resH, resV);
                        using (Graphics gr = Graphics.FromImage(_resized))
                        {
							gr.InterpolationMode = _interpolationMode;
							gr.DrawImage(bmp1, 0, 0);
                        }						
                    }
                    break;
            }

			bmp1.Dispose(); // free

			// Always remove the transparent background
			DropTraparency(_resized, _dropThreshold, _resizedBackgroundColour);

            // if != 8bpp then the conversion may be done here directly
			if (!Is8bpp())
				ConvertTo16bppRgb565Format();

			foreach (AoWBitmap subImages in _subImageList)
				subImages.Resize();

            return true;          
        }

        private void ApplyScaleFactor(ref int cx, ref int cy)
        {
            _scalingFactor = ((int)(_scalingFactor * 10)) / 10f;
            int roundedFactor = (int)(_scalingFactor * 10);

            if (roundedFactor == 1)
            {
               _cX = cx;
               _cY = cy;
            }
            else if (roundedFactor == 5) // mean 0.5
            {
                if (cx % 2 > 0) cx += 1;
                if (cy % 2 > 0) cy += 1;
                _cX = cx / 2;
                _cY = cy / 2;                
            }
            else
            {
                // pad until can / 10
                int q = (cx * roundedFactor) % 10;
                while (q > 0)
                {
                    ++cx;
                    q = (cx * roundedFactor) % 10;
                }
                q = (cy * roundedFactor) % 10;
                while (q > 0)
                {
                    ++cy;
                    q = (cy * roundedFactor) % 10;
                }

                _cX = (cx * roundedFactor) / 10;
                _cY = (cy * roundedFactor) / 10;                
            }
        }

		// generic not to repeat code
		public class BoundingBoxCalculator<T> where T : struct
		{
			// this will scan both 8bit and 16bit images
			public AowRect Calc(T[] values, T back, int stride, int width, int height)
			{
				Debug.Assert(stride * height == values.Length);

				IEquatable<T> eqBack = (IEquatable<T>)back;

				int x = 0, y = 0;
				// this will ensure it will always shrinked
				AowRect box = new AowRect(height, width, 0, 0);

				// Find first coloured pixel (top to bottom, left to right):
				// assume 0 is the entry of the background in palette
				int i = 0; // index of the first pixel
				for (; y < height; ++y)
				{
					i = y * stride;
					for (x = 0; x < width; ++x, ++i)
					{
						if (!eqBack.Equals(values[i]))
							break; // found
					}
					if (x != width)
						break;
				}

				if (y == height)
				{
					// This is a completely empty picture
					// leave the BoundingBox as set before
				}
				else
				{
					box.Top = y;
					box.Left = x;

					// Find first coloured pixel (bottom to top, right to left):					
					for (y = height - 1; y >= 0; --y)
					{
						i = (stride * y) + width - 1; // last pixel of the line						
						for (x = width - 1; x >= 0; --x, --i)
						{
							if (!eqBack.Equals(values[i]))
								break; // found
						}
						if (x >= 0)
							break;
					}

					box.Bottom = y;
					if (x < box.Left)
					{
						// for now the old pixel was the rightest
						box.Right = box.Left;
						box.Left = x;
					}
					else box.Right = x;

					// Now find if the box should be enlarged
					for (y = box.Top; y <= box.Bottom; ++y)
					{
						// Left
						i = stride * y; // first pixel of the line
						for (x = 0; x < width; ++x, ++i)
						{
							if (!eqBack.Equals(values[i]))
							{
								if (x < box.Left)
									box.Left = x;
								break;
							}
						}

						// Right
						i = (stride * y) + width - 1; // last pixel of the line						
						for (x = width - 1; x >= 0; --x, --i)
						{
							if (!eqBack.Equals(values[i]))
							{
								if (x > box.Right)
									box.Right = x;
								break;
							}
						}
					}
				}

				return box;
			} // end Calc

#if USE_UNSAFE
			unsafe
			// this will scan only for 8bit images
			public AowRect Calc(byte* values, byte back, int stride, int width, int height)
			{
				IEquatable<byte> eqBack = (IEquatable<byte>)back;

				int x = 0, y = 0;
				// this will ensure it will always shrinked
				AowRect box = new AowRect(height, width, 0, 0);

				// Find first coloured pixel (top to bottom, left to right):
				// assume 0 is the entry of the background in palette
				for (; y < height; ++y)
				{
					byte* pV = values + y * stride;
					for (x = 0; x < width; ++x, ++pV)
					{
						if (!eqBack.Equals(*pV))
							break; // found
					}
					if (x != width)
						break;
				}

				if (y == height)
				{
					// This is a completely empty picture
					// leave the BoundingBox as set before
				}
				else
				{
					box.Top = y;
					box.Left = x;

					// Find first coloured pixel (bottom to top, right to left):					
					for (y = height - 1; y >= 0; --y)
					{
						byte* pV = values + ((stride * y) + width - 1); // last pixel of the line						
						for (x = width - 1; x >= 0; --x, --pV)
						{
							if (!eqBack.Equals(*pV))
								break; // found
						}
						if (x >= 0)
							break;
					}

					box.Bottom = y;
					if (x < box.Left)
					{
						// for now the old pixel was the rightest
						box.Right = box.Left;
						box.Left = x;
					}
					else box.Right = x;

					// Now find if the box should be enlarged
					for (y = box.Top; y <= box.Bottom; ++y)
					{
						// Left
						byte* pV = values + y * stride;
						for (x = 0; x < width; ++x, ++pV)
						{
							if (!eqBack.Equals(*pV))
							{
								if (x < box.Left)
									box.Left = x;
								break;
							}
						}

						// Right
						pV = values + ((stride * y) + width - 1); // last pixel of the line						
						for (x = width - 1; x >= 0; --x, --pV)
						{
							if (!eqBack.Equals(*pV))
							{
								if (x > box.Right)
									box.Right = x;
								break;
							}
						}
					}
				}

				return box;
			} // end Calc
#endif
		
#if USE_UNSAFE
			// this will scan only for 16bit images
			unsafe
			public AowRect Calc(Int16* values, Int16 back, int stride, int width, int height)
			{
				IEquatable<Int16> eqBack = (IEquatable<Int16>)back;

				int x = 0, y = 0;
				// this will ensure it will always shrinked
				AowRect box = new AowRect(height, width, 0, 0);

				// Find first coloured pixel (top to bottom, left to right):
				// assume 0 is the entry of the background in palette
				for (; y < height; ++y)
				{
					Int16* pV = values + y * stride;
					for (x = 0; x < width; ++x, ++pV)
					{
						if (!eqBack.Equals(*pV))
							break; // found
					}
					if (x != width)
						break;
				}

				if (y == height)
				{
					// This is a completely empty picture
					// leave the BoundingBox as set before
				}
				else
				{
					box.Top = y;
					box.Left = x;

					// Find first coloured pixel (bottom to top, right to left):					
					for (y = height - 1; y >= 0; --y)
					{
						Int16* pV = values + ((stride * y) + width - 1); // last pixel of the line						
						for (x = width - 1; x >= 0; --x, --pV)
						{
							if (!eqBack.Equals(*pV))
								break; // found
						}
						if (x >= 0)
							break;
					}

					box.Bottom = y;
					if (x < box.Left)
					{
						// for now the old pixel was the rightest
						box.Right = box.Left;
						box.Left = x;
					}
					else box.Right = x;

					// Now find if the box should be enlarged
					for (y = box.Top; y <= box.Bottom; ++y)
					{
						// Left
						Int16* pV = values + y * stride;
						for (x = 0; x < width; ++x, ++pV)
						{
							if (!eqBack.Equals(*pV))
							{
								if (x < box.Left)
									box.Left = x;
								break;
							}
						}

						// Right
						pV = values + ((stride * y) + width - 1); // last pixel of the line						
						for (x = width - 1; x >= 0; --x, --pV)
						{
							if (!eqBack.Equals(*pV))
							{
								if (x > box.Right)
									box.Right = x;
								break;
							}
						}
					}
				}

				return box;
			} // end Calc
#endif
		}

		/*
		 * Now the encoding functions
		 */

		// for Type02_RLESprite08_0x02
		protected void WriteRLE8BitImage(BinaryWriter imageStream)
		{
			if (_resized == null)
				return;
			if (_resized.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidOperationException("Bitmap is not Format8bppIndexed");

			_imageDataOffset = (int)imageStream.BaseStream.Position;
			BitmapData bmpData = null;
			try
			{
				int h = _resized.Height;
				int w = _resized.Width;
				//Debug.Assert(h < 256);
				//Debug.Assert(w < 256);

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = _resized.LockBits(rect, ImageLockMode.ReadWrite, _resized.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride;
				int area = stride * h;
				Byte[] pixelData = new Byte[area];  // This should be large enough
#if USE_UNSAFE
				unsafe
				{
					Byte* idxValues = (Byte*)ptr;
#else
				Byte[] idxValues = new Byte[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif
					// Set Bounding Box				
					Byte back = (_savedTX < 0) ? idxValues[0] : idxValues[_savedTX + _savedTY * stride];

					BoundingBoxCalculator<Byte> boxer = new BoundingBoxCalculator<Byte>();
					AowRect newBox = boxer.Calc(idxValues, back, stride, w, h);
					_boundingBox.Set(newBox);

					for (int Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
					{
						Byte backgroundPixels = 0;
						UInt32 scanLineLength = 0;
						int offset = Y * stride;
						// Encode the scanline
						for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
						{
							if (idxValues[offset + X] == back)
								backgroundPixels += 1;
							else
							{
								if (backgroundPixels > 0)
								{
									pixelData[scanLineLength] = back;
									pixelData[scanLineLength + 1] = backgroundPixels;
									backgroundPixels = 0;
									scanLineLength = scanLineLength + 2;
								}
								pixelData[scanLineLength] = idxValues[offset + X];
								scanLineLength = scanLineLength + 1;
							}
						}

						// Trailing background pixels ...
						if (backgroundPixels > 0)
						{
							pixelData[scanLineLength] = back;
							pixelData[scanLineLength + 1] = backgroundPixels;
							scanLineLength = scanLineLength + 2;
						}

						UInt32 encodedScanLineLength = scanLineLength + 4; //sizeof(encodedScanLineLength);
						imageStream.Write(encodedScanLineLength);

						for (int i = 0; i < scanLineLength; ++i)
							imageStream.Write(pixelData[i]);
					}
#if USE_UNSAFE
				}
#endif
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					_resized.UnlockBits(bmpData);
				_imageDataSize = (int)imageStream.BaseStream.Position - _imageDataOffset;			
			}
		} // end Encode8BitImage

		// for Type17_RLESprite16_0x11 and Type18_TransparentRLESprite16_0x12
		protected void WriteRLE16BitImage(BinaryWriter imageStream)
		{
			if (_resized == null)
				return;			
			if (_resized.PixelFormat != PixelFormat.Format16bppRgb565)
				throw new InvalidOperationException("Bitmap is not Format16bppRgb565");

			_imageDataOffset = (int)imageStream.BaseStream.Position;
			BitmapData bmpData = null;
			try
			{
				int h = _resized.Height;
				int w = _resized.Width;
				
				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = _resized.LockBits(rect, ImageLockMode.ReadWrite, _resized.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride / 2;
				int area = stride * h;
				Int16[] pixelData = new Int16[area];  // This should be large enough

#if USE_UNSAFE
				unsafe
				{
					Int16* idxValues = (Int16*)ptr;
#else
				Int16[] idxValues = new Int16[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Set Bounding Box
					Int16 back = (_savedTX < 0)
						? (Int16)(UInt16)ToRGB565(_resizedBackgroundColour)
						: idxValues[_savedTX + _savedTY * stride];

					BoundingBoxCalculator<Int16> boxer = new BoundingBoxCalculator<Int16>();
					AowRect newBox = boxer.Calc(idxValues, back, stride, w, h);
					_boundingBox.Set(newBox);

					for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
					{
						Int16 backgroundPixels = 0;
						UInt32 scanLineLength = 0;
						offset = Y * stride;
						// Encode the scanline
						for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
						{
							if (idxValues[offset + X] == back)
								backgroundPixels += 1;
							else
							{
								if (backgroundPixels > 0)
								{
									pixelData[scanLineLength] = back;
									pixelData[scanLineLength + 1] = (Int16)(2 * backgroundPixels);
									backgroundPixels = 0;
									scanLineLength = scanLineLength + 2;
								}
								pixelData[scanLineLength] = idxValues[offset + X];
								scanLineLength = scanLineLength + 1;
							}
						}

						// Trailing background pixels ...
						if (backgroundPixels > 0)
						{
							pixelData[scanLineLength] = back;
							pixelData[scanLineLength + 1] = (Int16)(2 * backgroundPixels);
							scanLineLength = scanLineLength + 2;
						}

						UInt32 encodedScanLineLength = 2 * scanLineLength + 4; //sizeof(encodedScanLineLength);
						imageStream.Write(encodedScanLineLength);

						if ((scanLineLength % 2) > 0)
						{
							pixelData[scanLineLength] = back;
							scanLineLength += 1;
						}

						for (int i = 0; i < scanLineLength; ++i)
							imageStream.Write(pixelData[i]);
					}
#if USE_UNSAFE
				}
#endif

			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					_resized.UnlockBits(bmpData);
				_imageDataSize = (int)imageStream.BaseStream.Position - _imageDataOffset;
			}
		} // end Encode16BitImage

		// Seem that AoW1 read bad the palette
		// for Type01_Picture08_0x01
		protected void WritePlain8BitImage(BinaryWriter imageStream)
		{
			if (_resized == null)
				return;
			if (_resized.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidOperationException("Bitmap is not Format8bppIndexed)");

			_imageDataOffset = (int)imageStream.BaseStream.Position;
			BitmapData bmpData = null;
			try
			{
				int h = _resized.Height;
				int w = _resized.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = _resized.LockBits(rect, ImageLockMode.ReadWrite, _resized.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride;
				int area = stride * h;//w * h;

#if USE_UNSAFE
				unsafe
				{
					Byte* idxValues = (Byte*)ptr;
#else	
				Byte[] idxValues = new Byte[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif
					if (_boundingBox.IsValid())
					{
						// _boundingBox is not empty
						for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
						{
							offset = Y * stride;
							for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
								imageStream.Write(idxValues[X + offset]);
						}
					}
					else
					{
						for (int offset = 0, Y = 0; Y < h; ++Y)
						{
							offset = Y * stride;
							for (int X = 0; X < w; ++X)
								imageStream.Write(idxValues[X + offset]);
						}
					}
#if USE_UNSAFE
				}
#endif
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                // Logger.LogMessage(MsgLevel.FAULT, ex.Message);
            }
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					_resized.UnlockBits(bmpData);
				_imageDataSize = (int)imageStream.BaseStream.Position - _imageDataOffset;
			}
		} // end WritePlain8BitImage

		// To be tested with AoW1
		// for Type03_Sprite08_0x03		
		protected void WriteMasked8BitImage(BinaryWriter imageStream)
		{
			if (_resized == null)
				return;
			if (_resized.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidOperationException("Bitmap is not Format8bppIndexed)");

			_imageDataOffset = (int)imageStream.BaseStream.Position;
			BitmapData bmpData = null;
			try
			{
				int h = _resized.Height;
				int w = _resized.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = _resized.LockBits(rect, ImageLockMode.ReadWrite, _resized.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride;
				int area = stride * h;//w * h;

#if USE_UNSAFE
				unsafe
				{
					Byte* idxValues = (Byte*)ptr;
#else	
				Byte[] idxValues = new Byte[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Set Bounding Box
					Byte back = (_savedTX < 0) ? idxValues[0] : idxValues[_savedTX + _savedTY * stride];
					// This need to calculate the bounding box
					BoundingBoxCalculator<Byte> boxer = new BoundingBoxCalculator<Byte>();
					AowRect newBox = boxer.Calc(idxValues, back, stride, w, h);
					_boundingBox.Set(newBox);

					if (_boundingBox.IsValid())
					{
						// _boundingBox is not empty

						// padding value
						int ddx = 0;
						if (_boundingBox.Width >= 0x20)
							ddx = (1 + (_boundingBox.Width - 1) / 8) * 8;
						else
							ddx = (1 + (_boundingBox.Width - 1) / 4) * 4;
						Byte pad = (Byte)(ddx - _boundingBox.Width);
						Byte colourByte = 0;
						Byte backCount = 0;

						for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
						{
							offset = Y * stride;
							for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
							{
								colourByte = idxValues[X + offset];
								if (colourByte == back)
								{
									++backCount;
								}
								else
								{
									if (backCount > 0)
									{
										imageStream.Write(back);
										imageStream.Write(backCount);
										backCount = 0;
									}
									imageStream.Write(colourByte);
								}
								if (X == _boundingBox.Right)
									backCount += pad;
							}
							if (Y == _boundingBox.Bottom)
							{
								if (backCount > 0)
								{
									imageStream.Write(back);
									imageStream.Write(backCount);
								}
							}
						}
					}
#if USE_UNSAFE
				}
#endif
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                //Logger.LogMessage(MsgLevel.FAULT, ex.Message);
            }
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					_resized.UnlockBits(bmpData);
				_imageDataSize = (int)imageStream.BaseStream.Position - _imageDataOffset;
			}
		} // end WriteMasked8BitImage

		// for Type16_Picture16_0x10 and Type22_Sprite16_0x16
		protected void WritePlain16BitImage(BinaryWriter imageStream)
		{
			if (_resized == null)
				return;
			
			if (_resized.PixelFormat != PixelFormat.Format16bppRgb565)
				throw new InvalidOperationException("Bitmap is not Format16bppRgb565");

			_imageDataOffset = (int)imageStream.BaseStream.Position;
			BitmapData bmpData = null;
			try
			{
				int h = _resized.Height;
				int w = _resized.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = _resized.LockBits(rect, ImageLockMode.ReadWrite, _resized.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// beware to the padding it add to have an even stride!!!
				int stride = bmpData.Stride / 2;
				int area = stride * h;//w * h;
#if USE_UNSAFE
				unsafe
				{
					Int16* idxValues = (Int16*)ptr;
#else
				Int16[] idxValues = new Int16[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					if (_imageType == AoWImageType.Type22_Sprite16_0x16)
					{
						// Set Bounding Box
						Int16 back = (_savedTX < 0)
							? (Int16)(UInt16)ToRGB565(_resizedBackgroundColour)
							: idxValues[_savedTX + _savedTY * stride];
						// This need to calculate the bounding box
						BoundingBoxCalculator<Int16> boxer = new BoundingBoxCalculator<Int16>();
						AowRect newBox = boxer.Calc(idxValues, back, stride, w, h);
						_boundingBox.Set(newBox);
					}

					if (_boundingBox.Right > _boundingBox.Left)
					{
						// _boundingBox is not empty
						for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
						{
							offset = Y * stride;
							for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
								imageStream.Write(idxValues[X + offset]);
						}
					}
					else
					{
						for (int offset = 0, Y = 0; Y < h; ++Y)
						{
							offset = Y * stride;
							for (int X = 0; X < w; ++X)
								imageStream.Write(idxValues[X + offset]);
						}
					}
#if USE_UNSAFE
				}
#endif
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					_resized.UnlockBits(bmpData);
				_imageDataSize = (int)imageStream.BaseStream.Position - _imageDataOffset;
			}
		} // end WritePlain16BitImage

		/*
		 * Now the decoding functions
		 */

		protected void ReadRLE8BitImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidOperationException("Bitmap is not Format8bppIndexed");

			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;
				
				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride;
				int area = stride * h;

#if USE_UNSAFE
				unsafe
				{
					Byte* idxValues = (Byte*)ptr;
#else
				Byte[] idxValues = new Byte[area];
				// Copy the indexed values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif
					// Set all to background color
					Byte back = (Byte)_originalBackgroundColour;
					for (int i = 0; i < area; ++i)
						idxValues[i] = back;

					// Now convert the background color to UInt32
					_originalBackgroundColour = ToRGB888(bmp.Palette.Entries[_originalBackgroundColour]);
					_resizedBackgroundColour = _originalBackgroundColour;

					if (_imageDataSize > 0)
					{
						for (int Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
						{
							// Decode the scanline
							int offset = Y * stride;
							int X = _boundingBox.Left;
							UInt32 scanLineLength = br.ReadUInt32() - 4u;

							for (; scanLineLength > 0; --scanLineLength)
							{
								Byte pixelData = br.ReadByte();
								if (back == pixelData)
								{
									--scanLineLength;
									pixelData = br.ReadByte();
									for (; pixelData > 0; --pixelData, ++X)
										idxValues[offset + X] = back;
								}
								else
								{
									idxValues[offset + X] = pixelData;
									++X;
								}
							}
						}
					}
#if USE_UNSAFE
				}
#else
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmp != null)
					bmp.UnlockBits(bmpData);
			}
		} // end Decode8BitImage

		protected void ReadRLE16BitImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format16bppRgb565)
				throw new InvalidOperationException("Bitmap is not Format16bppRgb565");

			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;
			
				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride / 2;
				int area = stride * h;
#if USE_UNSAFE
				unsafe
				{
					Int16* idxValues = (Int16*)ptr;
#else
				Int16[] idxValues = new Int16[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Set all to background color
					Int16 back = (Int16)(UInt16)_originalBackgroundColour;
					for (int i = 0; i < area; ++i)
						idxValues[i] = back;

					// Now convert the background color to UInt32
					_originalBackgroundColour = ToRGB888(_originalBackgroundColour);
					_resizedBackgroundColour = _originalBackgroundColour;

					if (_imageDataSize > 0)
					{
						for (int Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
						{
							// Decode the scanline
							int offset = Y * stride;
							int X = _boundingBox.Left;
							UInt32 scanLineLength = (br.ReadUInt32() - 4u) / 2u;
							bool skipDummyBackground = (scanLineLength % 2 > 0);

							for (; scanLineLength > 0; --scanLineLength)
							{
								Int16 pixelData = br.ReadInt16();
								if (back == pixelData)
								{
									--scanLineLength;
									pixelData = br.ReadInt16();
									for (; pixelData > 0; pixelData -= 2, ++X)//pixelData -= 2; // ??? (the ??= are in AoWCGE)
										idxValues[offset + X] = back;
								}
								else
								{
									idxValues[offset + X] = pixelData;
									++X;
								}
							}

							if (skipDummyBackground)
								br.ReadInt16();
						}
					}
#if USE_UNSAFE
				}
#else			
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} // end Decode16BitImage

		// Sadly seem that plain 8 bit image works only with AoWSM
		protected void ReadPlain8BitImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidOperationException("Bitmap is not Format8bppIndexed");

            // Logger.LogMessage(MsgLevel.DEBUG, string.Format("Image Type of {0} ({1}) converted from {2} to {3}", _name, _imageNumber, _imageType, AoWImageType.Type16_Picture16_0x10));
            Debug.WriteLine(string.Format("Image Type of {0} ({1}) converted from {2} to {3}", _name, _imageNumber, _imageType, AoWImageType.Type16_Picture16_0x10));

            _imageType = AoWImageType.Type16_Picture16_0x10;

			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride;
				int area = stride * h;
#if USE_UNSAFE
				unsafe
				{
					Byte* idxValues = (Byte*)ptr;
#else	
				Byte[] idxValues = new Byte[area];
				// Copy the indexed values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif
				if (_imageDataSize > 0)
				{
					if (_boundingBox.IsValid())
					{
						// _boundingBox is not empty
						for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
						{
							offset = Y * stride;
							for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
								idxValues[X + offset] = br.ReadByte();
						}
					}
					else
					{
						for (int offset = 0, Y = 0; Y < h; ++Y)
						{
							offset = Y * stride;
							for (int X = 0; X < w; ++X)
								idxValues[X + offset] = br.ReadByte();
						}
					}
				}
					// use the first pixel as background color
					_originalBackgroundColour = ToRGB888(bmp.Palette.Entries[idxValues[0]]);
					_resizedBackgroundColour = _originalBackgroundColour;
#if USE_UNSAFE
				}
#else
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} // end ReadPlain8BitImage

		// Seem valid only for AoWSM, however I cannot write it back, so convert type to RLESprite

		protected void ReadMasked8BitImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
				throw new InvalidOperationException("Bitmap is not Format8bppIndexed");

            // Logger.LogMessage(MsgLevel.DEBUG, string.Format("Image Type of {0} ({1}) converted from {2} to {3}", _name, _imageNumber, _imageType, AoWImageType.Type02_RLESprite08_0x02));
            Debug.WriteLine(string.Format("Image Type of {0} ({1}) converted from {2} to {3}", _name, _imageNumber, _imageType, AoWImageType.Type02_RLESprite08_0x02));

            _imageType = AoWImageType.Type02_RLESprite08_0x02;

			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride;
				int area = stride * h;
#if USE_UNSAFE
				unsafe
				{
					Byte* idxValues = (Byte*)ptr;
#else
				Byte[] idxValues = new Byte[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Set all to background color
					Byte back = (Byte)_originalBackgroundColour;
					for (int i = 0; i < area; ++i)
						idxValues[i] = back;
					// Now convert the background color to UInt32
					_originalBackgroundColour = ToRGB888(bmp.Palette.Entries[_originalBackgroundColour]);
					_resizedBackgroundColour = _originalBackgroundColour;

					if (_imageDataSize > 0)
					{
						if (_boundingBox.IsValid())
						{
							// _boundingBox is not empty

							// padding value
							int ddx = 0;
							if (_boundingBox.Width >= 0x20)
								ddx = (1 + (_boundingBox.Width - 1) / 8) * 8;
							else
								ddx = (1 + (_boundingBox.Width - 1) / 4) * 4;
							Byte colourByte = 0;
							Byte backCount = 0;
							UInt32 byteRead = 0;
							for (int offset, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
							{
								offset = Y * stride + _boundingBox.Left;
								for (int X = 0; X < ddx; ++X)
								{
									if (backCount == 0)
									{
										colourByte = br.ReadByte();
										++byteRead;
										if (colourByte != back)
										{
											idxValues[offset] = colourByte;
											++offset;
										}
										else
										{
											if (byteRead >= _imageDataSize)
												break;
											backCount = br.ReadByte();
											++byteRead;
											++offset;
											--backCount;
										}
									}
									else
									{
										--backCount;
										++offset;
									}
									if (byteRead >= _imageDataSize)
										break;
								}
								if (byteRead >= _imageDataSize)
									break;
							}
						}
					}
#if USE_UNSAFE
				}
#else		
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} // end ReadMasked8BitImage		

		protected void ReadPlain16BitImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format16bppRgb565)
				throw new InvalidOperationException("Bitmap is not Format16bppRgb565");
			
			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// beware to the padding it add to have an even stride!!!
				int stride = bmpData.Stride / 2;
				int area = stride * h;
#if USE_UNSAFE
				unsafe
				{
					Int16* idxValues = (Int16*)ptr;
#else
				Int16[] idxValues = new Int16[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Choose first pixel as background color
					Int16 back = (Int16)(UInt16)_originalBackgroundColour;
					if (_imageType == AoWImageType.Type16_Picture16_0x10)
						back = idxValues[0];
					// Set all to background color				
					for (int i = 0; i < area; ++i)
						idxValues[i] = back;
					// Now convert the background color to UInt32
					_originalBackgroundColour = ToRGB888((UInt32)back);
					_resizedBackgroundColour = _originalBackgroundColour;

					if (_imageDataSize > 0)
					{
						if (_boundingBox.IsValid())
						{
							// _boundingBox is not empty
							for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
							{
								offset = Y * stride;
								for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
									idxValues[X + offset] = br.ReadInt16();
							}
						}
						else
						{
							for (int offset = 0, Y = 0; Y < h; ++Y)
							{
								offset = Y * stride;
								for (int X = 0; X < w; ++X)
									idxValues[X + offset] = br.ReadInt16();
							}
						}
					}
#if USE_UNSAFE
				}
#else		
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} // end ReadPlain16BitImage

		protected void ReadMasked16BitImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format16bppRgb565)
				throw new InvalidOperationException("Bitmap is not Format16bppRgb565");

			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// beware to the padding it add to have an even stride!!!
				int stride = bmpData.Stride / 2;
				int area = stride * h;
#if USE_UNSAFE
				unsafe
				{
					Int16* idxValues = (Int16*)ptr;
#else
				Int16[] idxValues = new Int16[area];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Set all to background color
					Int16 back = (Int16)(UInt16)_originalBackgroundColour;
					for (int i = 0; i < area; ++i)
						idxValues[i] = back;
					// Now convert the background color to UInt32
					_originalBackgroundColour = ToRGB888(_originalBackgroundColour);
					_resizedBackgroundColour = _originalBackgroundColour;

					if (_imageDataSize > 0)
					{
						if (_boundingBox.IsValid())
						{
							// _boundingBox is not empty
							for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
							{
								offset = Y * stride;
								for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
									br.ReadByte(); // throw away the mask
								for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
									idxValues[X + offset] = br.ReadInt16();
							}
						}
						else
						{
							for (int offset = 0, Y = 0; Y < h; ++Y)
							{
								offset = Y * stride;
								for (int X = 0; X < w; ++X)
									br.ReadByte(); // throw away the mask
								for (int X = 0; X < w; ++X)
									idxValues[X + offset] = br.ReadInt16();
							}
						}
					}
#if USE_UNSAFE
				}
#else	
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} // end ReadMasked16BitImage


		protected void ReadAlphaMaskedImage(BinaryReader br)
		{
			Bitmap bmp = _original;
			if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
				throw new InvalidOperationException("Bitmap is not Format32bppArgb");

			BitmapData bmpData = null;
			try
			{
				int h = bmp.Height;
				int w = bmp.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, w, h);
				bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				int stride = bmpData.Stride / 4;
				int area = stride * h;
#if USE_UNSAFE
				unsafe
				{
					Int32* idxValues = (Int32*)ptr;
#else
				Int32[] idxValues = new Int32[area];
				// Copy the indexed values into the array.
				Marshal.Copy(ptr, idxValues, 0, area);
#endif

					// Set all to background color
					_originalBackgroundColour = _originalBackgroundColour & 0x00FFFFFF;
					Int32 back = (Int32)(_originalBackgroundColour); // ARGB
					for (int i = 0; i < area; ++i)
						idxValues[i] = back;
					// Now convert the background color to UInt32
					_resizedBackgroundColour = _originalBackgroundColour;

					if (_imageDataSize > 0)
					{
						if (_boundingBox.IsValid())
						{
							// _boundingBox is not empty
							for (int offset = 0, Y = _boundingBox.Top; Y <= _boundingBox.Bottom; ++Y)
							{
								offset = Y * stride;
								for (int X = _boundingBox.Left; X <= _boundingBox.Right; ++X)
									idxValues[X + offset] = back + br.ReadByte() << 24;
							}
						}
						else
						{
							for (int offset = 0, Y = 0; Y < h; ++Y)
							{
								offset = Y * stride;
								for (int X = 0; X < w; ++X)
									idxValues[X + offset] = back + br.ReadByte();
							}
						}
					}
#if USE_UNSAFE
				}
#else
				// Copy the RGB values back to the bitmap
				Marshal.Copy(idxValues, 0, ptr, area);
#endif
			}
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} // end ReadAlphaMaskedImage

		// Color management


		protected UInt32 ToRGB565(UInt32 rgb888)
		{
			UInt32 rgb565 =
				  (((rgb888 & 0x00FF0000) >> 19) << 11)	// r
				+ (((rgb888 & 0x0000FF00) >> 10) << 5)	// g
				+ ((rgb888 & 0x000000FF) >> 3);		// b
			return rgb565;
		}

		protected UInt32 ToRGB888(UInt32 rgb565)
		{
			UInt32 rgb888 =
				  ((rgb565 & 0x0000F800) << 8)	// r
				+ ((rgb565 & 0x000007E0) << 5)	// g
				+ ((rgb565 & 0x0000001F) << 3);	// b
			return rgb888;
		}

		protected UInt32 ToRGB888(Color c)
		{
			UInt32 rgb888 = ((UInt32)c.ToArgb()) & 0x00FFFFFF;
			return rgb888;
		}

		protected Int32 ToARGB8888(UInt32 c)
		{
			Int32 argb8888 = (Int32)(c | 0xFF000000);
			return argb8888;
		}

		// this may be done directly here
#if USE_UNSAFE
		unsafe
#endif
		private void ConvertTo16bppRgb565Format()
		{
			Bitmap bmp1 = _resized;
			Bitmap bmp2 = new Bitmap(bmp1.Width, bmp1.Height, PixelFormat.Format16bppRgb565);
			bmp2.SetResolution(bmp1.HorizontalResolution, bmp1.VerticalResolution);
						
			// Lock the bitmap's bits.
			Rectangle rect = new Rectangle(0, 0, bmp1.Width, bmp1.Height);
			BitmapData bmpData1 = bmp1.LockBits(rect, ImageLockMode.ReadWrite, bmp1.PixelFormat);
			BitmapData bmpData2 = bmp2.LockBits(rect, ImageLockMode.ReadWrite, bmp2.PixelFormat);
			try
			{
				// Get the address of the first line.
				IntPtr ptr1 = bmpData1.Scan0;
				IntPtr ptr2 = bmpData2.Scan0;

				// Declare an array to hold the bytes of the bitmap.
				// This code is specific to a bitmap with 32 bits per pixels.
				int h = bmp1.Height, w = bmp1.Width;
				int stride1 = bmpData1.Stride / 4;
				int stride2 = bmpData2.Stride / 2;			
				
#if USE_UNSAFE
				// try with pointers
				// Fastest!!!!
				int* pData1 = (int*)ptr1;
				short* pData2 = (short*)ptr2;
				for (int y = 0; y < h; ++y)
				{
					int* p1 = pData1 + y * stride1;
					short* p2 = pData2 + y * stride2;
					for (int x = 0; x < w; ++x, ++p1, ++p2)
					{
						UInt32 v = (UInt32)(*p1);
						UInt32 rgb565 =
							(((v & 0x00FF0000) >> 19) << 11)	// r
							+ (((v & 0x0000FF00) >> 10) << 5)	// g
							+ ((v & 0x000000FF) >> 3);		// b

						*p2 = (Int16)(UInt16)rgb565;
					}
				}				
#else
				// Copy the values into the array.
				int area1 = bmpData1.Height * stride1;
				int area2 = bmpData2.Height * stride2;
				int[] values1 = new int[area1];
				short[] values2 = new short[area2];

				Marshal.Copy(ptr1, values1, 0, area1);
				Marshal.Copy(ptr2, values2, 0, area2);

				for (int y = 0; y < bmp1.Height; ++y)
				{
					int offset1 = y * stride1;
					int offset2 = y * stride2;
					for (int x = 0; x < bmp1.Width; ++x)
					{
						UInt32 v = (UInt32)(values1[offset1 + x]);
						UInt32 rgb565 =
							(((v & 0x00FF0000) >> 19) << 11)	// r
							+ (((v & 0x0000FF00) >> 10) << 5)	// g
							+ ((v & 0x000000FF) >> 3);		// b

						values2[offset2 + x] = (Int16)(UInt16)rgb565;
					}
				}
				

				// Copy the values back to the bitmap
				Marshal.Copy(values2, 0, ptr2, area2);
#endif
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                //Logger.LogMessage(MsgLevel.FAULT, ex.Message);
            }
			finally
			{
				// Unlock the bits.
				bmp1.UnlockBits(bmpData1);
				bmp2.UnlockBits(bmpData2);
			}


			_resized.Dispose();
			_resized = bmp2;
		} // end ConvertTo16bppRgb565Format

		// store where the transparency is
		protected int _savedTX = 0, _savedTY = 0;

#if USE_UNSAFE
		unsafe
#endif
		private void DropTraparency(Bitmap bmp, int threshold, uint background)
		{
			if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
				throw new ArgumentException("Invalid pixel format");

			// 
			byte r = (byte)((background & 0x00FF0000) >> 16);
			byte g = (byte)((background & 0x0000FF00) >> 8);
			byte b = (byte)(background & 0x000000FF);


			// Faster!!!!
			// Lock the bitmap's bits.
			Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
			try
			{
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// Declare an array to hold the bytes of the bitmap.
				// This code is specific to a bitmap with 32 bits per pixels.
				int bytes = bmp.Width * bmp.Height * 4;
#if USE_UNSAFE
				byte* rgbValues = (byte*)ptr;
#else			
				byte[] rgbValues = new byte[bytes];

				// Copy the RGB values into the array.
				Marshal.Copy(ptr, rgbValues, 0, bytes);			
#endif

				bool savedBackground = false;
				_savedTX = -1;
				_savedTY = 0;
				if (IsPlain())
				{
					// ARGB format -> BGRA
					for (int i = 0; i < bytes; i += 4)
					{
						if (rgbValues[i] == b
							&& rgbValues[i + 1] == g
							&& rgbValues[i + 2] == r)
						{
							rgbValues[i + 3] = 0xFF;

							if (!savedBackground)
							{
								int index = i / 4;
								_savedTY = index / bmp.Width;
								_savedTX = index % bmp.Width;
								savedBackground = true;
							}
						}
						else if (rgbValues[i + 3] >= threshold)
							rgbValues[i + 3] = 0xFF;
						else
						{
							// too trasparent
							rgbValues[i] = b;
							rgbValues[i + 1] = g;
							rgbValues[i + 2] = r;
							rgbValues[i + 3] = 0xFF;

							if (!savedBackground)
							{
								int index = i / 4;
								_savedTY = index / bmp.Width;
								_savedTX = index % bmp.Width;
								savedBackground = true;
							}
						}
					}
				}
				else
				{
					// ARGB format -> BGRA
					for (int i = 0; i < bytes; i += 4)
					{
						if (rgbValues[i] == b
							&& rgbValues[i + 1] == g
							&& rgbValues[i + 2] == r)
						{
							rgbValues[i + 3] = 0xFF;

							if (!savedBackground)
							{
								int index = i / 4;
								_savedTY = index / bmp.Width;
								_savedTX = index % bmp.Width;
								savedBackground = true;
							}
						}
						else if (rgbValues[i + 3] >= threshold)
						{
							// remove alpha
							rgbValues[i + 3] = 0xFF;
						}
						else
						{
							// too trasparent
							rgbValues[i] = b;
							rgbValues[i + 1] = g;
							rgbValues[i + 2] = r;
							rgbValues[i + 3] = 0xFF;

							if (!savedBackground)
							{
								int index = i / 4;
								_savedTY = index / bmp.Width;
								_savedTX = index % bmp.Width;
								savedBackground = true;
							}
						}
					}
				}

#if USE_UNSAFE
#else
				// Copy the RGB values back to the bitmap
				Marshal.Copy(rgbValues, 0, ptr, bytes);				
#endif
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                //Logger.LogMessage(MsgLevel.FAULT, ex.Message);
            }
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					bmp.UnlockBits(bmpData);
			}
		} //end DropTraparency

		//
#if USE_UNSAFE
		unsafe
#endif
		protected void FindFirstTransparent(Bitmap bmp, uint background, out int X, out int Y)
		{
			if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
				throw new ArgumentException("Invalid pixel format");

			// Lock the bitmap's bits.
			Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
			X = 0;
			Y = 0;
			try
			{
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// Declare an array to hold the bytes of the bitmap.
				// This code is specific to a bitmap with 32 bits per pixels.
				int bytes = bmp.Width * bmp.Height * 4;
				#if USE_UNSAFE
				byte* rgbValues = (byte*)ptr;
#else
				byte[] rgbValues = new byte[bytes];
				// Copy the RGB values into the array.
				Marshal.Copy(ptr, rgbValues, 0, bytes);
#endif
				byte r = (byte)((background & 0x00FF0000) >> 16);
				byte g = (byte)((background & 0x0000FF00) >> 8);
				byte b = (byte)(background & 0x000000FF);
				int w = bmp.Width;
				// ARGB format -> BGRA
				for (int i = 0; i < bytes; i += 4)
				{
					if (rgbValues[i] == b
						&& rgbValues[i + 1] == g
						&& rgbValues[i + 2] == r)
					{
						int index = i / 4;
						Y = index / w;
						X = index % w;
						break;
					}
				}
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                // Logger.LogMessage(MsgLevel.FAULT, ex.Message);
            }
			finally
			{
				// Unlock the bits.
				bmp.UnlockBits(bmpData);
			}
		} //end DropTraparency

		//
#if USE_UNSAFE
		unsafe
#endif
		protected UInt32 GetBackgroundColorForWrite()
		{
			if (_resized == null)
				return 0;

			UInt32 back = 0;
			BitmapData bmpData = null;
			try
			{
				int height = _resized.Height;
				int width = _resized.Width;
				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, width, height);
				bmpData = _resized.LockBits(rect, ImageLockMode.ReadWrite, _resized.PixelFormat);
				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				if (_resized.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					// Declare an array to hold the bytes of the bitmap.
					// This code is specific to a bitmap with 8 bits per pixels.
					int area = height * bmpData.Stride;
#if USE_UNSAFE
					byte* values = (byte*)ptr;					
#else
					byte[] values = new byte[area];
					// Copy the indexed values into the array.
					Marshal.Copy(ptr, values, 0, area);
#endif
					back = (_savedTX < 0) ? values[0] : values[_savedTX + _savedTY * bmpData.Stride];					

				}
				else if (_resized.PixelFormat == PixelFormat.Format16bppRgb565)
				{
					// Declare an array to hold the bytes of the bitmap.
					// This code is specific to a bitmap with 16 bits per pixels.
					int area = height * bmpData.Stride / 2;
					
#if USE_UNSAFE
					Int16* values = (Int16*)ptr;					
#else
					short[] values = new short[area];
					// Copy the RGB565 values into the array.
					Marshal.Copy(ptr, values, 0, area);
#endif
					back = (_savedTX >= 0)
						? (UInt32)(UInt16)values[_savedTX + _savedTY * bmpData.Stride / 2]
						: ToRGB565(_resizedBackgroundColour);
				}
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                //Logger.LogMessage(MsgLevel.FAULT, ex.Message);
            }
			finally
			{
				// Unlock the bits.
				if (bmpData != null)
					_resized.UnlockBits(bmpData);
			}
			return back;			
		} // end GetBackgroundColorForWrite

		
				
				
	} // end AoWBitmap



	public abstract class AoWImageLibrary
	{
		public abstract bool OpenIlb(string filename, List<AoWBitmap> imageList);
		public abstract bool MakeIlb(string filename, List<AoWBitmap> imageList);		
		
	} // end AoWImageLibrary

}
