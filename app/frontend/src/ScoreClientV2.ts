/** 楽譜のサマリー情報 */
export interface ScoreSummary {
  id: string;
  owner_id: string;
  title: string;
  description: string;
}

/** 新しい楽譜 */
export interface NewScore {
  title: string;
  description?: string;
}

/** 新しいく作られた楽譜 */
export interface NewlyScore {
  id: string;
}

/** 楽譜のページ */
export interface ScorePage {
  id: string;
  itemId: string;
  page: string;
  objectName: string;
}

/** 楽譜のアノテーション */
export interface ScoreAnnotation {
  id: string;
  contentHash: string;
}

/** 楽譜のデータ */
export interface ScoreData {
  title: string;
  descriptionHash: string;
  pages: ScorePage[];
  annotations: ScoreAnnotation[];
}

/** アクセスのタイプ */
type Accesses = "private" | "public";

/** 楽譜の詳細 */
export interface ScoreDetail {
  createAt: Date;
  updateAt: Date;
  data: ScoreData;
  dataHash: string;
  access: Accesses;
  hashSet: { [hash: string]: string };
}

//-------------------------------------------------------------------------------

const GET_HEADERS = {
  "Content-Type": "application/json",
};

const POST_HEADERS = {
  "Content-Type": "application/json",
};

/** 初期化されていない */
const NotInitializedScore = 520;

/** 楽譜がない */
const NotFoundScore = 521;

/** 楽譜に関するリクエストを実行するクライアント */
export default class ScoreClientV2 {
  constructor(private baseUrl: string) {}

  /** 自分の楽譜のサマリーを一覧で取得する */
  async getMyScoreSummaries(): Promise<ScoreSummary[]> {
    const requestUrl = new URL("scores/user", this.baseUrl);

    try {
      const response = await fetch(requestUrl.href, {
        method: "GET",
        headers: GET_HEADERS,
        credentials: "include",
      });

      const json = await response.json();
      return json;
    } catch (err) {
      throw err;
    }
  }

  /** 楽譜を作成する */
  async create(newScore: NewScore): Promise<NewlyScore> {
    const requestUrl = new URL("scores/user", this.baseUrl);

    try {
      const response = await fetch(requestUrl.href, {
        method: "POST",
        headers: POST_HEADERS,
        credentials: "include",
        body: JSON.stringify(newScore),
      });

      if (response.ok) {
        return (await response.json()) as NewlyScore;
      } else if (response.status === NotInitializedScore) {
        const initRequestUrl = new URL("scores/new", this.baseUrl);
        const initResponse = await fetch(initRequestUrl.href, {
          method: "POST",
          headers: POST_HEADERS,
          credentials: "include",
        });

        if (initResponse.ok) {
          const response2 = await fetch(requestUrl.href, {
            method: "POST",
            headers: POST_HEADERS,
            credentials: "include",
            body: JSON.stringify(newScore),
          });

          if (response2.ok) {
            return (await response2.json()) as NewlyScore;
          }
        }
      }
      throw new Error("楽譜の作成に失敗");
    } catch (err) {
      throw err;
    }
  }

  async getDetail(scoreId: string): Promise<ScoreDetail | undefined> {
    const requestUrl = new URL(`scores/user/${scoreId}`, this.baseUrl);

    try {
      const response = await fetch(requestUrl.href, {
        method: "GET",
        headers: GET_HEADERS,
        credentials: "include",
      });

      if (response.ok) {
        var json = await response.json();

        return json;
      } else if (response.status === NotFoundScore) {
        return undefined;
      } else if (response.status === 404) {
        return undefined;
      }
      throw new Error();
    } catch (err) {
      throw err;
    }
  }
}
