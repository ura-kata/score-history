import queryString from "query-string";

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

const postHeaders = {
  "Content-Type": "application/json",
};

const patchHeaders = {
  "Content-Type": "application/json",
};

export interface ScoreVersionPage {
  image_url: string;
  thumbnail_url: string;
  no: number;
  description: string;
}
export interface ScoreVersion {
  version: number;
  description: string;
  create_at: Date;
  update_at: Date;
  pages: ScoreVersionPage[];
}

export interface SummaryScoreVersion {
  version: number;
  description: string;
  page_count: number;
}
export interface Score {
  name: string;
  title: string;
  description: string;
  version_meta_urls: SocreVersionMetaUrl[];
  versions: SummaryScoreVersion[];
}

export interface SocreVersionMetaUrl {
  version: number;
  url: string;
}

export interface NewScore {
  name: string;
  title: string | null;
  description: string | null;
}

export interface UserMe {
  name: string;
  email: string;
  id: string;
}

export interface UploadedContent {
  href: string;
  original_name: string;
}

export interface ScoreV2PropertyItem {
  title?: string;
  description?: string;
}
export interface ScoreV2VersionObject {
  create_at: Date;
  author: string;
  //--------------------------------------
  property: ScoreV2PropertyItem;
  pages: string[];
  parent?: string;
  message: string;
  comments: {
    [pageHash: string]: string[];
  };
}
export interface ScoreV2PageObject {
  create_at: Date;
  author: string;
  //--------------------------------------
  number: string;
  image: string;
  thumbnail: string;
}
export interface ScoreV2CommentObject {
  create_at: Date;
  author: string;
  //--------------------------------------
  comment: string;
}
export interface ScoreV2Latest {
  head_hash: string;
  head: ScoreV2VersionObject;
}
export interface ScoreV2LatestSet {
  [scoreName: string]: ScoreV2Latest;
}

export interface PatchScoreV2PropertyItem {
  title?: string;
  description?: string;
}

/** ページを挿入するコミット */
export interface InsertPageCommitObject {
  index: number;
  number?: string;
  image?: string;
  thumbnail?: string;
}

/** ページを追加するコミット */
export interface AddPageCommitObject {
  number?: string;
  image?: string;
  thumbnail?: string;
}

/** ページを更新するコミット */
export interface UpdatePageCommitObject {
  index: number;
  number?: string;
  image?: string;
  thumbnail?: string;
}

/** ページを削除するコミット */
export interface DeletePageCommitObject {
  index: number;
}

/** 楽譜のプロパティを更新するコミット */
export interface UpdatePropertyCommitObject {
  title?: string;
  description?: string;
}

/** コメントを挿入するコミット */
export interface InsertCommentCommitObject {
  page: string;
  index: number;
  comment?: string;
}

/** コメントを追加するコミット */
export interface AddCommentCommitObject {
  page: string;
  comment?: string;
}

/** コメントを更新するコミット */
export interface UpdateCommentCommitObject {
  page: string;
  index: number;
  comment?: string;
}

/** コメントを削除するコミット */
export interface DeleteCommentCommitObject {
  page: string;
  index: number;
}

export interface CommitObject {
  type:
    | "insert_page"
    | "add_page"
    | "update_page"
    | "delete_page"
    | "update_property"
    | "insert_comment"
    | "add_comment"
    | "update_comment"
    | "delete_comment";

  insert_page: InsertPageCommitObject;
  add_page: AddPageCommitObject;
  update_page: UpdatePageCommitObject;
  delete_page: DeletePageCommitObject;

  update_property: UpdatePropertyCommitObject;

  insert_comment: InsertCommentCommitObject;
  add_comment: AddCommentCommitObject;
  update_comment: UpdateCommentCommitObject;
  delete_comment: DeleteCommentCommitObject;
}
export interface CommitRequest {
  /** 変更する Version Object の Hash */
  parent: string;
  commits: CommitObject[];
}

/** バージョンのハッシュセット Key: version, Value: Hash */
export interface ScoreV2VersionSet {
  [version: string]: string;
}

/** バージョンのハッシュ */
export interface ScoreV2Version {
  hash: string;
  version: number;
}

export default class PracticeManagerApiClient {
  constructor(private baseUrl: string) {}

