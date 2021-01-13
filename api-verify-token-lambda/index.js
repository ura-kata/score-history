const dotenv = require("dotenv");
const jwt = require("jsonwebtoken");
const jwkToPem = require("jwk-to-pem");

dotenv.config();

const environment = process.env.ENVIRONMENT
  ? process.env.ENVIRONMENT
  : "Production";

const cognitoRegion = process.env.COGNITO_REGION;

if (!cognitoRegion) {
  console.log();
  throw Error("'COGNITO_REGION' is not found in the environment variable.");
}
const cognitoUserPoolId = process.env.COGNITO_USER_POOL_ID;

if (!cognitoUserPoolId) {
  console.log();
  throw Error(
    "'COGNITO_USER_POOL_ID' is not found in the environment variable."
  );
}

const jwksText = process.env.JWKS;

if (!jwksText) {
  console.log();
  throw Error("'JWKS' is not found in the environment variable.");
}

const jwks = JSON.parse(jwksText);

const iss = `https://cognito-idp.${cognitoRegion}.amazonaws.com/${cognitoUserPoolId}`;

const pems = {};

const keys = jwks.keys;
for (var i = 0; i < keys.length; i++) {
  const key = keys[i];
  const key_id = key.kid;
  const modulus = key.n;
  const exponent = key.e;
  const key_type = key.kty;
  const jwk = { kty: key_type, n: modulus, e: exponent };
  const pem = jwkToPem(jwk);
  pems[key_id] = pem;
}

const decodeToken = (token) => {
  if(!token) return undefined;
  const decodedJwt = jwt.decode(token, { complete: true });
  if(decodedJwt) return decodedJwt;
  return undefined;
};

const accessTokenVerify = (accessToken) =>
  new Promise((resolve) => {
    if(!accessToken){
      console.log(`The access token could not be recognized.(${accessToken})`);

      resolve({
        ok: false,
        message: `The access token could not be recognized.(${accessToken})`,
      });
      return;
    }
    const decodedJwt = jwt.decode(accessToken, { complete: true });
    if (!decodedJwt) {
      console.log("Could not decode JWT Token.");

      resolve({
        ok: false,
        message: "Could not decode JWT Token.",
      });
      return;
    }

    const kid = decodedJwt.header.kid;

    const pem = pems[kid];

    if (!pem) {
      console.log("pem is not found.");
      resolve({
        ok: false,
        message: "pem is not found.",
      });
      return;
    }

    if (decodedJwt.payload.token_use !== "access") {
      resolve({
        ok: false,
        message: "token is not access token.",
      });
      return;
    }

    jwt.verify(accessToken, pem, { issuer: iss }, (err, payload) => {
      if (err) {
        console.log(err);
        resolve({
          ok: false,
          message: err,
        });
        return;
      } else {
        console.log(payload);
        resolve({
          ok: true,
          message: "success",
          payload: payload
        });
        return;
      }
    });
  });

const findToken = (cookie) => {
  let accessToken = null;
  let refreshToken = null;
  let idToken = null;

  if (cookie && cookie.length) {
    for (let i = 0; i < cookie.length; i++) {
      const cookieValue = cookie[i];
      if (!accessToken && 0 <= cookieValue.indexOf("access_token")) {
        const startIndex = cookieValue.indexOf("access_token");
        const endIndex = cookieValue.indexOf(";", startIndex);
        accessToken = 0 <= endIndex ? cookieValue.substring(startIndex + 13, endIndex) : cookieValue.substring(startIndex + 13);
      }
      if (!refreshToken && 0 <= cookieValue.indexOf("refresh_token")) {
        const startIndex = cookieValue.indexOf("refresh_token");
        const endIndex = cookieValue.indexOf(";", startIndex);
        refreshToken = 0 <= endIndex ? cookieValue.substring(startIndex + 14, endIndex) : cookieValue.substring(startIndex + 14);
      }
      if (!idToken && 0 <= cookieValue.indexOf("id_token")) {
        const startIndex = cookieValue.indexOf("id_token");
        const endIndex = cookieValue.indexOf(";", startIndex);
        idToken = 0 <= endIndex ? cookieValue.substring(startIndex + 9, endIndex) : cookieValue.substring(startIndex + 9);
      }
    }
  }
  return {
    accessToken: accessToken,
    refreshToken: refreshToken,
    idToken: idToken
  };
};

// Help function to generate an IAM policy
var generatePolicy = function (principalId, effect, resource, optionContext) {
  // Required output:
  var authResponse = {};
  authResponse.principalId = principalId;
  if (effect && resource) {
    var policyDocument = {};
    policyDocument.Version = "2012-10-17"; // default version
    policyDocument.Statement = [];
    var statementOne = {};
    statementOne.Action = "execute-api:Invoke"; // default action
    statementOne.Effect = effect;
    statementOne.Resource = resource;
    policyDocument.Statement[0] = statementOne;
    authResponse.policyDocument = policyDocument;
  }

  if(optionContext){
    authResponse.context = optionContext;
  }
  return authResponse;
};

var generateAllow = function (principalId, resource, optionContext) {
  return generatePolicy(principalId, "Allow", resource, optionContext);
};

var generateDeny = function (principalId, resource, optionContext) {
  return generatePolicy(principalId, "Deny", resource, optionContext);
};

exports.handler = async (event, context) => {
  const cookie = event.multiValueHeaders.Cookie;

  const path = event.path;

  // Access to'/auth'does not validate the token.
  // if(path.startsWith("/auth")) {
  //   return generateAllow('me', event.methodArn);
  // }

  // Validate the token and redirect if it fails.
  const { accessToken, refreshToken, idToken } = findToken(cookie);

  const result = await accessTokenVerify(accessToken);
  if (!result.ok) {
    context.fail("Unauthorized");
    return;
  }

  const idTokenJwt = decodeToken(idToken);

  const sub = idTokenJwt ? idTokenJwt.payload.sub : "";
  const email = idTokenJwt ? idTokenJwt.payload.email : "";
  const cognitoUserName = idTokenJwt ? idTokenJwt.payload["cognito:username"] : "";

  const principalId = idTokenJwt ? sub : result.payload.username;

  const optionContext = {};
  optionContext["sub"] = sub;
  optionContext["email"] = email;
  optionContext["cognito:username"] = cognitoUserName;


  return generateAllow(principalId, event.methodArn, optionContext);
};
