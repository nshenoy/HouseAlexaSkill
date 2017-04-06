using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AlexaSkill.Data;
using HouseAlexaSkill.Configuration;
using HouseAlexaSkill.IoTDevices;


namespace HouseAlexaSkill.Controllers
{
    public class AlexaController : Controller
    {
        private static string[] ResponsePrefixes = {
            "Here's what I found. ",
            "Looks like ",
            " "
        };

        private static Random GlobalRandom = new Random();
        private string randomResponsePrefix
        {
            get
            {
                var rand = new Random(AlexaController.GlobalRandom.Next());
                return AlexaController.ResponsePrefixes[rand.Next(0, AlexaController.ResponsePrefixes.Length-1)];
            }
        }

        private IGarageDoors garageDoors;
        private List<HelloResponse> helloResponses;

        public AlexaController(IGarageDoors garageDoors, IOptions<List<HelloResponse>> helloResponsesConfiguration)
        {
            this.garageDoors = garageDoors;
            this.helloResponses = helloResponsesConfiguration.Value;
        }

        [Route("api/alexa")]
        [HttpGet]
        public async Task<IActionResult> Hello()
        {
            var response = await this.GetGarageDoorsStatus();
            return Ok(response);
        }

        [Route("api/alexa")]
        [HttpPost]
        public async Task<IActionResult> HomeAutomation([FromBody] AlexaRequest request)
        {
            AlexaResponse response = null;

            switch(request.Request.Type)
            {
                case "AMAZON.HelpIntent":
                    response = this.HelpIntentHandler(request);
                    break;
                case "IntentRequest":
                    response = await this.IntentRequestHandler(request);
                    break;
                case "SessionEndedRequest":
                    response = this.SessionEndedRequestHandler(request);
                    break;
            }

            return Ok(response);
        }

        private async Task<AlexaResponse> IntentRequestHandler(AlexaRequest request)
        {
            AlexaResponse response = null;

            switch(request.Request.Intent.Name)
            {
                case "HelloIntent":
                    response = this.HelloIntentHandler(request);
                    break;
                case "GarageDoorsStatusIntent":
                    response = await this.GetGarageDoorsStatus();
                    break;
                case "GarageDoorOpenCloseIntent":
                    response = await this.ToggleGarageDoor(request);
                    break;
            }

            return response;
        }

        private AlexaResponse HelpIntentHandler(AlexaRequest request)
        {
            return new AlexaResponse("Just ask Alexa to say hi to the kids or if the garage doors are open.");
        }

        private AlexaResponse SessionEndedRequestHandler(AlexaRequest request)
        {
            return null;
        }

        private async Task<AlexaResponse> GetGarageDoorsStatus()
        {
            StringBuilder response = new StringBuilder();

            var overallStatus = await this.garageDoors.GetAllDoorsStatus();
            if (overallStatus.Any())
            {
                response.Append(this.randomResponsePrefix);

                var firstStatus = overallStatus.First().Value;
                if (overallStatus.All(d => d.Value == firstStatus))
                {
                    response.Append($"both doors are {firstStatus}.");
                }
                else
                {
                    foreach (KeyValuePair<string, string> status in overallStatus)
                    {
                        response.Append($"The {status.Key} is {status.Value}. ");
                    }
                }
            }
            else
            {
                response.Append("Sorry but I am unable to retrieve data about the garage. Please try again, or check the API or device for issues.");
            }

            return new AlexaResponse(response.ToString());
        }

        private async Task<AlexaResponse> ToggleGarageDoor(AlexaRequest request)
        {
            var doorAction = request.Request.Intent.GetSlots().FirstOrDefault(s => s.Key == "DoorAction").Value;
            var doorIdentifier = request.Request.Intent.GetSlots().FirstOrDefault(s => s.Key == "DoorIdentifier").Value;

            var response = await this.garageDoors.ToggleGarageDoor(doorAction, doorIdentifier);

            return new AlexaResponse(response);
        }

        private AlexaResponse HelloIntentHandler(AlexaRequest request)
        {
            string response;

            var name = request.Request.Intent.GetSlots().FirstOrDefault(s => s.Key == "Name").Value;
            var helloResponse = this.helloResponses.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase));
            if (helloResponse != null)
            {
                response = "";
                var rand = new Random(AlexaController.GlobalRandom.Next());
                response = helloResponse.Responses[rand.Next(0, helloResponse.Responses.Count)];
            }
            else
            {
                response = "Hi!";
            }

            return new AlexaResponse(response);
        }
    }
}
