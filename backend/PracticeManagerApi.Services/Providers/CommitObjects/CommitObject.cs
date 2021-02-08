using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class CommitObject
    {
        [JsonPropertyName("type")]
        public string CommitType { get; set; }


        [JsonPropertyName(InsertPageCommitObject.CommitType)]
        public InsertPageCommitObject InsertPage { get; set; }

        [JsonPropertyName(AddPageCommitObject.CommitType)]
        public AddPageCommitObject AddPage { get; set; }

        [JsonPropertyName(UpdatePageCommitObject.CommitType)]
        public UpdatePageCommitObject UpdatePage { get; set; }

        [JsonPropertyName(DeletePageCommitObject.CommitType)]
        public DeletePageCommitObject DeletePage { get; set; }



        [JsonPropertyName(UpdatePropertyCommitObject.CommitType)]
        public UpdatePropertyCommitObject UpdateProperty { get; set; }



        [JsonPropertyName(InsertCommentCommitObject.CommitType)]
        public InsertCommentCommitObject InsertComment { get; set; }

        [JsonPropertyName(AddCommentCommitObject.CommitType)]
        public AddCommentCommitObject AddComment { get; set; }

        [JsonPropertyName(UpdateCommentCommitObject.CommitType)]
        public UpdateCommentCommitObject UpdateComment{ get; set; }

        [JsonPropertyName(DeleteCommentCommitObject.CommitType)]
        public DeleteCommentCommitObject DeleteComment { get; set; }
    }
}
