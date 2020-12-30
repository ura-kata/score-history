// import UUID from "uuid";
import { v4 as uuid } from "uuid";
import CryptoJS from "crypto-js";

const cognitoDomain = "pm-uttne.auth.us-east-1.amazoncognito.com";
const clientId = "1aealljop75snqvo6sq29h9g02";
const redirectUri = "http://localhost:8080/";

const base64URLEncode = (base64: string): string => {
  return base64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
};

const createVerifier = () => {
  const base64 = CryptoJS.lib.WordArray.random(32).toString(
    CryptoJS.enc.Base64
  );
  return base64URLEncode(base64);
};

const createChallenge = (verifier: string): string => {
  const base64 = CryptoJS.SHA256(verifier).toString(CryptoJS.enc.Base64);
  return base64URLEncode(base64);
};

const redirectAuth = () => {
  console.log("redirectAuth");

  const state = uuid();
  console.log(state);

  const verifier = createVerifier();

  window.sessionStorage.setItem("state", state);
  window.sessionStorage.setItem("verifier", verifier);

  const challenge = createChallenge(verifier);

  const url =
    `https://${cognitoDomain}/oauth2/authorize?` +
    [
      "response_type=code",
      "client_id=" + clientId,
      "redirect_uri=" + redirectUri,
      "state=" + state,
      "scope=openid+email",
      "code_challenge_method=S256",
      "code_challenge=" + challenge,
    ].join("&");

  console.log(url);
  location.href = url;
};

const checkState = (state: string): boolean => {
  const savedState = window.sessionStorage.getItem("state");

  return state === savedState;
};

const getCodeAndState = (): { code: string; state: string } | undefined => {
  const params = location.search;
  const list = params.split(/[?]|[&]/);

  let code: string | undefined = undefined;
  let state: string | undefined = undefined;

  list.forEach((l) => {
    if (!code && l.startsWith("code=")) {
      code = l.substring(5);
    } else if (!state && l.startsWith("state=")) {
      state = l.substring(6);
    }
  });

  if (code && state && checkState(state)) {
    return {
      code: code,
      state: state,
    };
  }
  return undefined;
};

interface Token {
  access_token: string;
  refresh_token: string;
  id_token: string;
  token_type: string;
  expires_in: number;
}

const getToken = async (code: string): Promise<Token | undefined> => {
  const verifier = window.sessionStorage.getItem("verifier");
  if (!verifier) {
    console.log("verifier is not found.");
    return undefined;
  }

  const url = `https://${cognitoDomain}/oauth2/token`;

  const body = [
      "grant_type=authorization_code",
      "client_id=" + clientId,
      "code=" + code,
      "redirect_uri=" + redirectUri,
      "scope=openid+email",
      "code_verifier=" + verifier,
    ].join("&");
  const headers = {
    "Content-Type": "application/x-www-form-urlencoded",
  };

  try {
    const response = await fetch(url, {
      method: "POST",
      headers: headers,
      body: body,
    });

    if (!response.ok) {
      console.log(response.status);
      console.log(await response.text());
      return undefined;
    }

    const json = await response.json();

    return json as Token;
  } catch (err) {
    console.log(err);
    return undefined;
  }
};

window.onload = async () => {
  console.log("hello world.");
  alert(location.pathname);
  alert(location.search);

  const codeAndState = getCodeAndState();

  if (codeAndState) {
    // get token
    console.log("get token");
    const token = await getToken(codeAndState.code);

    console.log(token);
  } else {
    // redirect oauth2/authorize
    console.log("redirect oauth2/authorize");
    redirectAuth();
  }
};

