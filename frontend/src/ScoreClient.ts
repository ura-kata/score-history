import HashObjectStore from "./HashObjectStore";
import PracticeManagerApiClient, {
  CommitObject,
  CommitRequest,
  ScoreV2CommentObject,
  ScoreV2Latest,
  ScoreV2LatestSet,
  ScoreV2VersionSet,
  UpdatePropertyRequest,
} from "./PracticeManagerApiClient";

/** 新しいスコア */
export interface NewScoreData {
  owner: string;
  scoreName: string;
  property: ScoreProperty;
}

export interface ScoreProperty {
  title?: string;
  description?: string;
}

export interface ScoreSummary {
  owner: string;
  scoreName: string;
  property: ScoreProperty;
}

export interface ScoreData {
  scoreSummary: ScoreSummary;
  versions: string[];
  pages: ScorePage[];
}

export interface ScoreSummarySet {
  [ownerAndScoreName: string]: ScoreSummary;
}

export interface ScorePage {
  number: string;
  image: string;
  thumbnail: string;
}

export interface ScoreComment {
  comment: string;
}

export type PageOperationType = "add" | "insert" | "remove" | "update";
export interface PageOperation {
  type: PageOperationType;
  image?: string;
  thumbnail?: string;
  number?: string;
  index?: number;
}

export type CommentOperationType = "add" | "insert" | "remove" | "update";
export interface CommentOperation {
  type: CommentOperationType;
  comment?: string;
  index?: number;
}

/** サーバーのスコアにアクセスするためのクライアント */
export default class ScoreClient {
  scoreSet: ScoreV2LatestSet = {};
  versionSetCollection: { [ownerAndScoreName: string]: ScoreV2VersionSet } = {};

  /** コンストラクタ */
  constructor(
    private apiClient: PracticeManagerApiClient,
    private objectStore: HashObjectStore
  ) {}

  /** スコアのサマリー一覧を取得する */
  async getScores(): Promise<ScoreSummarySet> {
    try {
      var scoreSet = await this.apiClient.getScoreV2Set();

      const reuslt: ScoreSummarySet = {};
      Object.entries(scoreSet).forEach(([key, value]) => {
        const ownerAndScoreName = key.split("/");
        const owner = ownerAndScoreName[0];
        const scoreName = ownerAndScoreName[1];
        this.objectStore.setObjectToLocal(
          owner,
          scoreName,
          value.head_hash,
          value.head
        );
        const property = value.property;
        reuslt[key] = {
          owner: owner,
          scoreName: scoreName,
          property: {
            title: property.title ?? undefined,
            description: property.description ?? undefined,
          },
        };
      });

      this.scoreSet = scoreSet;

      return reuslt;
    } catch (err) {
      console.log(err);
      throw new Error(`楽譜を取得することができませんでした`);
    }
  }
  async getScore(owner: string, scoreName: string): Promise<ScoreData> {
    try {
      const response = await this.apiClient.getScoreV2(owner, scoreName);

      this.objectStore.setObjectToLocal(
        owner,
        scoreName,
        response.head_hash,
        response.head
      );

      this.scoreSet[`${owner}/${scoreName}`] = response;

      const versionsResponse = await this.apiClient.getScoreV2Versions(
        owner,
        scoreName
      );

      this.versionSetCollection[`${owner}/${scoreName}`] = versionsResponse;

      const versions = Object.entries(versionsResponse).map(([key, _]) => key);

      const pagesResponse = await this.objectStore.getPageObjects(
        owner,
        scoreName,
        response.head.pages
      );

      const pages = response.head.pages
        .map((hash) => pagesResponse[hash])
        .map(
          (page) =>
            ({
              image: page.image,
              thumbnail: page.thumbnail,
              number: page.number,
            } as ScorePage)
        );

      return {
        scoreSummary: {
          owner: owner,
          scoreName: scoreName,
          property: {
            title: response.property.title,
            description: response.property.description,
          },
        },
        versions: versions,
        pages: pages,
      };
    } catch (err) {
      console.log(err);
      throw new Error(`スコアの取得に失敗しました`);
    }
  }

