const DELETE_HEADERS = {
  "Content-Type": "application/json",
};

/** ユーザーの操作に関するリクエストを行うクライアント */
export default class AccessClient {
  constructor(private baseUrl: string, private signInPageUrl: string) {}

  /** ログアウトをする */
  async signout(): Promise<void> {
    const requestUrl = new URL("token", this.baseUrl);

    try {
      const response = await fetch(requestUrl.href, {
        method: "DELETE",
        headers: DELETE_HEADERS,
        credentials: "include",
      });
    } catch (err) {
      throw err;
    }
  }

  /** Sign In Page に遷移する */
  gotoSignInPage(returnUrl: string) {
    // TODO returnUrl をパラメータとして渡して遷移する
    window.location.href = this.signInPageUrl;
  }
}
