import queryString from "query-string";

export interface ScoreVersionPage{
  image_url: string;
  thumbnail_url: string;
  no: number;
  description: string;
}
export interface ScoreVersion{
  version: number;
  description: string;
  create_at: Date;
  update_at: Date;
  pages: ScoreVersionPage[];

}

export interface SummaryScoreVersion{
  version: number;
  description: string;
  page_count: number;
}
export interface Score{
  name: string;
  title: string;
  description: string;
  version_meta_urls: SocreVersionMetaUrl[];
  versions: SummaryScoreVersion[];
}

export interface SocreVersionMetaUrl{
  version: number;
  url: string;
}

export interface NewScore{
  name: string;
  title: string | null;
  description: string | null;
}

export interface UserMe{
  name: string;
  email: string;
  id: string;
}

export interface UploadedContent {
  href: string;
  original_name: string;
  }

export default class PracticeManagerApiClient {
  constructor(private baseUrl: string) {}

  async getUserMe(): Promise<UserMe> {
    const url = new URL('api/v1/user/me', this.baseUrl);

    try{
      const response = await fetch(url.href, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const json = await response.json();

      return json;
    } catch(err){
      throw err;
    }
  }

  async getVersion(): Promise<string> {
    const url = new URL('api/version', this.baseUrl);

    try{
      const response = await fetch(url.href, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const json = await response.json();

      return json.version;
    } catch(err){
      throw err;
    }
  }

  async getScoreVersion(name: string, version: number): Promise<ScoreVersion> {
    const url = new URL(`api/v1/score/${name}/version/${version.toString()}`, this.baseUrl);

    try{
      const response = await fetch(url.href, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      const json = await response.json();

      return json;
    } catch(err){
      throw err;
    }
  }

  async createVersion(name: string, files: File[]): Promise<string> {
    const url = new URL(`api/v1/score/${name}/version`, this.baseUrl);

    const formData = new FormData();
    const nos: {[name: string]: number} = {};
    files.forEach((file, i)=>{
      formData.append('Images', file);
      nos[file.name] = i;
    })

    formData.append('Nos', JSON.stringify(nos));

    try{
      const response = await fetch(url.href, {
        method: 'POST',
        body: formData,
      });

      if(response.ok){
        return "";
      }

      throw new Error(`Score 画像の登録に失敗しました(${response.text()})`);

    } catch(err){
      throw err;
    }
  }

  async getScores(): Promise<Score[]> {
    const url = new URL(`api/v1/score`, this.baseUrl);
    try{
      const response = await fetch(url.href, {
        method: 'GET',
      });

      if(!response.ok){
        throw new Error(`Score の取得に失敗しました(${await response.text()})`);
      }
      const scores = await response.json() as Score[];

      return scores;

    } catch(err){
      throw err;
    }
  }

  async getScore(scoreName: string): Promise<Score> {
    const url = new URL(`api/v1/score/${scoreName}`, this.baseUrl);
    try{
      const response = await fetch(url.href, {
        method: 'GET',
      });

      if(!response.ok){
        throw new Error(`Score の取得に失敗しました(${await response.text()})`);
      }
      const score = await response.json() as Score;

      return score;

    } catch(err){
      throw err;
    }
  }

  async createScore(newScore: NewScore): Promise<void> {
    const url = new URL(`api/v1/score`, this.baseUrl);

    if(newScore.name === ""){
      throw new Error('Score の名前を入力してください')
    }
    if(!newScore.name.match(/^[A-Za-z0-9]+$/)){
      throw new Error('Score の名前は半角英数字を入力してください')
    }
    try{
      const response = await fetch(url.href, {
        method: 'POST',
        headers:{
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(newScore),
      });

      if(!response.ok){
        throw new Error(`Score の取得に失敗しました(${await response.text()})`);
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
}
