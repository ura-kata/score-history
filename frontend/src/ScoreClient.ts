import HashObjectStore from "./HashObjectStore";
import PracticeManagerApiClient, {
  AddPageCommitObject,
  CommitObject,
  CommitRequest,
  ScoreV2Latest,
  ScoreV2LatestSet,
  ScoreV2PropertyItem,
  ScoreV2VersionObject,
  ScoreV2VersionSet,
} from "./PracticeManagerApiClient";

/** 新しいスコア */
export interface NewScoreData {
  owner: string;
  scoreName: string;
  property: ScoreV2PropertyItem;
  pages: AddPageCommitObject[];
}

/** サーバーのスコアにアクセスするためのクライアント */
export default class ScoreClient {
  /** コンストラクタ */
  constructor(
    private apiClient: PracticeManagerApiClient,
    private objectStore: HashObjectStore
  ) {}

  /** スコアのサマリー一覧を取得する */
  async getScores(): Promise<ScoreV2LatestSet> {
    try {
      var scoreSet = await this.apiClient.getScoreV2Set();

      Object.entries(scoreSet).forEach(([key, value]) => {
        const ownerAndScoreName = key.split("/");
        const owner = ownerAndScoreName[0];
        const scoreName = ownerAndScoreName[1];
        this.objectStore.setObjectToLocal(
          owner,
          scoreName,
          value.head_hash,
          JSON.stringify(value.head)
        );
      });
      return scoreSet;
    } catch (err) {
      console.log(err);
      throw new Error(`楽譜を取得することができませんでした`);
    }
  }

  async createScore(newSocreData: NewScoreData): Promise<ScoreV2Latest> {
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
      throw new Error();
    }

    if (newSocreData.pages.length <= 0) {
      this.objectStore.setObjectToLocal(
        owner,
        scoreName,
        newSocre.head_hash,
        JSON.stringify(newSocre.head)
      );
      return newSocre;
    }
    try {
      const commitRequest: CommitRequest = {
        parent: newSocre.head_hash,
        commits: newSocreData.pages.map(
          (p) =>
            ({
              type: "add_page",
              add_page: p,
            } as CommitObject)
        ),
      };

      const newScoreNext = await this.apiClient.commit(
        owner,
        scoreName,
        commitRequest
      );

      this.objectStore.setObjectToLocal(
        owner,
        scoreName,
        newScoreNext.head_hash,
        JSON.stringify(newScoreNext.head)
      );

      return newScoreNext;
    } catch (err) {
      console.log(err);
      throw new Error(`楽譜の作成に失敗しました`);
    }
  }

  async getVersions(
    owner: string,
    scoreName: string
  ): Promise<ScoreV2VersionSet> {
    const versionSet = await this.apiClient.getScoreV2Versions(
      owner,
      scoreName
    );

    return versionSet;
  }

  async getVersion(
    owner: string,
    scoreName: string,
    hash: string
  ): Promise<ScoreV2VersionObject> {
    const versionObject = await this.objectStore.getVersionObjects(
      owner,
      scoreName,
      [hash]
    );

    return versionObject[hash];
  }
}
