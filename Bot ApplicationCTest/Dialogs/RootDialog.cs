using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.Internals.Fibers;
using System.Linq;

namespace Bot_ApplicationCTest.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private object stringZ;
        public static List<Model> modelsList = new List<Model>();
        public static List<AccessoriesFamily> accFamiliesList = new List<AccessoriesFamily>();
        public static List<Accessories> accList = new List<Accessories>();
        public static Configuration myConfig;
        string modelName = "";
        string modelImage = "";
        string modelPreconfigId = "";
        string cid = "";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;

            // return our reply to the user
            //await context.PostAsync($"You sent {activity.Text} which was {length} characters");
            Activity replym = activity.CreateReply();

            if (activity.Text == "start")
            {

                //GET BIKE FAMILIES
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://configurator.scramblerducati.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                    HttpResponseMessage resp = await client.GetAsync("bikes/v1/it/it/families/?context=s&complete=true");
                    if (resp.IsSuccessStatusCode)
                    {

                        var jsonString = await resp.Content.ReadAsStringAsync();
                        var families = JsonConvert.DeserializeObject(jsonString);

                        JArray fams = (JArray)families;

                        string familyName = fams[0]["name"].Value<string>().ToString();
                        string familyImage = fams[0]["preview_image"].Value<string>().ToString();

                        JArray models = (JArray)fams[0]["models"];


                        //ATTACHMENT CAROUSEL START

                        //HERO CARD
                        Activity replyToConversation = activity.CreateReply("Scegli il modello");
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();
                        replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                        List<CardImage> cardImages = new List<CardImage>();
                        List<CardAction> cardButtons = new List<CardAction>();
                        List<Attachment> plAttachments = new List<Attachment>();

                        for (int i = 0; i < models.Count; i++)
                        {
                            string modelId = models[i]["id"].Value<string>().ToString();
                            modelName = models[i]["name"].Value<string>().ToString();
                            modelImage = models[i]["preview_image"].Value<string>().ToString();
                            string modelPreconfig = models[i]["preconfigurations"][0]["id"].Value<string>().ToString();

                            modelsList.Add(new Model(modelId, modelName, modelImage, modelPreconfig));

                            cardImages.Add(new CardImage(url: modelImage));

                            CardAction plButton1 = new CardAction()
                            {
                                Value = modelName,
                                Type = "imBack",
                                Title = "Configura"
                            };

                            cardButtons.Add(plButton1);

                            HeroCard plCard1 = new HeroCard()
                            {
                                Title = modelName,
                                //Subtitle = "Scegli",
                                Images = cardImages.ToList().GetRange(i, 1),
                                Buttons = cardButtons.ToList().GetRange(i, 1)
                            };

                            plAttachments.Add(plCard1.ToAttachment());

                            replyToConversation.Attachments.Add(plAttachments[i]);

                        }

                        await context.PostAsync(replyToConversation);

                        //ATTACHMENT CAROUSEL END

                    }
                }


            }
            else if (modelsList.Exists(x => x.modelName.Contains(activity.Text)))
            {
                //FAMIGLIE DI ACCESSORI
                modelPreconfigId = modelsList.Find(x => x.modelName.Contains(activity.Text)).preconfigurationId;

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://configurator.scramblerducati.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage resp = await client.GetAsync("bikes/v1/it/it/preconfigurations/" + modelPreconfigId + "/accessories");
                    if (resp.IsSuccessStatusCode)
                    {

                        var jsonString = await resp.Content.ReadAsStringAsync();
                        var AccFamilies = JsonConvert.DeserializeObject(jsonString);

                        JArray AccFams = (JArray)AccFamilies;

                        //ATTACHMENT CAROUSEL START

                        //HERO CARD
                        Activity replyToConversation = activity.CreateReply("Scegli la categoria categoria");
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();
                        replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                        List<CardImage> cardImages = new List<CardImage>();
                        List<CardAction> cardButtons = new List<CardAction>();
                        List<Attachment> plAttachments = new List<Attachment>();

                        for (int i = 0; i < AccFams.Count; i++)
                        {
                            string AccFamilyName = AccFams[i]["group_name"].Value<string>().ToString();

                            JArray AccModels = (JArray)AccFams[i]["accessories"];

                            string AccFamilyImage = AccModels[0]["preview_image"].Value<string>().ToString();

                            accFamiliesList.Add(new AccessoriesFamily(AccFamilyName, AccFamilyImage));

                            cardImages.Add(new CardImage(url: AccFamilyImage));

                            CardAction plButton1 = new CardAction()
                            {
                                Value = AccFamilyName,
                                Type = "imBack",
                                Title = "Scegli"
                            };

                            cardButtons.Add(plButton1);

                            HeroCard plCard1 = new HeroCard()
                            {
                                Title = AccFamilyName,
                                //Subtitle = "Scegli",
                                Images = cardImages.ToList().GetRange(i, 1),
                                Buttons = cardButtons.ToList().GetRange(i, 1)
                            };

                            plAttachments.Add(plCard1.ToAttachment());

                            replyToConversation.Attachments.Add(plAttachments[i]);

                        }

                        await context.PostAsync(replyToConversation);

                        //ATTACHMENT CAROUSEL END


                    }
                }


                //INIT CONFIGURATION

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://configurator.scramblerducati.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var resp = await client.PostAsJsonAsync("http://configurator.scramblerducati.com/bikes/v1/it/it/configuration/init/" + modelPreconfigId + "?resolution_width=1280","");
                    if (resp.IsSuccessStatusCode)
                    {
                        // Get the URI of the created resource.
                        Uri gizmoUrl = resp.Headers.Location;
                        var jsonString = await resp.Content.ReadAsStringAsync();
                        var initConfig = JsonConvert.DeserializeObject(jsonString);

                        JObject bisConfig = (JObject)initConfig;

                        //Configuration ID
                        string session = bisConfig.GetValue("session").ToString();
                        var desSession = JsonConvert.DeserializeObject(session);
                        JObject bisSession = (JObject)desSession;
                        string cid = bisSession.GetValue("cid").ToString();

                        //Configuration image
                        string config = bisConfig.GetValue("configuration").ToString();
                        var desConfig = JsonConvert.DeserializeObject(config);
                        JObject bizConfig = (JObject)desConfig;
                        string configImage = bizConfig.GetValue("preview_image").ToString();

                        //Configuration price
                        string configPrice = bizConfig.GetValue("total_amount").ToString();
                        var desConfigPrice = JsonConvert.DeserializeObject(configPrice);
                        JObject bizConfigPrice = (JObject)desConfigPrice;
                        string configurationPrice = bizConfigPrice.GetValue("formatted_amount").ToString();

                        //Configuration myConfig = new Configuration(cid, configImage, configurationPrice);
                        myConfig = new Configuration(cid, configImage, configurationPrice);

                    }
                    else
                    {
                        string cc = resp.Content.ToString();
                    }

                }

                //END INIT CONFIGURATION


            }

            else if (accFamiliesList.Exists(x => x.accFamilyName.Contains(activity.Text)))
            {
                //ACCESSORIO INTRA FAMIGLIA

                AccessoriesFamily accFamily = accFamiliesList.Find(x => x.accFamilyName.Contains(activity.Text));
                string accFamilyName = accFamiliesList.Find(x => x.accFamilyName.Contains(activity.Text)).accFamilyName;
                int accFamilyNameIndex = accFamiliesList.IndexOf(accFamily);

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://configurator.scramblerducati.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage resp = await client.GetAsync("bikes/v1/it/it/preconfigurations/" + modelPreconfigId + "/accessories");
                    if (resp.IsSuccessStatusCode)
                    {

                        var jsonString = await resp.Content.ReadAsStringAsync();
                        var AccFamilies = JsonConvert.DeserializeObject(jsonString);

                        JArray AccFams = (JArray)AccFamilies;

                        JArray AccModels = (JArray)AccFams[accFamilyNameIndex]["accessories"];


                        //ATTACHMENT CAROUSEL START

                        //HERO CARD
                        Activity replyToConversation = activity.CreateReply("Scegli l'accessorio");
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();
                        replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                        List<CardImage> cardImages = new List<CardImage>();
                        List<CardAction> cardButtons = new List<CardAction>();
                        List<Attachment> plAttachments = new List<Attachment>();

                        for (int i = 0; i < AccModels.Count; i++)
                        {
                            string AccModelId = AccModels[i]["id"].Value<string>().ToString();

                            string AccModelName = AccModels[i]["name"].Value<string>().ToString();

                            string AccModelImage = AccModels[i]["preview_image"].Value<string>().ToString();

                            accList.Add(new Accessories(AccModelId, AccModelName, AccModelImage));

                            cardImages.Add(new CardImage(url: AccModelImage));

                            CardAction plButton1 = new CardAction()
                            {
                                Value = AccModelName,
                                Type = "imBack",
                                Title = "Aggiungi"
                            };

                            cardButtons.Add(plButton1);

                            HeroCard plCard1 = new HeroCard()
                            {
                                Title = AccModelName,
                                //Subtitle = "Scegli",
                                Images = cardImages.ToList().GetRange(i, 1),
                                Buttons = cardButtons.ToList().GetRange(i, 1)
                            };

                            plAttachments.Add(plCard1.ToAttachment());

                            replyToConversation.Attachments.Add(plAttachments[i]);

                        }

                        await context.PostAsync(replyToConversation);

                        //ATTACHMENT CAROUSEL END

                    }
                }


            }
            else if (accList.Exists(x => x.accName.Contains(activity.Text)))
            {
                //AGGIUNGI ACCESSORIO ALLA CONFIGURAZIONE

                string accId = accList.Find(x => x.accName.Contains(activity.Text)).accId;

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://configurator.scramblerducati.com/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpContent _Body = new StringContent("");
                    _Body.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var resp = await client.PutAsJsonAsync("http://configurator.scramblerducati.com/bikes/v1/it/it/configuration/" + myConfig.cid + "/accessories/" + accId + "?force=false&qty=1&resolution_width=1280", _Body);
                    if (resp.IsSuccessStatusCode)
                    {
                        // Get the URI of the created resource.
                        Uri gizmoUrl = resp.Headers.Location;
                        var jsonString = await resp.Content.ReadAsStringAsync();
                        var initConfig = JsonConvert.DeserializeObject(jsonString);

                        JObject bisConfig = (JObject)initConfig;

                        //Configuration image
                        string config = bisConfig.GetValue("composed_assets").ToString();
                        var desConfig = JsonConvert.DeserializeObject(config);
                        JObject bizConfig = (JObject)desConfig;
                        string configViewImage = bizConfig.GetValue("EE").ToString();
                        var desConfig2 = JsonConvert.DeserializeObject(configViewImage);
                        JArray bizConfig2 = (JArray)desConfig2;
                        //string familyName = fams[0]["name"].Value<string>().ToString();
                        string configImage = bizConfig2[0]["url"].Value<string>().ToString();

                        //Configuration price
                        string configPrice = bisConfig.GetValue("price").ToString();
                        var desConfigPrice = JsonConvert.DeserializeObject(configPrice);
                        JObject bizConfigPrice = (JObject)desConfigPrice;
                        string configurationPrice = bizConfigPrice.GetValue("formatted_amount").ToString();

                        myConfig.confImage = configImage;
                        myConfig.confPrice = configurationPrice;

                        //replym.Text = myConfig.confPrice;

                        //replym.Attachments = new List<Attachment>();
                        //Attachment messageOptions = new Attachment();

                        //IMAGE
                        //messageOptions.ContentType = "image/jpg";
                        //messageOptions.ContentUrl = myConfig.confImage;

                        //replym.Attachments.Add(messageOptions);


                        replym = activity.CreateReply(myConfig.confPrice);

                        replym.Attachments = new List<Attachment>();
                        Attachment altromessageOptions = new Attachment();

                        List<CardAction> endConfigChoices = new List<CardAction>();

                        CardAction buttonYes = new CardAction()
                        {
                            Type = "imBack",
                            Title = "Sì",
                            Value = "Sì"
                        };

                        CardAction buttonNo = new CardAction()
                        {
                            Type = "imBack",
                            Title = "No",
                            Value = "No"
                        };

                        endConfigChoices.Add(buttonYes);
                        endConfigChoices.Add(buttonNo);

                        //associate the Attachments List with the Message and send it
                        //conversation.msgAltro.Attachments.Add(altromessageOptions);

                        HeroCard plCard = new HeroCard()
                        {

                            Title = $"Scegli",
                            //Subtitle = $"{cardContent.Key} Wikipedia Page",
                            //Images = cardImages,
                            Buttons = endConfigChoices
                        };


                        await context.PostAsync(replym);


                    }
                    else
                    {
                        string cc = resp.Content.ToString();
                    }

                }


            }

            else
            {

                await context.PostAsync("Fallback");


            }

            context.Wait(MessageReceivedAsync);
        }
    }
}