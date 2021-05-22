using System;
using Amazon;
using Amazon.DynamoDBv2;

namespace ScoreHistoryApi.Factories
{
    public class DynamoDbClientFactory
    {
        private string _regionSystemName;
        private Uri _endpointUrl;
        public string RegionSystemName
        {
            get => _regionSystemName;
            set
            {
                _regionSystemName = value;
                if (!(value is null))
                {
                    _endpointUrl = default;
                }
            }
        }

        public Uri EndpointUrl
        {
            get => _endpointUrl;
            set
            {
                _endpointUrl = value;
                if (!(value is null))
                {
                    _regionSystemName = default;
                }

            }
        }

        public DynamoDbClientFactory SetRegionSystemName(string regionSystemName)
        {
            RegionSystemName = regionSystemName;
            return this;
        }

        public DynamoDbClientFactory SetEndpointUrl(Uri endpointUrl)
        {
            EndpointUrl = endpointUrl;
            return this;
        }

        public IAmazonDynamoDB Create()
        {
            if (!(RegionSystemName is default(string)))
            {
                var region = RegionEndpoint.GetBySystemName(RegionSystemName);

                var config = new AmazonDynamoDBConfig()
                {
                    RegionEndpoint = region
                };
                return new AmazonDynamoDBClient(config);
            }

            if (!(EndpointUrl is default(Uri)))
            {
                var config = new AmazonDynamoDBConfig()
                {
                    ServiceURL = EndpointUrl.ToString()
                };
                return new AmazonDynamoDBClient(config);
            }

            throw new InvalidOperationException();
        }
    }
}
