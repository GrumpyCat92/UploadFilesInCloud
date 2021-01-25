using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Configuration;
using System.Threading;

namespace UploadFilesInCloud
{
    class Program
    {
        static string token = string.Empty;
        static void Main(string[] args)
        {
            try
            {
                token = ConfigurationManager.AppSettings["tokenYandex"];
                Console.WriteLine("Enter a directory path: ");
                var path = Console.ReadLine();
                Console.WriteLine("Enter a Yandex directory's name: ");
                var cloudDir = Console.ReadLine();
                if (string.IsNullOrEmpty(path))
                    throw new Exception("Directory path must be not empty");
                var files = Directory.GetFiles(path);
                if (files.Length == 0)
                    throw new Exception("Directory is empty");

                for (int j = 0; j <= files.Length-1; j++)
                {
                    StartUploaded(files[j], cloudDir);
                }

                Console.Read();

            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Console.ReadKey();
            }
        
        }

        static async void StartUploaded(string filePath, string cloudDirectory)
        {
                Console.WriteLine($"{filePath} is started to upload...");
                await Task.Run(() => UploadFile(filePath,cloudDirectory));                
        }

        private static async Task UploadFile(string filePath, string cloudDirectory)
        {
            var newFilePath = $"{cloudDirectory}/{Path.GetFileName(filePath)}";
            Console.WriteLine($"{filePath} is started to upload");
            try
            {
                using (var wc = new WebClient())
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create($"https://cloud-api.yandex.net/v1/disk/resources/upload?path={newFilePath}");
                    request.Method = "GET";
                    request.Headers.Add("Authorization", $"OAuth {token}");
                    using (var response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Stream dataStream = response.GetResponseStream();
                            StreamReader reader = new StreamReader(dataStream);
                            var responseBody = JsonConvert.DeserializeObject<UploadModel>(reader.ReadToEnd());
                            reader.Close();
                            dataStream.Close();
                            var href = responseBody.href;

                            using (var stream = new FileStream(filePath, FileMode.Open))
                            {
                                wc.UploadDataAsync(new Uri(href), "PUT", ToByteArray(stream));
                            }
                            Console.WriteLine($"{newFilePath} is uploaded");
                        }
                        else
                        {
                            Console.WriteLine($"{newFilePath} is not uploaded");
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{newFilePath} is not uploaded");
            }
        }

        public static Byte[] ToByteArray(Stream stream)
        {
            Int32 length = stream.Length > Int32.MaxValue ? Int32.MaxValue : Convert.ToInt32(stream.Length);
            Byte[] buffer = new Byte[length];
            stream.Read(buffer, 0, length);
            return buffer;
        }
    }
}