  async getUserMe(): Promise<UserMe> {
    const url = new URL("api/v1/user/me", this.baseUrl);

    try {
      const response = await fetch(url.href, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      const json = await response.json();

      return json;
    } catch (err) {
      throw err;
    }
  }

  async getVersion(): Promise<string> {
    const url = new URL("api/version", this.baseUrl);

    try {
      const response = await fetch(url.href, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      const json = await response.json();

      return json.version;
    } catch (err) {
      throw err;
    }
  }

  async getScoreVersion(name: string, version: number): Promise<ScoreVersion> {
    const url = new URL(
      `api/v1/score/${name}/version/${version.toString()}`,
      this.baseUrl
    );

    try {
      const response = await fetch(url.href, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      const json = await response.json();

      return json;
    } catch (err) {
      throw err;
    }
  }

  async createVersion(name: string, files: File[]): Promise<string> {
    const url = new URL(`api/v1/score/${name}/version`, this.baseUrl);

    const formData = new FormData();
    const nos: { [name: string]: number } = {};
    files.forEach((file, i) => {
      formData.append("Images", file);
      nos[file.name] = i;
    });

    formData.append("Nos", JSON.stringify(nos));

    try {
      const response = await fetch(url.href, {
        method: "POST",
        body: formData,
      });

      if (response.ok) {
        return "";
      }

      throw new Error(`Score 画像の登録に失敗しました(${response.text()})`);
    } catch (err) {
      throw err;
    }
  }

  async getScores(): Promise<Score[]> {
    const url = new URL(`api/v1/score`, this.baseUrl);
    try {
      const response = await fetch(url.href, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`Score の取得に失敗しました(${await response.text()})`);
      }
      const scores = (await response.json()) as Score[];

      return scores;
    } catch (err) {
      throw err;
    }
  }

  async getScore(scoreName: string): Promise<Score> {
    const url = new URL(`api/v1/score/${scoreName}`, this.baseUrl);
    try {
      const response = await fetch(url.href, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`Score の取得に失敗しました(${await response.text()})`);
      }
      const score = (await response.json()) as Score;

      return score;
    } catch (err) {
      throw err;
    }
  }

  async createScore(newScore: NewScore): Promise<void> {
    const url = new URL(`api/v1/score`, this.baseUrl);

    if (newScore.name === "") {
      throw new Error("Score の名前を入力してください");
    }
    if (!newScore.name.match(/^[A-Za-z0-9]+$/)) {
      throw new Error("Score の名前は半角英数字を入力してください");
    }
    try {
      const response = await fetch(url.href, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(newScore),
      });

      if (!response.ok) {
        throw new Error(`Score の取得に失敗しました(${await response.text()})`);
      }
    } catch (err) {
      throw err;
    }
  }

  async uploadContent(
    file: File,
    owner: string,
    scoreName: string
  ): Promise<UploadedContent> {
    const url = new URL(`api/v1/content/upload`, this.baseUrl);

    // Todo 引数の検証を追加する
    const formData = new FormData();
    formData.append("content", file);
    formData.append("owner", owner);
    formData.append("score_name", scoreName);

    try {
      const response = await fetch(url.href, {
        method: "POST",
        // headers: {
        //   "Content-Type": "multipart/form-data",
        //   Accept: "application/json",
        // },
        body: formData,
      });

      // throw new Error("test");

      if (response.ok) {
        const uploadedContent = (await response.json()) as UploadedContent;

        return uploadedContent;
      }

      throw new Error(
        `コンテンツのアップロードに失敗しました (${response.text()})`
      );
    } catch (err) {
      throw err;
    }
  }

  async deleteContent(cntentUrl: string): Promise<void> {
    // Todo 引数の検証

    const url = new URL(`api/v1/content/delete`, this.baseUrl);

    const requestUrl = queryString.stringifyUrl({
      url: url.toString(),
      query: {
        uri: cntentUrl,
      },
    });

    try {
      const response = await fetch(requestUrl, {
        method: "DELETE",
        // headers: {
        //   "Content-Type": "application/json",
        // },
      });

      if (!response.ok) {
        throw new Error(
          `コンテンツの削除に失敗しました (${await response.text()})`
        );
      }
    } catch (err) {
      throw err;
    }
  }

  /** 自分がアクセス可能な楽譜を取得する */
  async getScoreV2Set(): Promise<ScoreV2LatestSet> {
    const url = new URL(`api/v1/score_v2`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`スコアの取得に失敗しました`);
      }

