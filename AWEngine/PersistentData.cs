using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Serialization;
using System.Diagnostics;

using AoWGraphics;

namespace IlbEditorNet
{
	public static class PersistentData
	{
		static private string persistentDataPath = "persistent.xml";
		static private PersistentDataInstance dataField = null;

		public static PersistentDataInstance Data
		{
			get 
			{
				if (dataField == null)
					Load();
				return dataField; 
			}			
		}

		static public bool Load()
		{
			try
			{
				using (StreamReader sr = new StreamReader(persistentDataPath))
				{
					XmlSerializer xs = new XmlSerializer(typeof(PersistentDataInstance));
					dataField = xs.Deserialize(sr) as PersistentDataInstance;
                    if (dataField != null)
                    {
                        if (dataField.AoW1Default == null)
                            dataField.AoW1Default = new AoW1Bitmap();
                        return true;
                    }
				}
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
				// Logger.LogMessage(MsgLevel.DEBUG, ex.Message);
				dataField = new PersistentDataInstance();
                dataField.AoW1Default = new AoW1Bitmap();
			}
			return true;
		}

		static public bool Save()
		{
			if (dataField == null)
				return false;

			try
			{
				using (StreamWriter sr = new StreamWriter(persistentDataPath))
				{
					XmlSerializer xs = new XmlSerializer(typeof(PersistentDataInstance));
					xs.Serialize(sr, dataField);
					return true;
				}
			}
			catch (Exception ex)
			{
                Debug.WriteLine(ex.Message);
                // Logger.LogMessage(MsgLevel.FAULT, ex.Message);
			}
			return false;
		}		
	}

	[System.SerializableAttribute()]
	public class PersistentDataInstance
	{
		public PersistentDataInstance()
		{
			imageFormatField = ImageFormat.Bmp;
			//msgLevelField = MsgLevel.FAULT | MsgLevel.INFO | MsgLevel.SEVERE | MsgLevel.WARNING;
			backgroundColorField = Color.Magenta;
			colorMatrixField = new ColorMatrix();
			gammaCorrectionField = 1.0f;
			//filterListField = new List<AForge.Imaging.Filters.IFilter>();
		}

		private string lastFolderField;
		private ImageFormat imageFormatField;
		//private MsgLevel msgLevelField;
		private Color backgroundColorField;
		private bool playOriginalField;
		
		private bool applyFiltering = false;
		private ColorMatrix colorMatrixField;
		private float gammaCorrectionField;
		//private List<AForge.Imaging.Filters.IFilter> filterListField;

		public string LastFolder
		{
			get { return lastFolderField; }
			set { lastFolderField = value; }
		}

		/*public MsgLevel MsgLevel
		{
			get { return msgLevelField; }
			set { msgLevelField = value; }
		}		
        */
		[XmlIgnore()]
		public ImageFormat ImageFormat
		{
			get { return imageFormatField; }
			set { imageFormatField = value; }
		}

		public String ImageFormatAsString
		{
			get { return imageFormatField.ToString(); }
			set
			{
				switch (value)
				{
					case "Bmp": imageFormatField = ImageFormat.Bmp; break;
					case "Png": imageFormatField = ImageFormat.Png; break;
				}
			}
		}

		public bool PlayOriginal
		{
			get { return playOriginalField; }
			set { playOriginalField = value; }
		}

		public bool ApplyFiltering
		{
			get { return applyFiltering; }
			set { applyFiltering = value; }
		}

		public float GammaCorrection
		{
			get { return gammaCorrectionField; }
			set { gammaCorrectionField = value; }
		}
		[XmlIgnore()]
		public ColorMatrix ColorMatrix
		{
			get { return colorMatrixField; }
			set { colorMatrixField = value; }
		}

		public float[] ColorMatrixAsFloat
		{
			get 
			{
				float[] matrix = new float[25];
				for (int k = 0, j = 0; j < 5; ++j)
					for (int i = 0; i < 5; ++i, ++k)
						matrix[k] = colorMatrixField[j, i];

				return matrix; 
			}
			set 
			{
				if (value != null)
				{
					int k = 0;
					foreach (float f in value)
					{
						colorMatrixField[k / 5, k % 5] = f;
						++k;
					}
				}
			}
		}

		/*[XmlIgnore()]
		public List<AForge.Imaging.Filters.IFilter> FilterList
		{
			get { return filterListField; }			
		}*/

		// defalut settings for create new images
        private AoW1Bitmap _defaultImage;
        public AoW1Bitmap AoW1Default
        {
            get { return _defaultImage; }
            set { _defaultImage = value; }
        }
		
       
	}

	
}
