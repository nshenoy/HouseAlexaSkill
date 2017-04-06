using System.Collections.Generic;

namespace HouseAlexaSkill.Configuration
{
    public class GarageDoorInfo
    {
        public string FriendlyName
        {
            get;
            set;
        }

        public string ButtonFeedKey
        {
            get;
            set;
        }

        public string StatusFeedKey
        {
            get;
            set;
        }

        public List<string> UtteranceIdentifiers
        {
            get;
            set;
        }
    }
}
