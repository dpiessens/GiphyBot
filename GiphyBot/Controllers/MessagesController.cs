using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Connector;

namespace GiphyBot.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly Regex GiphyRegex = new Regex("/giphy (?<search>.+)");

        private readonly Giphy giphyManager;
        private readonly TelemetryClient telemetryClient;

        public MessagesController(TelemetryClient telemetryClient)
        {
            var apiKey = System.Configuration.ConfigurationManager.AppSettings["Giphy.ApiKey"];
            this.giphyManager = new Giphy(apiKey);
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                var match = GiphyRegex.Match(activity.Text ?? string.Empty);
                if (match.Success)
                {
                    var searchText = match.Groups["search"].Value.Trim();

                    telemetryClient?.TrackEvent("BotRequest", new Dictionary<string, string> { { "Search", searchText } });

                    var result =
                        await giphyManager.TranslateIntoGif(new TranslateParameter { Phrase = searchText, Rating = Rating.Pg});
                    
                    Activity reply;
                    if (result == null)
                    {
                        reply = activity.CreateReply("Sorry giphy sucks!");
                    }
                    else
                    {
                        var fakeFileName = $"{searchText.Replace(' ', '_')}.{result.Data.Type}";
                        reply = activity.CreateReply();
                       
                        // return our reply to the user
                        if (reply != null)
                        {
                            reply.Attachments = new List<Attachment>
                            {
                                new Attachment
                                {
                                    ContentType = $"image/{result.Data.Type}",
                                    ContentUrl = result.Data.Images.Original.Url,
                                    Name = fakeFileName
                                }
                            };
                        }
                    }

                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private static Activity HandleSystemMessage(IActivity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}