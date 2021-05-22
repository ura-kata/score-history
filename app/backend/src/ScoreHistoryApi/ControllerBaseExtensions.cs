using System;
using Microsoft.AspNetCore.Mvc;
using ScoreHistoryApi.Models;

namespace ScoreHistoryApi
{
    public static class ControllerBaseExtensions
    {
        public static AuthorizerData GetAuthorizerData(this ControllerBase self)
        {
            string id = null;
            string name = null;
            string email = null;
            string sub = null;

            foreach (var claim in self.Request.HttpContext.User.Claims)
            {
                switch (claim.Type.ToLowerInvariant())
                {
                    case "principalid":
                        id = claim.Value;
                        break;
                    case "sub":
                        sub = claim.Value;
                        break;
                    case "email":
                        email = claim.Value;
                        break;
                    case "cognito:username":
                        name = claim.Value;
                        break;
                }
            }

            return new AuthorizerData()
            {
                Email = email,
                CognitoUserName = name,
                PrincipalId = Guid.TryParse(id, out var principalid) ? principalid : Guid.Empty,
                Sub = Guid.TryParse(id, out var subId) ? subId : Guid.Empty,
            };
        }
    }
}
