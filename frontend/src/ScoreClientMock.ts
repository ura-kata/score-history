import HashObjectStore from "./HashObjectStore";
import PracticeManagerApiClient from "./PracticeManagerApiClient";
import ScoreClient, {
  CommentOperation,
  NewScoreData,
  PageOperation,
  ScoreComment,
  ScoreData,
  ScorePage,
  ScoreProperty,
  ScoreSummary,
  ScoreSummarySet,
} from "./ScoreClient";

/** サーバーのスコアにアクセスするためのクライアント */
export default class ScoreClientMock {
  private _socreClient: ScoreClient;

  /** コンストラクタ */
  constructor(
    private apiClient: PracticeManagerApiClient,
    private objectStore: HashObjectStore
  ) {
    this._socreClient = new ScoreClient(apiClient, objectStore);
  }

  /** スコアのサマリー一覧を取得する */
  async getScores(): Promise<ScoreSummarySet> {
    return await this._socreClient.getScores();
  }
  async getScore(owner: string, scoreName: string): Promise<ScoreData> {
    return await this._socreClient.getScore(owner, scoreName);
  }

  async createScore(newSocreData: NewScoreData): Promise<ScoreSummary> {
    return await this._socreClient.createScore(newSocreData);
  }

  async getVersions(owner: string, scoreName: string): Promise<string[]> {
    return await this._socreClient.getVersions(owner, scoreName);
  }

  async createVersions(owner: string, scoreName: string): Promise<string[]> {
    return await this._socreClient.createVersions(owner, scoreName);
  }

  async getPages(
    owner: string,
    scoreName: string,
    version: string
  ): Promise<ScorePage[]> {
    return await this._socreClient.getPages(owner, scoreName, version);
  }

  async updateProperty(
    owner: string,
    scoreName: string,
    oldProperty: ScoreProperty,
    newProperty: ScoreProperty
  ): Promise<void> {
    await this._socreClient.updateProperty(
      owner,
      scoreName,
      oldProperty,
      newProperty
    );
  }

  async updatePages(
    owner: string,
    scoreName: string,
    pageOperations: PageOperation[]
  ): Promise<void> {
    await this._socreClient.updatePages(owner, scoreName, pageOperations);
  }

  async getComments(
    owner: string,
    scoreName: string,
    version: string,
    pageIndex: number
  ): Promise<ScoreComment[]> {
    return new Promise<ScoreComment[]>((resolve) => {
      resolve(
        [...Array(50)].map((item, index) => ({
          comment: `${owner}: ${scoreName}: ${version}: ${pageIndex}: ${index} テスト用のコメント1\n改行したコメント\nなどなど`,
        }))
      );
    });
  }

  async updateComments(
    owner: string,
    scoreName: string,
    pageIndex: number,
    commentOperations: CommentOperation[]
  ): Promise<void> {
    await this._socreClient.updateComments(
      owner,
      scoreName,
      pageIndex,
      commentOperations
    );
  }
}
