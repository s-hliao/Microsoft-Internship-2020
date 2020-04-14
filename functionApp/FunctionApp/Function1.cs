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
using AdaptiveCards;

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

            //log.LogInformation("jsonParsed");

            JToken blob = jsonObj["imageInfo"] ["imageRequestInfo"][ "blob"];

            
            string[,]pieces = new string[8,8];

            if (blob != null)
            {
               // log.LogInformation("blob seen");
                string blobString = blob.Value<string>();
                byte[] blobData = Convert.FromBase64String(blobString);

                //log.LogInformation("blob read");

                Image img = Image.Load<Rgba32>(blobData);
                img.Mutate(x => x.Grayscale());
                Image[,] images = new Image[8, 8];
                for(int i = 0; i<8; i++)
                {
                    for(int j = 0; j<8; j++)
                    {
                        Image clone = img.Clone(x => x.
                            Crop(new Rectangle(j * img.Width / 8, i * img.Height / 8, img.Width / 8, img.Height / 8)));
                        images[i,j] = clone;

                    }
                }
                //log.LogInformation("image cropped");

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
                            imageBytes[i].Add(memoryStream.ToArray());
                        }

                    }
                }
                //log.LogInformation("images streamed to byte");

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

                            log.LogInformation(responseBody);

                            JObject tagged = JObject.Parse(responseBody);

                            //log.LogInformation("tag confirmed");

                            string finalTag = null;
                            double max = 0;
                            foreach(JObject tag in tagged["predictions"])
                            {
                                //log.LogInformation("Predictions found");
                                double prob = tag["probability"].Value<double>();
                                //log.LogInformation("Probability found");
                                if (prob>.5 && prob>max)
                                {
                                    finalTag = tag["tagName"].Value<string>();
                                    max = prob;
                                }
                            }

                            //log.LogInformation("predicted");

                            if (finalTag != null)
                            {
                                pieces[i,j] = finalTag;
                            }
                            else
                            {
                                pieces[i, j] = "unknown";
                            }
                        }

                        
                       
                    }
                }
                AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body = new List<AdaptiveElement>()

                {
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a8",
                                             Value = pieces[0,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b8",
                                             Value = pieces[0,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c8",
                                             Value = pieces[0,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d8",
                                             Value = pieces[0,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e8",
                                             Value = pieces[0,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f8",
                                             Value = pieces[0,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g8",
                                             Value = pieces[0,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h8",
                                             Value = pieces[0,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a7",
                                             Value = pieces[1,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b7",
                                             Value = pieces[1,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c7",
                                             Value = pieces[1,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d7",
                                             Value = pieces[1,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e7",
                                             Value = pieces[1,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f7",
                                             Value = pieces[1,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g7",
                                             Value = pieces[1,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h7",
                                             Value = pieces[1,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a6",
                                             Value = pieces[2,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b6",
                                             Value = pieces[2,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c6",
                                             Value = pieces[2,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d6",
                                             Value = pieces[2,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e6",
                                             Value = pieces[2,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f6",
                                             Value = pieces[2,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g6",
                                             Value = pieces[2,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h6",
                                             Value = pieces[2,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a5",
                                             Value = pieces[3,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b5",
                                             Value = pieces[3,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c5",
                                             Value = pieces[3,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d5",
                                             Value = pieces[3,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e5",
                                             Value = pieces[3,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f5",
                                             Value = pieces[3,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g5",
                                             Value = pieces[3,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h5",
                                             Value = pieces[3,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a4",
                                             Value = pieces[4,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b4",
                                             Value = pieces[4,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c4",
                                             Value = pieces[4,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d4",
                                             Value = pieces[4,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e4",
                                             Value = pieces[4,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f4",
                                             Value = pieces[4,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g4",
                                             Value = pieces[4,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h4",
                                             Value = pieces[4,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a3",
                                             Value = pieces[5,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b3",
                                             Value = pieces[5,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c3",
                                             Value = pieces[5,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d3",
                                             Value = pieces[5,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e3",
                                             Value = pieces[5,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f3",
                                             Value = pieces[5,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g3",
                                             Value = pieces[5,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h3",
                                             Value = pieces[5,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a2",
                                             Value = pieces[6,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b2",
                                             Value = pieces[6,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c2",
                                             Value = pieces[6,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d2",
                                             Value = pieces[6,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e2",
                                             Value = pieces[6,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f2",
                                             Value = pieces[6,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g2",
                                             Value = pieces[6,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h2",
                                             Value = pieces[6,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items =
                        {
                            new AdaptiveColumnSet()
                            {
                                Columns =  {
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "a1",
                                             Value = pieces[7,0],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "b1",
                                             Value = pieces[7,1],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "c1",
                                             Value = pieces[7,2],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "d1",
                                             Value = pieces[7,3],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "e1",
                                             Value = pieces[7,4],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "f1",
                                             Value = pieces[7,5],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "g1",
                                             Value = pieces[7,6],
                                        }
                                    }
                                },
                                new AdaptiveColumn()
                                {
                                    Items =
                                    {
                                    new AdaptiveTextInput()
                                        {
                                             Id = "h1",
                                             Value = pieces[7,7],
                                        }
                                    }
                                },
                                }
                            }
                        }
                    },
                    new AdaptiveContainer()
                    {
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveActionSet()
                            {
                                Actions = new List<AdaptiveAction>()
                                {
                                new AdaptiveSubmitAction()
                                    {
                                        Type = "Action.Http",
                                        Title = "Queen",
                                        Id = "Queen",
                                    },
                                }
                        }
                    }
                }
                }
                };

                String json = card.ToJson();

                var skillCustomData = new
                {
                    template = "AdaptiveCards",
                    cardjson = json,
                };

                var skillResponse = new
                {
                    results = new[]
    {
                    new {
                        tags = new [] {
                            new {
                                actions = new [] {
                                    new {
                                        actionType = "Custom",                  //This is required to be set to "Custom"
                                        customData = skillCustomData,              //Set to custom data object
                                        providerName = "Chess Recognizer"        //The skill name as provider name
                                   }
                                },
                            }
                        }
                    }
                }
                };

            }
            




            JObject resp = new JObject();
            resp.Add(new JProperty("board", pieces));

            log.LogInformation("returned");
            return new JsonResult(resp);
        }
    }
}