      return (await response.json()) as ScoreV2LatestSet;
    } catch (err) {
      throw err;
    }
  }

  /** 指定した所有者の楽譜を取得する */
  async getScoreV2SetWithOwner(
    /** 所有者 */
    owner: string
  ): Promise<ScoreV2LatestSet> {
    assertArgumentUndefined(owner, "owner");

    const url = new URL(`api/v1/score_v2/${owner}`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`スコアの取得に失敗しました`);
      }

      return (await response.json()) as ScoreV2LatestSet;
    } catch (err) {
      throw err;
    }
  }

  /** 楽譜を取得する */
  async getScoreV2(
    /** 所有者 */
    owner: string,
    /** 楽譜名 */
    scoreName: string
  ): Promise<ScoreV2Latest> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");

    const url = new URL(`api/v1/score_v2/${owner}/${scoreName}`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`スコアの取得に失敗しました`);
      }

      return (await response.json()) as ScoreV2Latest;
    } catch (err) {
      throw err;
    }
  }

  /** 楽譜を作成する */
  async createScoreV2(
    /** 所有者 */
    owner: string,
    /** 楽譜名 */
    scoreName: string,
    property: ScoreV2PropertyItem
  ): Promise<ScoreV2Latest> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");
    assertArgumentUndefined(property, "property");

    const url = new URL(`api/v1/score_v2/${owner}/${scoreName}`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "POST",
        headers: postHeaders,
        body: JSON.stringify(property),
      });

      if (!response.ok) {
        throw new Error(`スコアの作成に失敗しました`);
      }

      return (await response.json()) as ScoreV2Latest;
    } catch (err) {
      throw err;
    }
  }

  /** 指定した楽譜を削除する */
  async deleteScoreV2(
    /** 所有者 */
    owner: string,
    /** 楽譜名 */
    scoreName: string
  ): Promise<void> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");

    const url = new URL(`api/v1/score_v2/${owner}/${scoreName}`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "DELETE",
      });

      if (!response.ok) {
        throw new Error(`スコアの削除に失敗しました`);
      }
    } catch (err) {
      throw err;
    }
  }

  /** 楽譜のプロパティを更新する */
  async updateScoreV2Property(
    /** 所有者 */
    owner: string,
    /** 楽譜名 */
    scoreName: string,
    /** 更新するプロパティ 更新しないものは undefined にする */
    propery: PatchScoreV2PropertyItem
  ): Promise<ScoreV2Latest> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");
    assertArgumentUndefined(propery, "property");

    const url = new URL(`api/v1/score_v2/${owner}/${scoreName}`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "PATCH",
        headers: patchHeaders,
        body: JSON.stringify(propery),
      });

      if (!response.ok) {
        throw new Error(`スコアのプロパティの更新に失敗しました`);
      }
      return (await response.json()) as ScoreV2Latest;
    } catch (err) {
      throw err;
    }
  }

  /** Hash を指定して Object を取得する */
  async getHashObjects(
    /** 所有者 */
    owner: string,
    /** 楽譜名 */
    scoreName: string,
    /** 取得する Hash のリスト */
    hash: string[]
  ): Promise<{ [hash: string]: any }> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");
    assertArgumentUndefined(hash, "hash");

    if (0 === hash.length) {
      throw new Error(`'hash' is empty`);
    }
    if (100 < hash.length) {
      throw new Error(`1 <= 'hash.length' <= 100 (${hash.length})`);
    }

    const url = new URL(`api/v1/score_v2/${owner}/${scoreName}`, this.baseUrl);

    const requestUrl = queryString.stringifyUrl(
      {
        url: url.toString(),
        query: {
          hash: hash,
        },
      },
      {
        arrayFormat: "comma",
      }
    );
    try {
      const response = await fetch(requestUrl, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`オブジェクトの取得に失敗しました`);
      }
      return (await response.json()) as { [hash: string]: any };
    } catch (err) {
      throw err;
    }
  }

  /** 楽譜のデータを更新する */
  async commit(
    /** 所有者 */
    owner: string,
    /** 楽譜名 */
    scoreName: string,
    /** 更新するプロパティ 更新しないものは undefined にする */
    commits: CommitRequest
  ): Promise<ScoreV2Latest> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");
    assertArgumentUndefined(commits, "commits");

    if (!commits.parent) {
      throw new Error(`'parent' を指定してください`);
    }

    const url = new URL(`api/v1/score_v2/${owner}/${scoreName}`, this.baseUrl);

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "PATCH",
        headers: patchHeaders,
        body: JSON.stringify(commits),
      });

      if (!response.ok) {
        throw new Error(`スコアのプロパティの更新に失敗しました`);
      }
      return (await response.json()) as ScoreV2Latest;
    } catch (err) {
      throw err;
    }
  }

  async getScoreV2Versions(
    owner: string,
    scoreName: string
  ): Promise<ScoreV2VersionSet> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");

    const url = new URL(
      `api/v1/score_v2/${owner}/${scoreName}/version`,
      this.baseUrl
    );

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "GET",
      });

      if (!response.ok) {
        throw new Error(`スコアのバージョン取得に失敗しました`);
      }
      return (await response.json()) as ScoreV2VersionSet;
    } catch (err) {
      throw err;
    }
  }

  async createScoreV2Version(
    owner: string,
    scoreName: string
  ): Promise<ScoreV2Version> {
    assertArgumentUndefined(owner, "owner");
    assertArgumentUndefined(scoreName, "scoreName");

    const url = new URL(
      `api/v1/score_v2/${owner}/${scoreName}/version`,
      this.baseUrl
    );

    const requestUrl = url.toString();
    try {
      const response = await fetch(requestUrl, {
        method: "POST",
        headers: postHeaders,
      });

      if (!response.ok) {
        throw new Error(`スコアのバージョンを作成に失敗しました`);
      }
      return (await response.json()) as ScoreV2Version;
    } catch (err) {
      throw err;
    }
  }
}
