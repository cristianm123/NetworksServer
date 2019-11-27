using Emgu.CV;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
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
        double seconds;
        Dictionary<string, double> hash;

        VideoCapture capture;
        List<byte[]> framesCompressed;
        List<byte[]> framesT;
        public Servidor_UDP()
        {
            hash = new Dictionary<string, double>();
            seconds = 0;
        }

        public void Inicio()
        {
            framesCompressed = new List<byte[]>();
            framesT = new List<byte[]>();
            string fileName = "C:/Users/crisf/Downloads/Rappi2 (1).mp4";
            capture = new VideoCapture(fileName);
            Mat m = new Mat();
            TotalFrame = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
            Fps = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
            List<double> l = new List<double>();
            Console.WriteLine("SELECT YOUR COMPRESSION METHOD:\n[1] LOSSY COMPRESSION.\n[2] COMPRESSION WITHOUT LOSS.\n[3] HYBRID.");
            long MSE = 0;
            switch (int.Parse(Console.ReadLine()))
            {
                case 1:
                    while (FrameNo < TotalFrame)
                    {
                        capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, FrameNo);
                        capture.Read(m);
                        
                        
                        //This is the original image taken from the frame
                        var originalImage = ImageToByte(m.Bitmap);
                        //This is the image compressed with data loss
                        var lossyImage = GetCompressedBitmap(m.Bitmap, 60L);
                        ImageConverter ic = new ImageConverter();

                        System.Drawing.Image img = (System.Drawing.Image)ic.ConvertFrom(lossyImage);
                        Bitmap bitmap1 = new Bitmap(img);
                        for (int i = 0; i < m.Bitmap.Width; i++)
                        {
                            for (int j = 0; j < m.Bitmap.Height; j++)
                            {
                                MSE += (long)Math.Pow(m.Bitmap.GetPixel(i, j).ToArgb() - bitmap1.GetPixel(i, j).ToArgb(), 2);
                            }
                        }
                        
                        //The percent of compressed data for the frame
                        l.Add(1.0 - ((double)lossyImage.Count()) / ((double)originalImage.Count()));
                        //Console.Write(Math.Round(l.Last(), 2) * 100 + "%. | ");
                        framesCompressed.Add(lossyImage);
                        framesT.Add(originalImage);
                        Console.WriteLine(framesCompressed.Count);
                        FrameNo += 2;
                    }
                    break;
                case 2:
                    while (FrameNo < TotalFrame)
                    {
                        capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, FrameNo);
                        capture.Read(m);
                        //This is the original image taken from the frame
                        var originalImage = ImageToByte(m.Bitmap);
                        //This is the image compressed without data loss
                        var compressedImage = Compress(originalImage);
                        //The percent of compressed data for the frame
                        l.Add(1.0 - ((double)compressedImage.Count()) / ((double)originalImage.Count()));
                        Console.Write(Math.Round(l.Last(), 2) * 100 + "%. | ");
                        framesCompressed.Add(compressedImage);
                        framesT.Add(originalImage);
                        FrameNo += 2;
                    }
                    break;
                case 3:
                    while (FrameNo < TotalFrame)
                    {
                        capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, FrameNo);
                        capture.Read(m);
                        //This is the original image taken from the frame
                        var originalImage = ImageToByte(m.Bitmap);
                        //This is the image compressed with data loss
                        var lossyImage = GetCompressedBitmap(m.Bitmap, 60L);
                        //This is the image compressed without data loss
                        var compressedImage = Compress(lossyImage);
                        //The percent of compressed data for the frame
                        l.Add(1.0 - ((double)compressedImage.Count()) / ((double)originalImage.Count()));
                        Console.Write(Math.Round(l.Last(), 2) * 100 + "%. | ");
                        framesCompressed.Add(compressedImage);
                        framesT.Add(originalImage);
                        FrameNo += 2;
                    }
                    break;
            }
            Console.WriteLine();
            MSE /= l.Count * 640 * 360;
            long L = 4294967296;
            Console.WriteLine("MSE: {0}\nRMSE: {1}\nPSNR: {2}", MSE, Math.Sqrt(MSE), 10 * Math.Log10(L * L / MSE));
            Console.WriteLine("Compress average: {0}", Math.Round(l.Average(), 5) * 100 + "%");
            ipep = new IPEndPoint(IPAddress.Any, 11000);
            newsock = new UdpClient(ipep);
            while (true)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                Console.WriteLine("Waiting for a client...");
                byte[] data = newsock.Receive(ref sender);
                if(Encoding.ASCII.GetString(data).Equals("."))
                {
                    list.Add(sender);
                    Console.WriteLine("Client received");
                    Thread t = new Thread(Send);
                    t.Start(list.Count - 1);
                }
                else
                {
                    var t = Encoding.ASCII.GetString(data).Split(',');
                    if(hash.ContainsKey(t[0]))
                    {
                        double d = hash[t[0]];
                        hash.Remove(t[0]);
                        hash.Add(t[0], double.Parse(t[1]) + d);
                    }
                    else
                    {
                        hash.Add(t[0], double.Parse(t[1]));
                    }
                    
                    seconds += double.Parse(t[1]);
                    Console.WriteLine("Cliente: {0}", t[0]);
                    Console.WriteLine("Segundos vistos por los clientes: {0}", seconds);
                    generateReport();
                }
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
            while (j++ < framesCompressed.Count)
            {
                Console.WriteLine("Packet send. {0}/{1}", j, framesCompressed.Count);
                newsock.Send(framesCompressed[j - 1], framesCompressed[j - 1].Length, sender);
                Thread.Sleep(60);
            }
        }

        public void generateReport()
        {
            Document doc = new Document(PageSize.LETTER, 0, 0, 0, 0);
            PdfWriter wri = PdfWriter.GetInstance(doc, new FileStream("Report.pdf", FileMode.Create));
            doc.Open();
            PdfPTable table = new PdfPTable(2);
            var boldFont = FontFactory.GetFont(FontFactory.TIMES_BOLD, 12);
            table.AddCell(new Paragraph("Name", boldFont));
            table.AddCell(new Paragraph("Time watched", boldFont));
            foreach (var item in hash.Keys)
            {
                table.AddCell(new Paragraph(item));
                table.AddCell(new Paragraph(hash[item]+" s"));
            }
            table.AddCell(new Paragraph("TOTAL TIME WATCHED:"));
            table.AddCell(new Paragraph(seconds+" s"));
            doc.Add(table);
            doc.Close();
        }

        public static byte[] ImageToByte(System.Drawing.Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static byte[] Compress(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream();
            GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
            zip.Write(buffer, 0, buffer.Length);
            zip.Close();
            ms.Position = 0;

            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

    }
}
