using System;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;

namespace ScoreHistoryApi.Factories
{
    public class S3ClientFactory
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

        public AWSCredentials Credentials { get; set; }

        public S3ClientFactory SetRegionSystemName(string regionSystemName)
        {
            RegionSystemName = regionSystemName;
            return this;
        }

        public S3ClientFactory SetEndpointUrl(Uri endpointUrl)
        {
            EndpointUrl = endpointUrl;
            return this;
        }

        public S3ClientFactory SetCredentials(AWSCredentials credentials)
        {
            Credentials = credentials;
            return this;
        }

        public S3ClientFactory SetCredentials(string accessKey, string secretKey)
        {
            if (accessKey is null)
                throw new ArgumentNullException(nameof(accessKey));
            if (secretKey is null)
                throw new ArgumentNullException(nameof(secretKey));

            Credentials = new BasicAWSCredentials(accessKey, secretKey);
            return this;
        }

        public IAmazonS3 Create()
        {
            if (!(RegionSystemName is default(string)))
            {
                var region = RegionEndpoint.GetBySystemName(RegionSystemName);

                var config = new AmazonS3Config()
                {
                    RegionEndpoint = region
                };
                return new AmazonS3Client(config);
            }

            if (!(EndpointUrl is default(Uri)))
            {
                var config = new AmazonS3Config()
                {
                    RegionEndpoint = RegionEndpoint.USEast1,
                    ServiceURL = EndpointUrl.ToString(),
                    ForcePathStyle = true,
                };

                if (Credentials is null)
                {
                    return new AmazonS3Client(config);
                }
                else
                {
                    return new AmazonS3Client(Credentials, config);
                }
            }

            throw new InvalidOperationException();
        }
    }
}
