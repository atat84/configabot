﻿using System;
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
        public static List<Model> modelsList = new List<Model>();
        public static List<AccessoriesFamily> accFamiliesList = new List<AccessoriesFamily>();
        public static List<Accessories> accList = new List<Accessories>();
        public static Configuration myConfig;
        string modelName = "";
        string currentModel = "";
        string modelImage = "";
        string modelPreconfigId = "";
        string cid = "";

        public async Task StartAsync(IDialogContext context)
        {
            //Welcome message
            //await context.PostAsync($"Welcome to the Search City bot. I'm currently configured to search for things in {defaultCity}");
            await context.PostAsync($"Welcome to the Web Bike Configurator bot. Configure your bike");

            context.Wait(MessageReceivedAsync);

            //return Task.CompletedTask;
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
                await chooseModel(context, activity);
            }
            else if (modelsList.Exists(x => x.modelName.Contains(activity.Text)))
            {
                await chooseModelAccessoriesCategory(context, activity);
            }

            else if (accFamiliesList.Exists(x => x.accFamilyName.Contains(activity.Text)))
            {
                await chooseModelAccessoryInCategory(context, activity);
            }
            else if (accList.Exists(x => x.accName.Contains(activity.Text)))
            {
                await addAccessory(context, activity);
            }
   
            else
            {
                await context.PostAsync("Scrivi start per iniziare");
            }

            context.Wait(MessageReceivedAsync);
        }


        private async Task chooseModel(IDialogContext context, Activity activity)
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


        private async Task chooseModelAccessoriesCategory(IDialogContext context, Activity activity)
        {

            //FAMIGLIE DI ACCESSORI
            modelName = activity.Text;

            modelPreconfigId = modelsList.Find(x => x.modelName.Contains(activity.Text)).preconfigurationId;

            if (currentModel == "")
            {
                initConfiguration();
            }


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


        }


        private async void initConfiguration()
        {

            //Iitialize configuration

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://configurator.scramblerducati.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.PostAsJsonAsync("http://configurator.scramblerducati.com/bikes/v1/it/it/configuration/init/" + modelPreconfigId + "?resolution_width=1280", "");
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

                    myConfig = new Configuration(cid, configImage, configurationPrice, modelName);

                    currentModel = modelName;

                }
                else
                {
                    string cc = resp.Content.ToString();
                }

            }

        }


        private async Task chooseModelAccessoryInCategory(IDialogContext context, Activity activity)
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


        private async Task addAccessory(IDialogContext context, Activity activity)
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

                    //Feedback message
                    Activity replyToAdd = activity.CreateReply(myConfig.confPrice);

                    replyToAdd.Attachments = new List<Attachment>();
                    Attachment altromessageOptions = new Attachment();

                    List<CardImage> cardImages = new List<CardImage>();

                    cardImages.Add(new CardImage(url: myConfig.confImage));

                    List<CardAction> endConfigChoices = new List<CardAction>();

                    CardAction buttonYes = new CardAction()
                    {
                        Type = "imBack",
                        Title = "Aggiungi un altro accessorio",
                        Value = myConfig.modelName
                    };

                    CardAction buttonNo = new CardAction()
                    {
                        Type = "openUrl",
                        Title = "Vieni a provarla!",
                        Value = "https://contact.ducati.com/it/it/scrambler/contact/model-info"
                    };

                    endConfigChoices.Add(buttonYes);
                    endConfigChoices.Add(buttonNo);

                    //associate the Attachments List with the Message and send it


                    HeroCard plCard = new HeroCard()
                    {

                        Title = $"Cosa vuoi fare adesso?",
                        //Subtitle = $"{cardContent.Key} Wikipedia Page",
                        Images = cardImages.ToList().GetRange(0, 1),
                        Buttons = endConfigChoices
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToAdd.Attachments.Add(plAttachment);


                    await context.PostAsync(replyToAdd);


                }
                else
                {
                    string cc = resp.Content.ToString();
                }

            }

        }

    }
}