  async createScore(newSocreData: NewScoreData): Promise<ScoreSummary> {
    const owner = newSocreData.owner;
    const scoreName = newSocreData.scoreName;

    let newSocre: ScoreV2Latest;
    try {
      newSocre = await this.apiClient.createScoreV2(
        owner,
        scoreName,
        newSocreData.property
      );
    } catch (err) {
      throw new Error(`楽譜の作成に失敗しました`);
    }

    this.objectStore.setObjectToLocal(
      owner,
      scoreName,
      newSocre.head_hash,
      newSocre.head
    );

    const result: ScoreSummary = {
      owner: owner,
      scoreName: scoreName,
      property: {
        title: newSocre?.property.title,
        description: newSocre?.property.description,
      },
    };
    return result;
  }

  async getVersions(owner: string, scoreName: string): Promise<string[]> {
    try {
      const versionSet = await this.apiClient.getScoreV2Versions(
        owner,
        scoreName
      );

      this.versionSetCollection[`${owner}/${scoreName}`] = versionSet;

      const versions = Object.entries(versionSet).map(([key, _]) => key);
      return versions;
    } catch (err) {
      throw new Error(`楽譜のバージョンの取得に失敗しました`);
    }
  }

  async createVersions(owner: string, scoreName: string): Promise<string[]> {
    try {
      const version = await this.apiClient.createScoreV2Version(
        owner,
        scoreName
      );

      let versionSet = this.versionSetCollection[`${owner}/${scoreName}`];
      if (!versionSet) {
        await this.getVersions(owner, scoreName);
        versionSet = this.versionSetCollection[`${owner}/${scoreName}`];
      }

      versionSet[version.version.toString()] = version.hash;

      const versions = Object.entries(versionSet).map(([key, _]) => key);
      return versions;
    } catch (err) {
      throw new Error(`楽譜のバージョンの取得に失敗しました`);
    }
  }

  async getPages(owner: string, scoreName: string): Promise<ScorePage[]> {
    const ownerAndScoreName = `${owner}/${scoreName}`;
    const score = this.scoreSet[ownerAndScoreName];

    try {
      const pageSet = await this.objectStore.getPageObjects(
        owner,
        scoreName,
        score.head.pages
      );

      return score.head.pages
        .map((h) => pageSet[h])
        .map((p) => ({
          image: p.image,
          thumbnail: p.thumbnail,
          number: p.number,
        }));
    } catch (err) {
      throw new Error("ページの取得に失敗しました");
    }
  }

  async updateProperty(
    owner: string,
    scoreName: string,
    oldProperty: ScoreProperty,
    newProperty: ScoreProperty
  ): Promise<void> {
    if (
      oldProperty.title === newProperty.title &&
      oldProperty.description === newProperty.description
    ) {
      throw new Error(`プロパティの変更がありません`);
    }
    const ownerAndScoreName = `${owner}/${scoreName}`;
    const score = this.scoreSet[ownerAndScoreName];

    const request: UpdatePropertyRequest = {
      parent: score.property_hash,
      property: {
        title:
          oldProperty.title !== newProperty.title
            ? newProperty.title
            : undefined,
        description:
          oldProperty.description !== newProperty.description
            ? newProperty.description
            : undefined,
      },
    };

    try {
      await this.apiClient.updateScoreV2Property(owner, scoreName, request);
    } catch (err) {
      console.log(err);
      throw new Error(
        `変更元のデータが古いです。更新してから再度実行してください。`
      );
    }
  }

