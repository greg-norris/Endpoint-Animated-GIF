using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using SkiaSharp;
using System.Runtime.InteropServices;
using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Display;
using EndpointDisplayTest.Properties;

var port = EPM815.Gpio.Pin.PD14 /16;
var pin = EPM815.Gpio.Pin.PD14 % 16;

var gpioController = new GpioController(PinNumberingScheme.Logical, new LibGpiodDriver(port));
gpioController.OpenPin(pin, PinMode.Output);
gpioController.Write(pin, PinValue.High); 

var screenWidth = 480;
var screenHeight = 272;
var imageWidth = 275;
var imageHeight = 183;

SKBitmap bitmap = new SKBitmap(screenWidth, screenHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
bitmap.Erase(SKColors.Transparent);

var configuration = new FBDisplay.ParallelConfiguration()
{
    Clock = 10000,
    Width = 480,
    Hsync_start = 480 + 2,
    Hsync_end = 480 + 2 + 41,
    Htotal = 480 + 2 + 41 + 2,
    Height = 272,
    Vsync_start = 272 + 2,
    Vsync_end = 272 + 2 + 10,
    Vtotal = 272 + 2 + 10 + 2,

};
var fbDisplay = new FBDisplay(configuration);
var displayController = new DisplayController(fbDisplay);
     
var img = Resources.endpointFireworks;
SKBitmap[] sk_imgs = null;
var framecnt = 0;
var x1 = -300;
var x2 = 480;
var x3 = -300;

while (true){
    using (var stream = new MemoryStream(img)){

        using (var canvas = new SKCanvas(bitmap)){
            
            using (SKManagedStream skStream = new SKManagedStream(stream)){

                using (SKCodec codec = SKCodec.Create(skStream)) {
                    //Get frame count and allocate bitmaps
                    int frameCount = codec.FrameCount;
                    var bitmaps = new SKBitmap[frameCount];
                    var durations = new int[frameCount];
                    var accumulatedDurations = new int[frameCount];

                    if (sk_imgs == null){
                        sk_imgs = new SKBitmap[frameCount];
                    }

                    //Loop through the frames
                    for (int frame = 0; frame < frameCount; frame++){
                       
                        durations[frame] = codec.FrameInfo[frame].Duration;

                        // Create a full color bitmap for each frame
                        if (sk_imgs[frame] == null){

                            var imageInfo = new SKImageInfo(codec.Info.Width,codec.Info.Height,       SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
                            bitmaps[frame] = new SKBitmap(imageInfo);

                            //Get the address of the pixels in that bitmap
                            IntPtr pointer = bitmaps[frame].GetPixels();

                            //Create an SKCodecOptions value to specify the frame
                            SKCodecOptions codecOptions = new SKCodecOptions(frame);

                            //Copy pixels from the frame into the bitmap
                            codec.GetPixels(imageInfo, pointer, codecOptions);

                            var pixelArray = bitmaps[frame].Bytes;

                            //pin the managed array so that the GC doesn't move it
                            var gcHandle = GCHandle.Alloc(pixelArray, GCHandleType.Pinned);

                            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height); 
                            var sk_img = SKBitmap.Decode(img, info);

                            //install the pixels with the color type of the pixel data
                           #pragma warning disable CS0618 // Type or member is obsolet
                            _ = sk_img.InstallPixels(
                                info: imageInfo,
                                pixels: gcHandle.AddrOfPinnedObject(),
                                rowBytes: imageInfo.RowBytes,
                                ctable: null,
                                releaseProc: delegate { gcHandle.Free(); },
                                context: null);
                           

                            sk_imgs[frame] = sk_img;
                        }

                        framecnt++;

                        if (framecnt > frameCount){


                            //Create Black Screen 
                            canvas.DrawColor(SKColors.White);
                            canvas.Clear(SKColors.White);
                            canvas.DrawBitmap(sk_imgs[frame], (screenWidth - codec.Info.Width) / 2, (screenHeight - codec.Info.Height) / 2); // where it is on the screen


                            using (SKPaint text = new SKPaint())
                            {
                                text.Color = SKColors.Yellow;
                                text.IsAntialias = true;
                                text.StrokeWidth = 2;
                                text.Style = SKPaintStyle.Stroke;

                                //SKFont Text - 
                                SKFont font = new SKFont();
                                font.Size = 45;
                                font.ScaleX = 2;
                                SKTextBlob textBlob = SKTextBlob.Create("Happy", font);
                                canvas.DrawText(textBlob, x1, 60, text);

                                if (x1 < 100)
                                    x1 += 20;
                            }

                            using (SKPaint text = new SKPaint())
                            {
                                text.Color = SKColors.White;
                                text.IsAntialias = true;
                                text.StrokeWidth = 4;
                                text.Style = SKPaintStyle.StrokeAndFill;

                                //SKFont Text - 
                                SKFont font = new SKFont();
                                font.Size = 45;
                                font.ScaleX = 2;
                                SKTextBlob textBlob = SKTextBlob.Create(".NET 8", font);
                                canvas.DrawText(textBlob, x2, 160, text);

                                if (x2 > 85)
                                    x2 -= 20;
                            }

                            using (SKPaint text = new SKPaint())
                            {
                                text.Color = SKColors.Yellow;
                                text.IsAntialias = true;
                                text.StrokeWidth = 2;
                                text.Style = SKPaintStyle.Stroke;

                                //SKFont Text - 
                                SKFont font = new SKFont();
                                font.Size = 45;
                                font.ScaleX = 2;
                                SKTextBlob textBlob = SKTextBlob.Create("2024", font);
                                canvas.DrawText(textBlob, x3, 250 , text);

                                if (x3 < 130)
                                    x3 += 22;
                            }

                            var data = bitmap.Copy(SKColorType.Rgb565).Bytes;

                            displayController.Flush(data);

                            Thread.Sleep(durations[frame]);
                        }
                        else{
                            //Create Black Screen 
                            canvas.DrawColor(SKColors.Black);
                            canvas.Clear(SKColors.Black);
                          
                            var logo = Resources.logo;
                            var info = new SKImageInfo(imageWidth, imageHeight);
                            var sk_img = SKBitmap.Decode(logo, info);
                            canvas.DrawBitmap(sk_img, (screenWidth - imageWidth) / 2, (screenHeight - imageHeight)/2);

                            var data = bitmap.Copy(SKColorType.Rgb565).Bytes;
                            displayController.Flush(data);
                            Thread.Sleep(durations[frame]);

                        }
                    }
                }
            }
        }
    }
}























#pragma warning restore CS0618 // Type or member is obsolete