// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuizBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class QuizBotBot : IBot
    {
        private readonly QuizBotAccessors _accessors;
        private readonly ILogger _logger;

        public static readonly string LuisKey = "QuizApp";

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>

        // Commented below for LUIS integration
        //public QuizBotBot(ConversationState conversationState, ILoggerFactory loggerFactory)
        //{
        //    if (conversationState == null)
        //    {
        //        throw new System.ArgumentNullException(nameof(conversationState));
        //    }

        //    if (loggerFactory == null)
        //    {
        //        throw new System.ArgumentNullException(nameof(loggerFactory));
        //    }

        //    _accessors = new QuizBotAccessors(conversationState)
        //    {
        //        CounterState = conversationState.CreateProperty<CounterState>(QuizBotAccessors.CounterStateName),
        //    };

        //    _logger = loggerFactory.CreateLogger<QuizBotBot>();
        //    _logger.LogTrace("Turn start.");
        //}

        // Services configured from the ".bot" file.
        private readonly BotServices _services;

        // Initializes a new instance of the LuisBot class.
        public QuizBotBot(BotServices services)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            if (!_services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration....");
            }
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Commented below for LUIS integration 

                //// Get the conversation state from the turn context.
                //var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                //// Bump the turn count for this conversation.
                //state.TurnCount++;

                //// Set the property using the accessor.
                //await _accessors.CounterState.SetAsync(turnContext, state);

                //// Save the new turn count into the conversation state.
                //await _accessors.ConversationState.SaveChangesAsync(turnContext);

                //// Echo back to the user whatever they typed.
                //var responseMessage = $"Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
                //await turnContext.SendActivityAsync(responseMessage);

                // Check LUIS model
                var recognizerResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);

                var entityFound = ParseLuisForEntities(recognizerResult);

                var topIntent = recognizerResult?.GetTopScoringIntent();
                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                {
                    await turnContext.SendActivityAsync($"==>LUIS Top Scoring Intent: {topIntent.Value.intent}, Score: {topIntent.Value.score}\n");
                }
                else
                {
                    var msg = @"No LUIS intents were found";
                    await turnContext.SendActivityAsync(msg);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

        private object ParseLuisForEntities(RecognizerResult recognizerResult)
        {
           
                var result = string.Empty;

                // recognizerResult.Entities returns type JObject.
                foreach (var entity in recognizerResult.Entities)
                {
                    // Parse JObject for a known entity types: Appointment, Meeting, and Schedule.
                    var dateFound = JObject.Parse(entity.Value.ToString())["text"];
                    var cityFound = JObject.Parse(entity.Value.ToString())["noida"];
                    

                    // We will return info on the first entity found.
                    if (dateFound != null)
                    {
                        // use JsonConvert to convert entity.Value to a dynamic object.
                        dynamic o = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                        if (o.Appointment[0] != null)
                        {
                            // Find and return the entity type and score.
                            var entType = o.Appointment[0].type;
                            var entScore = o.Appointment[0].score;
                            result = "Entity: " + entType + ", Score: " + entScore + ".";

                           // return result;
                        }
                    }

                    if (cityFound != null)
                    {
                        // use JsonConvert to convert entity.Value to a dynamic object.
                        dynamic o = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                        if (o.Meeting[0] != null)
                        {
                            // Find and return the entity type and score.
                            var entType = o.Meeting[0].type;
                            var entScore = o.Meeting[0].score;
                            result = "Entity: " + entType + ", Score: " + entScore + ".";

                            //return result;
                        }
                    }

                   
                }

                if (result.Length > 0)
            {
                return result;

            }
            else
            {
                // No entities were found, empty string returned.
                return "No entities found in LUIS response";
            }

                
            }
        }
  
}
