
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
}
