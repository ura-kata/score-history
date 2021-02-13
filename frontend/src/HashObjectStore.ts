import PracticeManagerApiClient, {
  ScoreV2CommentObject,
  ScoreV2PageObject,
  ScoreV2VersionObject,
} from "./PracticeManagerApiClient";

function assertArgumentUndefined<T>(
  arg: T,
  argName: string
): asserts arg is NonNullable<T> {
  if (arg === undefined) {
    throw new Error(`'${argName}' is undefined`);
  }
  if (arg === null) {
    throw new Error(`'${argName}' is null`);
  }
}

export interface ObjectJsonSet {
  [hash: string]: string;
}

const createLocalStorageKey = (
  owner: string,
  scoreName: string,
  hash: string
) => owner + "/" + scoreName + "/" + hash;

/** Object Store */
export default class HashObjectStore {
  /** コンストラクタ */
  constructor(private apiClient: PracticeManagerApiClient) {}

  setObjectToLocal(
    owner: string,
    scoreName: string,
    hash: string,
    value?: any
  ): void {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");
    assertArgumentUndefined(hash, "hash");

    const localStorageKey = createLocalStorageKey(owner, scoreName, hash);
    if (value) {
      const json = JSON.stringify(value);
      localStorage.setItem(localStorageKey, json);
    } else {
      localStorage.removeItem(localStorageKey);
    }
  }
  /** JSON オブジェクトを取得する */
  async getObjectJson(
    owner: string,
    scoreName: string,
    hash: string[]
  ): Promise<ObjectJsonSet> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");
    assertArgumentUndefined(hash, "hash");

    if (hash.length <= 0) {
      throw new Error();
    }
    if (100 < hash.length) {
      throw new Error();
    }

    const result: ObjectJsonSet = {};

    const notExistedOnLocal: string[] = [];
    hash.forEach(async (h) => {
      const json = localStorage.getItem(
        createLocalStorageKey(owner, scoreName, h)
      );
      if (json === null) {
        notExistedOnLocal.push(h);
        return;
      }
      result[h] = json;
    });

    if (notExistedOnLocal.length === 0) {
      return result;
    }
    try {
      notExistedOnLocal.forEach(async (hash) => {
        const response = await this.apiClient.getHashObjects(
          owner,
          scoreName,
          hash
        );

        const json = JSON.stringify(response);
        localStorage.setItem(
          createLocalStorageKey(owner, scoreName, hash),
          json
        );
        result[hash] = json;
      });

      return result;
    } catch (err) {
      console.log(err);
      throw new Error(`サーバーからオブジェクトの取得に失敗しました`);
    }
  }

  /** Version Object を取得する */
  async getVersionObjects(
    owner: string,
    scoreName: string,
    hash: string[]
  ): Promise<{ [hash: string]: ScoreV2VersionObject }> {
    const jsonSet = await this.getObjectJson(owner, scoreName, hash);

    const result: { [hash: string]: ScoreV2VersionObject } = {};
    Object.entries(jsonSet).forEach(([key, value]) => {
      result[key] = JSON.parse(value) as ScoreV2VersionObject;
    });

    return result;
  }

  /** Page Object を取得する */
  async getPageObjects(
    owner: string,
    scoreName: string,
    hash: string[]
  ): Promise<{ [hash: string]: ScoreV2PageObject }> {
    const jsonSet = await this.getObjectJson(owner, scoreName, hash);

    const result: { [hash: string]: ScoreV2PageObject } = {};
    Object.entries(jsonSet).forEach(([key, value]) => {
      result[key] = JSON.parse(value) as ScoreV2PageObject;
    });

    return result;
  }

  /** Comment Object を取得する */
  async getCommentObjects(
    owner: string,
    scoreName: string,
    hash: string[]
  ): Promise<{ [hash: string]: ScoreV2CommentObject }> {
    const jsonSet = await this.getObjectJson(owner, scoreName, hash);

    const result: { [hash: string]: ScoreV2CommentObject } = {};
    Object.entries(jsonSet).forEach(([key, value]) => {
      result[key] = JSON.parse(value) as ScoreV2CommentObject;
    });

    return result;
  }
}
