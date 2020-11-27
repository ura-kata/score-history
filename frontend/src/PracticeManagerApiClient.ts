
export interface ScoreVersionPage{
  url: string;
  no: number;
}
export interface ScoreVersion{
  version: number;
  pages: ScoreVersionPage[];

}

export default class PracticeManagerApiClient{
  constructor(private baseUrl: string){

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

  async getScoreVersion(): Promise<ScoreVersion> {
    const url = new URL(`api/v1/score/${'test'}/version/${'0'}`, this.baseUrl);

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

  async postScoreImage(files: File[]): Promise<string[]> {
    const url = new URL(`api/v1/score/${'test'}/version/${'0'}`, this.baseUrl);

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

      if(response.status === 200){
        return [];
      }
      const json = await response.json();

      return json.upload_error_file_list;
    } catch(err){
      throw err;
    }
  }
}
