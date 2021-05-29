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

const GET_HEADERS = {
  "Content-Type": "application/json",
};

const POST_HEADERS = {
  "Content-Type": "application/json",
};

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
  async create(newScore: NewScore): Promise<void> {
    const requestUrl = new URL("scores/user", this.baseUrl);

    try {
      const response = await fetch(requestUrl.href, {
        method: "POST",
        headers: POST_HEADERS,
        credentials: "include",
        body: JSON.stringify(newScore),
      });
    } catch (err) {
      throw err;
    }
  }
}