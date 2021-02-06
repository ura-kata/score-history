namespace PracticeManagerApi.Services.Providers
{
    public interface IScoreProvider
    {
        string UserName { get; }
        void CreateScore(string owner, string scoreName, NewScoreV2Property property);

        void UpdateProperty(string owner, string scoreName, string parentPropertyHash, PatchScoreV2Property property);

        void DeleteScore(string owner, string scoreName);

        void InsertPages(string owner, string scoreName, string parentVersionHash, int index, NewScoreV2Page[] pages);
        void DeletePages(string owner, string scoreName, string parentVersionHash, string[] hashList);
        void UpdatePages(string owner, string scoreName, string parentVersionHash, PatchScoreV2Page[] pages);

        void AddComments(string owner, string scoreName, string parentVersionHash, NewScoreV2Comment[] comments);
        void DeleteComments(string owner, string scoreName, string parentVersionHash,
            string targetPage, string[] hashList);
        void UpdateComments(string owner, string scoreName, string parentVersionHash, PatchScoreV2Comment[] comments);

        ScoreV2ObjectSet GetObjects(string owner, string scoreName, string[] hashList);

    }
}
