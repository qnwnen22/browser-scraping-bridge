#nullable disable warnings
using BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions;

namespace BrowserScrapingBridge.Server.Sourcing.Farfetch
{
    //https://www.farfetch.com/nl/experience-gateway
    internal class ExperienceGateway
    {
        public string operationName { get; set; } = "SizeAndFitData";
        public class Variables
        {
            public string productId { get; set; }
            public string merchantId { get; set; }
        }
        public Variables variables { get; set; } = new Variables();
        public string query { get; set; } = "query SizeAndFitData($productId: ID!, $merchantId: ID) {\n  user {\n    id\n    preference {\n      unitSystem\n      __typename\n    }\n    __typename\n  }\n  product(id: $productId, merchantId: $merchantId) {\n    ... on Product {\n      id\n      scale {\n        id\n        description\n        abbreviation\n        isOneSize\n        __typename\n      }\n      variations {\n        edges {\n          node {\n            ... on Variation {\n              id\n              price {\n                value {\n                  raw\n                  formatted\n                  __typename\n                }\n                __typename\n              }\n              images {\n                order\n                size1000 {\n                  url\n                  alt\n                  __typename\n                }\n                modelVariationSize\n                modelMeasurement {\n                  height {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  bodyMeasurements {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  __typename\n                }\n                __typename\n              }\n              fitting {\n                type\n                description\n                __typename\n              }\n              measurements {\n                type\n                name\n                imperial {\n                  raw\n                  formatted\n                  __typename\n                }\n                metric {\n                  raw\n                  formatted\n                  __typename\n                }\n                __typename\n              }\n              variationProperties {\n                ... on ScaledSizeVariationProperty {\n                  order\n                  values {\n                    id\n                    order\n                    description\n                    scale {\n                      id\n                      description\n                      __typename\n                    }\n                    __typename\n                  }\n                  __typename\n                }\n                __typename\n              }\n              __typename\n            }\n            ... on VariationUnavailable {\n              product {\n                id\n                __typename\n              }\n              images {\n                order\n                size1000 {\n                  url\n                  alt\n                  __typename\n                }\n                modelVariationSize\n                modelMeasurement {\n                  height {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  bodyMeasurements {\n                    type\n                    name\n                    imperial {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    metric {\n                      raw\n                      formatted\n                      __typename\n                    }\n                    __typename\n                  }\n                  __typename\n                }\n                __typename\n              }\n              __typename\n            }\n            __typename\n          }\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n}\n";
        public ExperienceGateway(string productId, string merchantId)
        {
            this.variables.productId = productId;
            this.variables.merchantId = merchantId;
        }
        public string ToPostString()
        {
            return this.ToSerializeObject();
        }
    }
}
