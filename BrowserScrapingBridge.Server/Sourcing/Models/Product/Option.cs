#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Common;

namespace BrowserScrapingBridge.Server.Sourcing.Models.Product
{    public class Option
    {
        public List<OptionGroup> optionGroups { get; set; }
        public List<DetailOption> detailOptions { get; set; }        public class OptionGroup
        {            public string id { get; set; }            public string _id { get; set; }
            public string optionId { get; set; }
            public List<Name> groupName { get; set; }
            public List<Group> groups { get; set; }            public class Group
            {                public string id { get; set; }                public string _id { get; set; }
                public string groupId { get; set; }
                public List<Name> optionName { get; set; }
                public List<Price>? price { get; set; }
                public int quantity { get; set; }
                public string? optionImage { get; set; }

                public Group() { }

                public Group(string id, Language language, string name, string image)
                {
                    groupId = id;
                    optionName = new List<Name>() { new Name(language, name) };
                    optionImage = image;
                }

                public Group(string id, string language, string name, string image)
                {
                    groupId = id;
                    optionName = new List<Name>() { new Name(language, name) };
                    optionImage = image;
                }
            }

        }        public class DetailOption
        {            public string id { get; set; }            public string _id { get; set; }
            public string optionIds { get; set; }
            public bool isSoldOut { get; set; }
            public List<Name> optionGroupName { get; set; }
            public List<Name> optionName { get; set; }
            public List<Price>? price { get; set; }
            public int quantity { get; set; }
            public string optionImage { get; set; }
        }
    }
}
