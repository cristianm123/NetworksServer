using Emgu.CV;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
        double seconds;
        Dictionary<string, double> hash;

        VideoCapture capture;
        List<byte[]> frames;
        public Servidor_UDP()
        {
            hash = new Dictionary<string, double>();
            seconds = 0;
        }

        public void Inicio()
        {
            frames = new List<byte[]>();
            string fileName = "C:/Users/crisf/Downloads/WhatsApp Video 2019-10-23 at 8.22.47 AM.mp4";
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
                FrameNo += 2;
            }
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
            while (j++ < frames.Count)
            {
                Console.WriteLine("Packet send. {0}/{1}", j, frames.Count);
                newsock.Send(frames[j - 1], frames[j - 1].Length, sender);
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
    }
}
