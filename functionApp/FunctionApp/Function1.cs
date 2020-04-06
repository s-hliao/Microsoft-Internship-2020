using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FunctionApp
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            StreamReader streamReader = new StreamReader(req.Body);
            string jsonString = streamReader.ReadToEnd();
            JObject jsonObj = JObject.Parse(jsonString);

            JToken blob =  jsonObj["imageInfo"]["imageRequestInfo"]["blob"];

            
            string[,]pieces = new string[8,8];

            if (blob != null)
            {
                string blobString = blob.Value<string>();
                byte[] blobData = Convert.FromBase64String(blobString);

                Image img = Image.Load<Rgba32>(blobData);
                img.Mutate(x => x.Grayscale());
                Image[,] images = new Image[8, 8];
                for(int i = 0; i<8; i++)
                {
                    for(int j = 0; j<8; j++)
                    {
                        Image clone = img.Clone(x => x.
                            Crop(new Rectangle(i * img.Width / 8, j * img.Height / 8, img.Width / 8, img.Height / 8)));
                        images[i,j] = clone;

                    }
                }
                List<List<byte[]>> imageBytes = new List<List<byte[]>>();

                for (int i = 0; i < 8; i++)
                {
                    imageBytes.Add(new List<byte[]>());
                    for (int j = 0; j < 8; j++)
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            IImageEncoder imgEnc = images[i,j].GetConfiguration().ImageFormatsManager.FindEncoder(JpegFormat.Instance);
                            images[i, j].Save(memoryStream, imgEnc);
                            imageBytes[j].Add(memoryStream.ToArray());
                        }

                    }
                }
                HttpResponseMessage response;
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Prediction-Key", "8d1318bea5b24b39a4827e78146f8edd");
                string url = "https://westus2.api.cognitive.microsoft.com/customvision/v3.0/Prediction/5e1fa8f4-65d6-447d-9079-a9dbd0f366bf/classify/iterations/Iteration2/image";
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        using (var content = new ByteArrayContent(imageBytes[i][j]))
                        {
                            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            response = await client.PostAsync(url, content);
                            response.EnsureSuccessStatusCode();
                            string responseBody = await response.Content.ReadAsStringAsync();

                            JObject tagged = JObject.Parse(responseBody);
                            string finalTag = null;
                            double max = 0;
                            foreach(JToken tag in tagged["Predictions"])
                            {
                                double prob = tag["Probability"].Value<double>();
                                if (prob>.5 && prob>max)
                                {
                                    finalTag = tag["TagName"].Value<string>();
                                    max = prob;
                                }
                            }

                            if (finalTag != null)
                            {
                                pieces[i,j] = finalTag;
                            }
                            else
                            {
                                pieces[i, j] = null;
                            }
                        }

                        
                       
                    }
                }

            }




            JObject resp = new JObject();
            resp.Add(new JProperty("board", pieces));
            return new JsonResult(resp);
        }
    }
}
