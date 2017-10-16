using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace gifmakercode
{   
    public class GifMaker : IDisposable
    {
        #region Header Constants
        private const string FileType = "GIF";
        private const string FileVersion = "89a";
        private const byte FileTrailer = 0x3b;

        private const int Extension_Introducer = 0xff21;
        private const byte Size_of_Extension_Block = 0x0b;
        private const string ApplicationIdentification = "NETSCAPE2.0";

        private const int Graphic_Control_Label = 0xf921;
        private const byte Size_of_remaining_fields = 0x04;

        private const long SourceGlobalColorInfoPosition = 10;
        private const long SourceGraphicControlExtensionPosition = 781;
        private const long SourceGraphicControlExtensionLength = 8;
        private const long SourceImageBlockPosition = 789;
        private const long SourceImageBlockHeaderLength = 11;
        private const long SourceColorBlockPosition = 13;
        private const long SourceColorBlockLength = 768;

        private const byte Block_Terminator = 0;
        #endregion

        private bool _GifHeadconfirm = true;                                      //first image
        private int? _width;
        private int? _height;
        private int? _repeatCount;
        private readonly Stream _stream;
                
        public TimeSpan FrameIntervalTime { get; set; }
               
        public GifMaker(Stream stream, int? width = null, int? height = null, int? repeatCount = null)
        {
            _stream = stream;
            _width = width;
            _height = height;
            _repeatCount = repeatCount;
        }

        public void AddFrame(Image img, int x = 0, int y = 0, TimeSpan? frameDelay = null)
        {
            using (var gifStream = new MemoryStream())
            {
                img.Save(gifStream, ImageFormat.Gif);
                if (_GifHeadconfirm)                                            // get the global color table info
                {
                    InitHeader(gifStream, img.Width, img.Height);
                }
                WriteGraphicControlBlock(gifStream, frameDelay.GetValueOrDefault(FrameIntervalTime));
                WriteImgBlock(gifStream, !_GifHeadconfirm, x, y, img.Width, img.Height);
            }
            _GifHeadconfirm = false;
        }

        private void InitHeader(Stream sourceGif, int w, int h)
        {
            // File Header
            Write_String(FileType);
            Write_String(FileVersion);
            Write_Short(_width.GetValueOrDefault(w));                           // Initial Logical Width
            Write_Short(_height.GetValueOrDefault(h));                          // Initial Logical Height
            sourceGif.Position = SourceGlobalColorInfoPosition;
            WriteByt(sourceGif.ReadByte());                                     // Global Color Table Info
            WriteByt(0);                                                        // Background Color Index
            WriteByt(0);                                                        // Pixel aspect ratio
            WriteColorTable(sourceGif);

            // App Extension Header
            Write_Short(Extension_Introducer);
            WriteByt(Size_of_Extension_Block);
            Write_String(ApplicationIdentification);
            WriteByt(3);                                                        // Application block length
            WriteByt(1);
            Write_Short(_repeatCount.GetValueOrDefault(0));                     // Repeat count for images.
            WriteByt(0);                                                        // terminator
        }

        private void WriteColorTable(Stream sourceGif)
        {
            sourceGif.Position = SourceColorBlockPosition;                      // Locating the image color table
            var colorTable = new byte[SourceColorBlockLength];
            sourceGif.Read(colorTable, 0, colorTable.Length);
            _stream.Write(colorTable, 0, colorTable.Length);
        }

        private void WriteGraphicControlBlock(Stream sourceGif, TimeSpan frameDelay)
        {
            sourceGif.Position = SourceGraphicControlExtensionPosition;         // Locating the source GCE
            var blockhead = new byte[SourceGraphicControlExtensionLength];
            sourceGif.Read(blockhead, 0, blockhead.Length);                     // Reading source GCE

            Write_Short(Graphic_Control_Label);                                 // Identifier
            WriteByt(Size_of_remaining_fields);                                 // Block Size
            WriteByt(blockhead[3] & 0xf7 | 0x08);                               // Setting disposal flag
            Write_Short(Convert.ToInt32(frameDelay.TotalMilliseconds / 10));    // Setting frame delay
            WriteByt(blockhead[6]);                                             // Transparent color index
            WriteByt(Block_Terminator);                                         // Terminator
        }

        private void WriteImgBlock(Stream sourceGif, bool includeColorTable, int x, int y, int h, int w)
        {
            sourceGif.Position = SourceImageBlockPosition;                      // Locating the image block
            var header = new byte[SourceImageBlockHeaderLength];
            sourceGif.Read(header, 0, header.Length);
            WriteByt(header[0]);                                                // Separator
            Write_Short(x);                                                     // Position X
            Write_Short(y);                                                     // Position Y
            Write_Short(h);                                                     // Height
            Write_Short(w);                                                     // Width

            if (includeColorTable)                                              // If first frame, use global color table - else use local
            {
                sourceGif.Position = SourceGlobalColorInfoPosition;
                WriteByt(sourceGif.ReadByte() & 0x3f | 0x80);                   // Enabling local color table
                WriteColorTable(sourceGif);
            }
            else
            {
                WriteByt(header[9] & 0x07 | 0x07);                              // Disabling local color table
            }

            WriteByt(header[10]);                                               // LZW Min Code Size

            // Read/Write image data
            sourceGif.Position = SourceImageBlockPosition + SourceImageBlockHeaderLength;

            var dataLength = sourceGif.ReadByte();
            while (dataLength > 0)
            {
                var imgData = new byte[dataLength];
                sourceGif.Read(imgData, 0, dataLength);

                _stream.WriteByte(Convert.ToByte(dataLength));
                _stream.Write(imgData, 0, dataLength);
                dataLength = sourceGif.ReadByte();
            }

            _stream.WriteByte(Block_Terminator);                                // Terminator

        }

        private void WriteByt(int value)
        {
            _stream.WriteByte(Convert.ToByte(value));
        }

        private void Write_Short(int value)
        {
            _stream.WriteByte(Convert.ToByte(value & 0xff));
            _stream.WriteByte(Convert.ToByte((value >> 8) & 0xff));
        }

        private void Write_String(string value)
        {
            _stream.Write(value.ToArray().Select(c => (byte)c).ToArray(), 0, value.Length);
        }

        public void Dispose()
        {
            // Complete File
            WriteByt(FileTrailer);

            // Pushing data
            _stream.Flush();
        }
    }
}