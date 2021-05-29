import AccessClient from "./AccessClient";
import HashObjectStore from "./HashObjectStore";
import PracticeManagerApiClient from "./PracticeManagerApiClient";
import ScoreClient from "./ScoreClient";
import ScoreClientMock from "./ScoreClientMock";
import ScoreClientV2 from "./ScoreClientV2";
import UserClient from "./UserClient";

const API_URI_BASE = process.env.REACT_APP_API_URI_BASE as string;
const ACCESS_API_URI_BASE = process.env.REACT_APP_ACCESS_API_URI_BASE as string;
const SIGNIN_PAGE_URI = process.env.REACT_APP_SIGNIN_PAGE_URI as string;

export const apiClient = new PracticeManagerApiClient(API_URI_BASE);

export const hashObjectStore = new HashObjectStore(apiClient);

// export const scoreClient = new ScoreClient(apiClient, hashObjectStore);

export const scoreClient = new ScoreClientMock(apiClient, hashObjectStore);

export const accessClient = new AccessClient(
  ACCESS_API_URI_BASE,
  SIGNIN_PAGE_URI
);

export const userClient = new UserClient(API_URI_BASE);

export const scoreClientV2 = new ScoreClientV2(API_URI_BASE);
