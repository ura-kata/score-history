const dotenv = require('dotenv');
const jwt = require('jsonwebtoken');
const jwkToPem = require('jwk-to-pem');

dotenv.config();

const environment = process.env.ENVIRONMENT ? process.env.ENVIRONMENT : "Production";

const redirectUrl = process.env.REDIRECT_URL_ILLEGAL_TOKEN

if(!redirectUrl){
  console.log();
  throw Error("'REDIRECT_URL_ILLEGAL_TOKEN' is not found in the environment variable.");
}

const cognitoRegion = process.env.COGNITO_REGION;

if(!cognitoRegion){
  console.log();
  throw Error("'COGNITO_REGION' is not found in the environment variable.");
}
const cognitoUserPoolId = process.env.COGNITO_USER_POOL_ID;

if(!cognitoUserPoolId){
  console.log();
  throw Error("'COGNITO_USER_POOL_ID' is not found in the environment variable.");
}

const jwksText = process.env.JWKS;

if(!jwksText){
  console.log();
  throw Error("'JWKS' is not found in the environment variable.");
}

const jwks = JSON.parse(jwksText);

const iss = `https://cognito-idp.${cognitoRegion}.amazonaws.com/${cognitoUserPoolId}`;

const pems = {};

const keys = jwks.keys;
for(var i = 0; i < keys.length; i++){
    const key = keys[i];
    const key_id = key.kid;
    const modulus = key.n;
    const exponent = key.e;
    const key_type = key.kty;
    const jwk = {kty: key_type, n: modulus, e: exponent};
    const pem = jwkToPem(jwk);
    pems[key_id] = pem;
}

const accessTokenVerify = (accessToken)=> new Promise((resolve)=>{
    const decodedJwt = jwt.decode(accessToken, {complete: true});
    if(!decodedJwt){
        console.log("Could not decode JWT Token.");

        resolve({
            ok: false,
            message: "Could not decode JWT Token."
        });
        return;
    }

    const kid = decodedJwt.header.kid;

    const pem = pems[kid];

    if(!pem){
        console.log("pem is not found.");
        resolve({
            ok: false,
            message: "pem is not found."
        });
        return;
    }

    if (decodedJwt.payload.token_use !== 'access') {
        resolve({
            ok: false,
            message: "token is not access token."
        });
        return;
    }

    jwt.verify(accessToken, pem, {issuer: iss}, (err, payload)=>{
        if (err){
            console.log(err);
            resolve({
                ok: false,
                message: err
            });
            return;
        }
        else{
            console.log(payload);
            resolve({
                ok: true,
                message: payload
            });
            return;
        }
    });
});

const findToken = (cookie) => {
    let accessToken = null;
    let refreshToken = null;

    if (cookie) {
        for(let i = 0; i < cookie.length; i++){
            const cookieValue = cookie[i].value;
            if(!accessToken && 0 <= cookieValue.indexOf('access_token')){
                const startIndex = cookieValue.indexOf('access_token');
                const endIndex = cookieValue.indexOf(';', startIndex);
                accessToken = cookieValue.substring(startIndex + 13, endIndex);
            }
            if(!refreshToken && 0 <= cookieValue.indexOf('refresh_token')){
                const startIndex = cookieValue.indexOf('refresh_token');
                const endIndex = cookieValue.indexOf(';', startIndex);
                refreshToken = cookieValue.substring(startIndex + 14, endIndex);
            }
        }
    }
    return {
        accessToken: accessToken,
        refreshToken: refreshToken
    };
};

exports.handler = async (event, context) => {

    const request = event.Records[0].cf.request;
    const headers = request.headers;

    const requestMethod = request["method"];
    const requestQuerystring = request["querystring"];
    const requestUri = request["uri"];

    console.log(`requestMethod: ${requestMethod}`);
    console.log(`requestQuerystring: ${requestQuerystring}`);
    console.log(`requestUri: ${requestUri}`);

    const uri = request.uri;

    // Complement index.html if no object is specified in the URL path.
    const newUri = uri.replace(/\/$/, '\/index.html');
    request.uri = newUri;

    // Access to'/ auth'does not validate the token.
    if(request.uri.startsWith("/auth")) {
        return request;
    }

    // Validate the token and redirect if it fails.
    const {accessToken, refreshToken} = findToken(headers.cookie);

    const result = await accessTokenVerify(accessToken);
    if(!result.ok){
       const response = {
           status: '302',
           statusDescription: 'not found token',
           headers: {
               location: [{
                   key: 'Location',
                   value: redirectUrl
               }]
           },
       };
       return response;
    }

    return request;


    // const response = {
    //         status: '418',
    //         statusDescription: 'found token',
    //         body: JSON.stringify({accessToken: accessToken, refreshToken: refreshToken, cookie: headers.cookie, verify: result}),
    //     };
    // return response;
    // return request;
};
