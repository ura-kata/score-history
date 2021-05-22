using System;

namespace ScoreHistoryApi.Models
{
    public class AuthorizerData
    {
        public Guid PrincipalId { get; set; }
        public Guid Sub { get; set; }
        public string Email { get; set; }
        public string CognitoUserName { get; set; }
    }
}
