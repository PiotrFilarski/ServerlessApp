using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Drawing2D;
using ZXing;
using System.Drawing.Imaging;
using System.Data.SqlClient;

namespace FunctionApp
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (String.IsNullOrEmpty(requestBody))
                return new BadRequestObjectResult("Something went wrong with web app request");

            FinancialStatement fsArg = JsonConvert.DeserializeObject<FinancialStatement>(requestBody);
            FinancialStatement fs = await ProcessFinancialStatementAsync(fsArg.Firstname, fsArg.Lastname, fsArg.Income, fsArg.Age, fsArg.Photo);
            if (fs == null)
                return new BadRequestObjectResult("Something went wrong with image or QR code generator");
            log.LogInformation($"Returning: {fs.Firstname} {fs.Lastname}");
            return new OkObjectResult($"Processed and returning: {fs.Firstname} {fs.Lastname}");
        }

        private static async Task<FinancialStatement> ProcessFinancialStatementAsync(string firstname, string lastname, int income, int age, byte[] photo)
        {
            FinancialStatement fs = new FinancialStatement();
            fs.Firstname = firstname;
            fs.Lastname = lastname;
            fs.Income = income;
            fs.Age = age;

            try
            {
                Image photoImage;
                using (var ms = new MemoryStream(photo))
                {
                    photoImage = Image.FromStream(ms);
                }

                Bitmap image = ResizeImage(photoImage, 100, 100);
                image = GrayScaleFilter(image);
                ImageConverter converter = new ImageConverter();
                fs.Photo = (byte[])converter.ConvertTo(image, typeof(byte[]));
            }
            catch
            {
                return null;
            }

            try
            {
                Bitmap qrcode = GenerateMyQCCode(fs.Firstname + "; " + fs.Lastname + "; " + fs.Income);
                ImageConverter converter = new ImageConverter();
                fs.Code = (byte[])converter.ConvertTo(qrcode, typeof(byte[]));
            }
            catch
            {
                return null;
            }

            for (int i = 0; i < 10; i++)
            {
                long fsFactor = FindPrimeNumber(100000);
            }

            //save results DB
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                var sql = "INSERT INTO FinancialStatements (Firstname, Lastname, Income, Age, Photo, Code) VALUES (@Firstname, @Lastname, @Income, @Age, @Photo, @Code)";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("Firstname", fs.Firstname);
                cmd.Parameters.AddWithValue("Lastname", fs.Lastname);
                cmd.Parameters.AddWithValue("Income", fs.Income);
                cmd.Parameters.AddWithValue("Age", fs.Age);
                cmd.Parameters.AddWithValue("Photo", fs.Photo);
                cmd.Parameters.AddWithValue("Code", fs.Code);

                var rows = cmd.ExecuteNonQuery();
            }
            return fs;
        }
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        public static Bitmap GrayScaleFilter(Bitmap image)
        {
            Bitmap grayScale = new Bitmap(image.Width, image.Height);

            for (Int32 y = 0; y < grayScale.Height; y++)
                for (Int32 x = 0; x < grayScale.Width; x++)
                {
                    Color c = image.GetPixel(x, y);

                    Int32 gs = (Int32)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);

                    grayScale.SetPixel(x, y, Color.FromArgb(gs, gs, gs));
                }
            return grayScale;
        }
        private static Bitmap GenerateMyQCCode(string QCText)
        {
            var QCwriter = new BarcodeWriter();
            QCwriter.Format = BarcodeFormat.QR_CODE;
            var result = QCwriter.Write(QCText);
            var barcodeBitmap = new Bitmap(result);
            return barcodeBitmap;
        }
        public static long FindPrimeNumber(int n)
        {
            int count = 0;
            long a = 2;
            while (count < n)
            {
                long b = 2;
                int prime = 1;// to check if found a prime
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }
                    b++;
                }
                if (prime > 0)
                {
                    count++;
                }
                a++;
            }
            return (--a);
        }
    }
}
