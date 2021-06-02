import { ScorePage } from "./ScoreClientV2";

/** 楽譜のアイテムの URL のジェネレーター */
export default class ScoreItemUrlGen {
  constructor(private baseUrl: string) {
    let i = baseUrl.length;
    for (; 1 <= i; --i) {
      if (baseUrl[i - 1] === "/") {
        continue;
      }
      break;
    }

    if (baseUrl.length !== i) {
      this.baseUrl = baseUrl.substr(0, i);
    }
  }

  /** ページ画像の URL を取得する */
  getImageUrl(ownerId: string, scoreId: string, page: ScorePage): string {
    var url = new URL(
      `${ownerId}/${scoreId}/${page.id}/${page.objectName}`,
      this.baseUrl
    );
    return url.href;
  }

  /** サムネイルの URL を取得する */
  getThumbnailImageUrl(
    ownerId: string,
    scoreId: string,
    page: ScorePage
  ): string {
    var url = new URL(
      `${ownerId}/${scoreId}/${page.id}/thumbnail.jpg`,
      this.baseUrl
    );
    return url.href;
  }
}
