﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi.Core.Parameters;
using Tweetinvi.Core.Web;
using Tweetinvi.Models;
using Tweetinvi.Models.DTO;
using Tweetinvi.Parameters;

namespace Tweetinvi.Controllers.Timeline
{
    public interface ITimelineQueryExecutor
    {
        // Home Timeline
        Task<ITwitterResult<ITweetDTO[]>> GetHomeTimeline(IGetHomeTimelineParameters timelineParameters, ITwitterRequest request);

        // User Timeline
        Task<IEnumerable<ITweetDTO>> GetUserTimeline(IUserTimelineQueryParameters timelineParameters);

        // Mention Timeline
        Task<IEnumerable<ITweetDTO>> GetMentionsTimeline(IMentionsTimelineParameters timelineParameters);

        // Retweets Of Me Timeline
        Task<ITwitterResult<ITweetDTO[]>> GetRetweetsOfMeTimeline(IGetRetweetsOfMeTimelineParameters timelineParameters, ITwitterRequest request);
    }

    public class TimelineQueryExecutor : ITimelineQueryExecutor
    {
        private readonly ITwitterAccessor _twitterAccessor;
        private readonly ITimelineQueryGenerator _timelineQueryGenerator;

        public TimelineQueryExecutor(
            ITwitterAccessor twitterAccessor,
            ITimelineQueryGenerator timelineQueryGenerator)
        {
            _twitterAccessor = twitterAccessor;
            _timelineQueryGenerator = timelineQueryGenerator;
        }

        // Home Timeline
        public Task<ITwitterResult<ITweetDTO[]>> GetHomeTimeline(IGetHomeTimelineParameters timelineParameters, ITwitterRequest request)
        {
            var query = _timelineQueryGenerator.GetHomeTimelineQuery(timelineParameters, request.ExecutionContext.TweetMode);
            request.Query.Url = query;
            request.Query.HttpMethod = HttpMethod.GET;
            return _twitterAccessor.ExecuteRequest<ITweetDTO[]>(request);
        }

        // User Timeline
        public Task<IEnumerable<ITweetDTO>> GetUserTimeline(IUserTimelineQueryParameters timelineParameters)
        {
            string query = _timelineQueryGenerator.GetUserTimelineQuery(timelineParameters);
            return _twitterAccessor.ExecuteGETQuery<IEnumerable<ITweetDTO>>(query);
        }

        // Mention Timeline
        public Task<IEnumerable<ITweetDTO>> GetMentionsTimeline(IMentionsTimelineParameters timelineParameters)
        {
            string query = _timelineQueryGenerator.GetMentionsTimelineQuery(timelineParameters);
            return _twitterAccessor.ExecuteGETQuery<IEnumerable<ITweetDTO>>(query);
        }

        // Retweets of Me Timeline
        public Task<ITwitterResult<ITweetDTO[]>> GetRetweetsOfMeTimeline(IGetRetweetsOfMeTimelineParameters timelineParameters, ITwitterRequest request)
        {
            var query = _timelineQueryGenerator.GetRetweetsOfMeTimelineQuery(timelineParameters, request.ExecutionContext.TweetMode);
            request.Query.Url = query;
            request.Query.HttpMethod = HttpMethod.GET;
            return _twitterAccessor.ExecuteRequest<ITweetDTO[]>(request);
        }
    }
}