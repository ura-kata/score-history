/** ユーザーの情報 */
export interface UserData {
  id: string;
  username: string;
  email: string;
}

const GET_HEADERS = {
  "Content-Type": "application/json",
};

/** ユーザーの操作に関するリクエストを行うクライアント */
export default class UserClient {
  constructor(private baseUrl: string) {}

  /** ユーザーの情報を取得する */
  async getMyData(): Promise<UserData> {
    const requestUrl = new URL("user", this.baseUrl);

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
}
