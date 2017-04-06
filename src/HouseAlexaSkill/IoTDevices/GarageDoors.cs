using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HouseAlexaSkill.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HouseAlexaSkill.IoTDevices
{
    public class GarageDoors : IGarageDoors
    {
        private AdafruitIOConfiguration adafruitIOConfiguration;
        private List<GarageDoorInfo> garageDoorsConfiguration;
        private HttpClient adafruitIOClient;
        private ILogger<GarageDoors> logger;

        public GarageDoors(IOptions<AdafruitIOConfiguration> adafruitIOConfiguration, IOptions<List<GarageDoorInfo>> garageDoorsConfiguration, HttpClient httpClient, ILogger<GarageDoors> logger)
        {
            this.adafruitIOConfiguration = adafruitIOConfiguration.Value;
            this.garageDoorsConfiguration = garageDoorsConfiguration.Value;
            this.adafruitIOClient = httpClient;
            this.logger = logger;
        }

        public async Task<string> GetSingleDoorStatus(string doorIdentifier)
        {
            var feedKey = doorIdentifier;
            var uri = new Uri(this.adafruitIOConfiguration.BaseUrl + $"{this.adafruitIOConfiguration.UserName}/feeds/{feedKey}/data?limit=1");
            var aioKey = this.adafruitIOConfiguration.AIOKey;

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("X-AIO-Key", this.adafruitIOConfiguration.AIOKey);

                using (HttpResponseMessage response = await this.adafruitIOClient.SendAsync(request))
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var jsonResponse = (JArray)JsonConvert.DeserializeObject(responseContent);
                    return (string)jsonResponse[0]["value"];
                }
            }
        }

        public async Task<Dictionary<string, string>> GetAllDoorsStatus()
        {
            Dictionary<string, string> overallStatus = new Dictionary<string, string>();
            foreach (var door in this.garageDoorsConfiguration)
            {
                var doorStatus = await this.GetSingleDoorStatus(door.StatusFeedKey);
                overallStatus.Add(door.FriendlyName, doorStatus);
            }

            return overallStatus;
        }

        public async Task<string> ToggleGarageDoor(string doorAction, string doorIdentifier)
        {
            var response = string.Empty;

            var door = this.garageDoorsConfiguration.FirstOrDefault(g => g.UtteranceIdentifiers.Contains(doorIdentifier.ToLower()));

            if (door == null)
            {
                var utteranceIdentifiersList = this.garageDoorsConfiguration.Select(g => g.UtteranceIdentifiers);
                List<string> identifiers = new List<string>();
                foreach(var utteranceIdentifiers in utteranceIdentifiersList)
                {
                    identifiers.AddRange(utteranceIdentifiers);
                }

                identifiers = identifiers.Distinct().ToList();
                string ids;
                if (identifiers.Count > 1)
                {
                    ids = string.Join(", ", identifiers.Take(identifiers.Count - 1));
                    ids += ", or " + identifiers.Last();
                }
                else
                {
                    ids = identifiers.First();
                }

                return $"Sorry, but I can't identify {doorIdentifier}. Trying saying {ids}.";
            }

            if ((doorAction != "open") && (doorAction != "close"))
            {
                return $"Sorry, but I can only check the status or open and close the garage doors.";
            }

            var doorStatusFeedKey = door.StatusFeedKey;
            var doorFriendlyName = door.FriendlyName;

            var currentDoorStatus = await this.GetSingleDoorStatus(doorStatusFeedKey);
            if (currentDoorStatus.Contains(doorAction))
            {
                return $"Looks like the {doorFriendlyName} is already {currentDoorStatus}.";
            }

            var buttonFeedKey = door.ButtonFeedKey;

            var uri = new Uri(this.adafruitIOConfiguration.BaseUrl + $"{this.adafruitIOConfiguration.UserName}/feeds/{buttonFeedKey}/data");
            var aioKey = this.adafruitIOConfiguration.AIOKey;

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("X-AIO-Key", this.adafruitIOConfiguration.AIOKey);
                request.Content = new StringContent("{ \"value\": 1 }", Encoding.UTF8, "application/json");

                using (var httpResponse = await this.adafruitIOClient.SendAsync(request))
                {
                    httpResponse.EnsureSuccessStatusCode();
                }
            }

            await Task.Delay(2000);

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("X-AIO-Key", this.adafruitIOConfiguration.AIOKey);
                request.Content = new StringContent("{ \"value\": 0 }", Encoding.UTF8, "application/json");

                using (var httpResponse = await this.adafruitIOClient.SendAsync(request))
                {
                    httpResponse.EnsureSuccessStatusCode();
                }
            }

            return ($"Sent the {doorAction} command to the {doorFriendlyName}.");
        }
    }
}
