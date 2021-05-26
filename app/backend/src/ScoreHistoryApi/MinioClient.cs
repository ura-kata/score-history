using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ScoreHistoryApi
{
    public class MinioClient : AmazonS3Client
    {
        public MinioClient(AWSCredentials credentials):base(credentials)
        {

        }
        public MinioClient(AWSCredentials credentials, AmazonS3Config clientConfig):base(credentials,clientConfig)
        {

        }
        public MinioClient(AmazonS3Config clientConfig):base(clientConfig)
        {

        }
        public override Task<PutACLResponse> PutACLAsync(PutACLRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new PutACLResponse());
        }
    }
}