  async updatePages(
    owner: string,
    scoreName: string,
    pageOperations: PageOperation[]
  ): Promise<void> {
    if (!(0 < pageOperations.length)) {
      throw new Error(`更新操作がありません`);
    }
    const ownerAndScoreName = `${owner}/${scoreName}`;
    const score = this.scoreSet[ownerAndScoreName];

    const commits: CommitObject[] = [];

    pageOperations.forEach((ope, index) => {
      switch (ope.type) {
        case "add": {
          const newCommit: CommitObject = {
            type: "add_page",
            add_page: {
              image: ope.image,
              thumbnail: ope.thumbnail,
              number: ope.number,
            },
          };
          commits.push(newCommit);
          break;
        }
        case "insert": {
          if (ope.index === undefined) {
            throw new Error(`index is undefined.`);
          }
          const newCommit: CommitObject = {
            type: "insert_page",
            insert_page: {
              index: ope.index,
              image: ope.image,
              thumbnail: ope.thumbnail,
              number: ope.number,
            },
          };
          commits.push(newCommit);
          break;
        }
        case "remove": {
          if (ope.index === undefined) {
            throw new Error(`index is undefined.`);
          }
          const newCommit: CommitObject = {
            type: "delete_page",
            delete_page: {
              index: ope.index,
            },
          };
          commits.push(newCommit);
          break;
        }
        case "update": {
          break;
        }
      }
    });

    const commitRequest: CommitRequest = {
      parent: score.head_hash,
      commits: commits,
    };
    try {
      await this.apiClient.commit(owner, scoreName, commitRequest);

      await this.createVersions(owner, scoreName);
    } catch (err) {
      console.log(err);
      throw new Error(
        `変更元のデータが古いです。更新してから再度実行してください。`
      );
    }
  }

  async getComments(
    owner: string,
    scoreName: string,
    pageIndex: number
  ): Promise<ScoreComment[]> {
    const ownerAndScoreName = `${owner}/${scoreName}`;
    const score = this.scoreSet[ownerAndScoreName];
    try {
      const pageHash = score.head.pages[pageIndex];
      if (!pageHash) return [];
      const commentHashList = score.head.comments[pageHash];
      if (!commentHashList || commentHashList.length === 0) {
        return [];
      }

      const commentSet = await this.objectStore.getCommentObjects(
        owner,
        scoreName,
        commentHashList
      );

      return commentHashList
        .map((commentHash) => {
          return commentSet[commentHash];
        })
        .map((comment) => ({ comment: comment.comment }));
    } catch (err) {
      throw new Error("コメントの取得に失敗しました");
    }
  }

  async updateComments(
    owner: string,
    scoreName: string,
    pageIndex: number,
    commentOperations: CommentOperation[]
  ): Promise<void> {
    if (!(0 < commentOperations.length)) {
      throw new Error(`更新操作がありません`);
    }
    const ownerAndScoreName = `${owner}/${scoreName}`;
    const score = this.scoreSet[ownerAndScoreName];

    const pagehash = score.head.pages[pageIndex];

    if (!pagehash) {
      throw new Error(`ページが存在しません`);
    }
    const commits: CommitObject[] = [];

    commentOperations.forEach((ope, index) => {
      switch (ope.type) {
        case "add": {
          const newCommit: CommitObject = {
            type: "add_comment",
            add_comment: {
              page: pagehash,
              comment: ope.comment,
            },
          };
          commits.push(newCommit);
          break;
        }
        case "insert": {
          if (ope.index === undefined) {
            throw new Error(`index is undefined.`);
          }
          const newCommit: CommitObject = {
            type: "insert_comment",
            insert_comment: {
              page: pagehash,
              index: ope.index,
              comment: ope.comment,
            },
          };
          commits.push(newCommit);
          break;
        }
        case "remove": {
          if (ope.index === undefined) {
            throw new Error(`index is undefined.`);
          }
          const newCommit: CommitObject = {
            type: "delete_comment",
            delete_comment: {
              page: pagehash,
              index: ope.index,
            },
          };
          commits.push(newCommit);
          break;
        }
        case "update": {
          break;
        }
      }
    });

    const commitRequest: CommitRequest = {
      parent: score.head_hash,
      commits: commits,
    };
    try {
      await this.apiClient.commit(owner, scoreName, commitRequest);

      await this.createVersions(owner, scoreName);
    } catch (err) {
      console.log(err);
      throw new Error(
        `変更元のデータが古いです。更新してから再度実行してください。`
      );
    }
  }
}
