const dotenv = require("dotenv");

dotenv.config();


const accessControlAllowOrigin = process.env.ACCESS_CONTROL_ALLOW_ORIGIN;

if (!accessControlAllowOrigin) {
  throw Error(
    "'ACCESS_CONTROL_ALLOW_ORIGIN' is not found in the environment variable."
  );
}

const cookieDomain = process.env.COOKIE_DOMAIN;

if (!cookieDomain) {
  throw Error(
    "'COOKIE_DOMAIN' is not found in the environment variable."
  );
}

const getCookieToken = (cookie) => {

    if(!cookie){
        return undefined;
    }
    if(!cookie.length){
        return undefined;
    }
    let accessToken = undefined;
    let refreshToken = undefined;

    cookie.forEach((c) => {
        c.split(';').forEach(item=>{
            const l = item.trim();
            if (!accessToken && l.startsWith("access_token=")) {
                accessToken = l.substring(13);
            } else if (!refreshToken && l.startsWith("refresh_token=")) {
                refreshToken = l.substring(14);
            }
        });
    });

    if (accessToken && refreshToken) {
        return {
            accessToken: accessToken,
            refreshToken: refreshToken,
        };
    }
    return undefined;
};

const getRequestBody = (event) => {
    const body = event.body ? JSON.parse(event.body) : {};

    return body;
};

exports.handler = async (event, context) => {

    console.log("start");

    const requestPath = event.path;

    console.log("request path");
    console.log(requestPath);

    console.log("request headers");
    console.log(event.headers);
    console.log("request multi value headers");
    console.log(event.multiValueHeaders);

    const requestCookie = event.multiValueHeaders ? event.multiValueHeaders['cookie'] : undefined;

    console.log("request cookie");
    console.log(requestCookie);

    const cookieToken = getCookieToken(requestCookie);

    console.log("cookie token");
    console.log(cookieToken);

    const queryStringParameters = event.queryStringParameters;

    console.log("queryStringParameters");
    console.log(queryStringParameters);

    const multiValueQueryStringParameters = event.multiValueQueryStringParameters;

    console.log("multiValueQueryStringParameters");
    console.log(multiValueQueryStringParameters);


    let responseBody;
    let statusCode = '200';
    let responseHeaders = {
        'Content-Type': 'application/json',
    };
    const multiValueHeaders = {};

    const now = Date.now();

    try {
        switch (event.httpMethod) {
            case 'DELETE':
                if(requestPath.startsWith('/token')){
                    responseBody = {"method": "get"};
                    multiValueHeaders["Set-Cookie"] = [
                        `access_token=deleted; expires=Thu, 01-Jan-1970 00:00:01 GMT; Path=/; Domain=${cookieDomain}; HttpOnly; Secure`,
                        `refresh_token=deleted; expires=Thu, 01-Jan-1970 00:00:01 GMT; Path=/; Domain=${cookieDomain}; HttpOnly; Secure`,
                        `date=${(new Date()).toISOString()}; Path=/; Domain=${cookieDomain}; HttpOnly; Secure`
                    ];
                    responseBody = {"method": "delete"};
                    break;
                }
                throw new Error(`Unsupported method "${event.httpMethod}"`);
            case 'GET':
                if(requestPath.startsWith('/token')){
                    //responseHeaders = {};

                    const accessToken = queryStringParameters['access_token'];
                    const refreshToken = queryStringParameters['refresh_token'];

                    if(!accessToken || !refreshToken){
                        throw new Error(`token not found`);
                    }

                    responseBody = {"method": "get"};
                    multiValueHeaders["Set-Cookie"] = [
                        `access_token=${accessToken}; Domain=${cookieDomain}; Path=/; HttpOnly; Secure`,
                        `refresh_token=${refreshToken}; Domain=${cookieDomain}; Path=/; HttpOnly; Secure`,
                        `date=${(new Date()).toISOString()}; Domain=.${cookieDomain}; Path=/; HttpOnly; Secure`
                    ];
                    break;
                }
                throw new Error(`Unsupported method "${event.httpMethod}"`);
            case 'POST':
                if(requestPath.startsWith('/token')){

                    const requestBody = getRequestBody(event)

                    console.log("request body");
                    console.log(requestBody);

                    const accessToken = requestBody['accessToken'];
                    const refreshToken = requestBody['refreshToken'];

                    if(!accessToken || !refreshToken){
                        throw new Error(`token not found`);
                    }

                    responseBody = {"method": "post"};
                    multiValueHeaders["Set-Cookie"] = [
                        `access_token=${accessToken}; Domain=${cookieDomain}; Path=/; HttpOnly; Secure,`,
                        `refresh_token=${refreshToken}; Domain=${cookieDomain}; Path=/; HttpOnly; Secure`,
                        `date=${(new Date()).toISOString()}; Domain=.${cookieDomain}; Path=/; HttpOnly; Secure`
                    ];
                    break;
                }
                throw new Error(`Unsupported method "${event.httpMethod}"`);
            default:
                throw new Error(`Unsupported method "${event.httpMethod}"`);
        }

        responseHeaders['Access-Control-Allow-Headers'] = 'Content-Type';
        responseHeaders['Access-Control-Allow-Origin'] = accessControlAllowOrigin;
        responseHeaders['Access-Control-Allow-Credentials'] = "true";
        responseHeaders["Access-Control-Allow-Methods"] = "OPTIONS,POST";
        console.log("success");
    } catch (err) {
        statusCode = '400';
        responseBody = err.message;
        console.log("err");
    } finally {
        responseBody = JSON.stringify(responseBody);
        console.log("finally");
    }

    return {
        statusCode,
        body: responseBody,
        headers: responseHeaders,
        multiValueHeaders,
    };
};
