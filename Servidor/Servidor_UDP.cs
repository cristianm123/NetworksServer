using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Servidor
{
    class Servidor_UDP
    {
        public List<IPEndPoint> list = new List<IPEndPoint>();
        public IPEndPoint ipep;
        public UdpClient newsock;
        double TotalFrame;
        double Fps;
        int FrameNo = 0;

        VideoCapture capture;
        List<byte[]> frames;
        public Servidor_UDP()
        {
            
            
        }

        public void Inicio()
        {
            frames = new List<byte[]>();
            string fileName = "C:/Users/crisf/Downloads/20180310_135541.mp4";
            capture = new VideoCapture(fileName);
            Mat m = new Mat();
            TotalFrame = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
            Fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
            while (FrameNo < TotalFrame)
            {
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, FrameNo);
                capture.Read(m);
                var b = GetCompressedBitmap(m.Bitmap, 60L);

                //ImageConverter converter = new ImageConverter();
                frames.Add(b);
                FrameNo += 5;
            }
            ipep = new IPEndPoint(IPAddress.Any, 11000);
            newsock = new UdpClient(ipep);
            while (true)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                Console.WriteLine("Waiting for a client...");
                byte[] data = newsock.Receive(ref sender);
                list.Add(sender);
                Console.WriteLine("Client received");
                Thread t = new Thread(Send);
                t.Start(list.Count - 1);
            }
        }

        private byte[] GetCompressedBitmap(Bitmap bmp, long quality)
        {
            using (var mss = new MemoryStream())
            {
                EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == ImageFormat.Jpeg.Guid);
                EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = qualityParam;
                bmp.Save(mss, imageCodec, parameters);
                return mss.ToArray();
            }
        }

        void Send(object o)
        {
            int i = Convert.ToInt32(o);
            IPEndPoint sender = list[i];
            Console.WriteLine("Message received from {0}: ", sender.ToString());
            int j = 0;
            while (j++ < frames.Count)
            {
                Console.WriteLine("Packet send. {0}/{1}", j, frames.Count);
                newsock.Send(frames[j - 1], frames[j - 1].Length, sender);
                Thread.Sleep(250);
            }
        }
    }
}
