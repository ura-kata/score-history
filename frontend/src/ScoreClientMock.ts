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

  async getPages(owner: string, scoreName: string): Promise<ScorePage[]> {
    return await this._socreClient.getPages(owner, scoreName);
  }

  async updateProperty(
    owner: string,
    scoreName: string,
    oldProperty: ScoreProperty,
    newProperty: ScoreProperty
  ): Promise<ScoreData> {
    return await this._socreClient.updateProperty(
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
  ): Promise<ScoreData> {
    return await this._socreClient.updatePages(
      owner,
      scoreName,
      pageOperations
    );
  }

  async getComments(
    owner: string,
    scoreName: string,
    pageIndex: number
  ): Promise<ScoreComment[]> {
    return this._socreClient.getComments(owner, scoreName, pageIndex);
  }

  async updateComments(
    owner: string,
    scoreName: string,
    pageIndex: number,
    commentOperations: CommentOperation[]
  ): Promise<ScoreData> {
    return await this._socreClient.updateComments(
      owner,
      scoreName,
      pageIndex,
      commentOperations
    );
  }
}
