using Amazon.DynamoDBv2.Model;
using Db.V1.Models;

namespace Db.V1
{
    public interface IConverter
    {
        ScoreSummaryDb ConvertToScoreSummaryDb(AttributeValue value);
        ScoreMainDb ConvertToScoreMainDb(AttributeValue value);
        AnnotationDataDb ConvertToAnnotationDataDb(AttributeValue value);
        ItemSummaryDb ConvertToItemSummaryDb(AttributeValue value);
        ItemMainDb ConvertToItemMainDb(AttributeValue value);

        AttributeValue ConvertFrom(ScoreSummaryDb model);
        AttributeValue ConvertFrom(ScoreMainDb model);
        AttributeValue ConvertFrom(AnnotationDataDb model);
        AttributeValue ConvertFrom(ItemSummaryDb model);
        AttributeValue ConvertFrom(ItemMainDb model);
    }

    public class Converter:IConverter
    {
        public ScoreSummaryDb ConvertToScoreSummaryDb(AttributeValue value)
        {
            throw new System.NotImplementedException();
        }

        public ScoreMainDb ConvertToScoreMainDb(AttributeValue value)
        {
            throw new System.NotImplementedException();
        }

        public AnnotationDataDb ConvertToAnnotationDataDb(AttributeValue value)
        {
            throw new System.NotImplementedException();
        }

        public ItemSummaryDb ConvertToItemSummaryDb(AttributeValue value)
        {
            throw new System.NotImplementedException();
        }

        public ItemMainDb ConvertToItemMainDb(AttributeValue value)
        {
            throw new System.NotImplementedException();
        }

        public AttributeValue ConvertFrom(ScoreSummaryDb model)
        {
            throw new System.NotImplementedException();
        }

        public AttributeValue ConvertFrom(ScoreMainDb model)
        {
            throw new System.NotImplementedException();
        }

        public AttributeValue ConvertFrom(AnnotationDataDb model)
        {
            throw new System.NotImplementedException();
        }

        public AttributeValue ConvertFrom(ItemSummaryDb model)
        {
            throw new System.NotImplementedException();
        }

        public AttributeValue ConvertFrom(ItemMainDb model)
        {
            throw new System.NotImplementedException();
        }
    }
}
