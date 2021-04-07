import {
  APIGatewayAuthorizerResult,
  APIGatewayRequestAuthorizerEvent,
} from 'aws-lambda/trigger/api-gateway-authorizer';
import * as jwt from 'jsonwebtoken';
import jwkToBuffer = require('jwk-to-pem');

const ENVIRONMENT = process.env.ENVIRONMENT
  ? process.env.ENVIRONMENT
  : 'Production';

const COGNITO_REGION = process.env.COGNITO_REGION;

if (!COGNITO_REGION) {
  console.log();
  throw Error("'COGNITO_REGION' is not found in the environment variable.");
}
const COGNITO_USER_POOL_ID = process.env.COGNITO_USER_POOL_ID;

if (!COGNITO_USER_POOL_ID) {
  console.log();
  throw Error(
    "'COGNITO_USER_POOL_ID' is not found in the environment variable."
  );
}

const JWKS_JSON = process.env.JWKS_JSON;

if (!JWKS_JSON) {
  console.log();
  throw Error("'JWKS_JSON' is not found in the environment variable.");
}

const jwks = JSON.parse(JWKS_JSON);

const iss = `https://cognito-idp.${COGNITO_REGION}.amazonaws.com/${COGNITO_USER_POOL_ID}`;

const pems: { [keyId: string]: string } = {};

const keys = jwks.keys;
for (var i = 0; i < keys.length; i++) {
  const key = keys[i];
  const key_id = ((key as any) as { kid: string }).kid;
  const pem = jwkToBuffer(key);
  pems[key_id] = pem;
}

const decodeToken = (token: string | null) => {
  if (!token) return undefined;
  const decodedJwt = jwt.decode(token, { complete: true });
  if (decodedJwt) return decodedJwt;
  return undefined;
};

const accessTokenVerify = (
  accessToken: string | null
): Promise<{ ok: boolean; message: string; payload?: any }> =>
  new Promise<{ ok: boolean; message: string; payload?: any }>((resolve) => {
    if (!accessToken) {
      console.log(`The access token could not be recognized.(${accessToken})`);

      resolve({
        ok: false,
        message: `The access token could not be recognized.(${accessToken})`,
      });
      return;
    }
    const decodedJwt = jwt.decode(accessToken, { complete: true });
    if (!decodedJwt) {
      console.log('Could not decode JWT Token.');

      resolve({
        ok: false,
        message: 'Could not decode JWT Token.',
      });
      return;
    }

    const kid = decodedJwt.header.kid;

    const pem = pems[kid];

    if (!pem) {
      console.log('pem is not found.');
      resolve({
        ok: false,
        message: 'pem is not found.',
      });
      return;
    }

    if (decodedJwt.payload.token_use !== 'access') {
      resolve({
        ok: false,
        message: 'token is not access token.',
      });
      return;
    }

    jwt.verify(accessToken, pem, { issuer: iss }, (err, payload) => {
      if (err) {
        console.log(err);
        resolve({
          ok: false,
          message: JSON.stringify(err),
        });
        return;
      } else {
        console.log(payload);
        resolve({
          ok: true,
          message: 'success',
          payload: payload,
        });
        return;
      }
    });
  });

const findToken = (
  cookie?: string[]
): {
  accessToken: string | null;
  refreshToken: string | null;
  idToken: string | null;
} => {
  let accessToken = null;
  let refreshToken = null;
  let idToken = null;

  if (cookie && cookie.length) {
    for (let i = 0; i < cookie.length; i++) {
      const cookieValue = cookie[i];
      if (!accessToken && 0 <= cookieValue.indexOf('access_token')) {
        const startIndex = cookieValue.indexOf('access_token');
        const endIndex = cookieValue.indexOf(';', startIndex);
        accessToken =
          0 <= endIndex
            ? cookieValue.substring(startIndex + 13, endIndex)
            : cookieValue.substring(startIndex + 13);
      }
      if (!refreshToken && 0 <= cookieValue.indexOf('refresh_token')) {
        const startIndex = cookieValue.indexOf('refresh_token');
        const endIndex = cookieValue.indexOf(';', startIndex);
        refreshToken =
          0 <= endIndex
            ? cookieValue.substring(startIndex + 14, endIndex)
            : cookieValue.substring(startIndex + 14);
      }
      if (!idToken && 0 <= cookieValue.indexOf('id_token')) {
        const startIndex = cookieValue.indexOf('id_token');
        const endIndex = cookieValue.indexOf(';', startIndex);
        idToken =
          0 <= endIndex
            ? cookieValue.substring(startIndex + 9, endIndex)
            : cookieValue.substring(startIndex + 9);
      }
    }
  }
  return {
    accessToken: accessToken,
    refreshToken: refreshToken,
    idToken: idToken,
  };
};

// Help function to generate an IAM policy
const generatePolicy = (
  principalId: string,
  effect: 'Allow' | 'Deny',
  resource: string,
  optionContext: { [key: string]: string }
): APIGatewayAuthorizerResult => {
  // Required output:
  const authResponse: APIGatewayAuthorizerResult = {
    principalId: principalId,
    policyDocument: {
      Version: '2012-10-17', // default version
      Statement: [
        {
          Action: 'execute-api:Invoke', // default action
          Effect: effect,
          Resource: resource,
        },
      ],
    },
  };

  if (optionContext) {
    authResponse.context = optionContext;
  }
  return authResponse;
};

var generateAllow = function (
  principalId: string,
  resource: string,
  optionContext: { [key: string]: string }
) {
  return generatePolicy(principalId, 'Allow', resource, optionContext);
};

var generateDeny = function (
  principalId: string,
  resource: string,
  optionContext: { [key: string]: string }
) {
  return generatePolicy(principalId, 'Deny', resource, optionContext);
};

export async function handler(
  event: APIGatewayRequestAuthorizerEvent,
  context: any
) {
  const cookie =
    event.multiValueHeaders !== null
      ? event.multiValueHeaders['Cookie']
      : undefined;

  const path = event.path;

  // Access to'/auth'does not validate the token.
  // if(path.startsWith("/auth")) {
  //   return generateAllow('me', event.methodArn);
  // }

  // Validate the token and redirect if it fails.
  const { accessToken, refreshToken, idToken } = findToken(cookie);

  const result = await accessTokenVerify(accessToken);
  if (!result.ok) {
    context.fail('Unauthorized');
    return;
  }

  const idTokenJwt = decodeToken(idToken);

  const sub = idTokenJwt ? idTokenJwt.payload.sub : '';
  const email = idTokenJwt ? idTokenJwt.payload.email : '';
  const cognitoUserName = idTokenJwt
    ? idTokenJwt.payload['cognito:username']
    : '';

  const principalId: string = idTokenJwt ? sub : result.payload.username;

  const optionContext: { [key: string]: string } = {};
  optionContext['sub'] = sub;
  optionContext['email'] = email;
  optionContext['cognito:username'] = cognitoUserName;

  return generateAllow(principalId, event.methodArn, optionContext);
}
