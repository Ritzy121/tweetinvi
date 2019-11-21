using System;
using System.Threading.Tasks;
using FakeItEasy;
using Tweetinvi.Core.Iterators;
using Tweetinvi.Core.Web;
using Tweetinvi.Exceptions;
using Xunit;

namespace xUnitinvi.Cursor
{
    public class TwitterPageIteratorTests
    {
        public TwitterPageIteratorTests()
        {
            _firstPage = new TwitterResult<long>(null)
            {
                DataTransferObject = 42
            };
            
            _secondPage =  new TwitterResult<long>(null)
            {
                DataTransferObject = 43
            };

            _getNextPage = A.Fake<Func<string, Task<ITwitterResult<long>>>>();
            _extractNextCursor = A.Fake<Func<ITwitterResult<long>, string>>();
            _isLastPage = A.Fake<Func<ITwitterResult<long>, bool>>();

            A.CallTo(() => _getNextPage(null)).Returns(_firstPage);

            A.CallTo(() => _extractNextCursor(_firstPage)).Returns("cursor_for_page_2");
            A.CallTo(() => _extractNextCursor(_secondPage)).Returns("cursor_for_page_3");

            A.CallTo(() => _isLastPage(_firstPage)).Returns(false);
            A.CallTo(() => _isLastPage(_secondPage)).Returns(true);

            A.CallTo(() => _getNextPage("cursor_for_page_2")).Returns(_secondPage);
        }

        private readonly ITwitterResult<long> _firstPage;
        private readonly ITwitterResult<long> _secondPage;
        private readonly Func<string, Task<ITwitterResult<long>>> _getNextPage;
        private readonly Func<ITwitterResult<long>, string> _extractNextCursor;
        private readonly Func<ITwitterResult<long>, bool> _isLastPage;

        [Fact]
        public async Task MoveToNextPage_ReturnsPageFromLambda()
        {
            // arrange
            var iterator = new TwitterPageIterator<ITwitterResult<long>, string>(null, _getNextPage, _extractNextCursor, _isLastPage);

            // act
            var result = await iterator.MoveToNextPage();

            // assert
            Assert.Equal("cursor_for_page_2", result.NextCursor);
            Assert.Equal("cursor_for_page_2", iterator.NextCursor);

            Assert.False(result.IsLastPage);
            Assert.False(iterator.Completed);

            Assert.Same(result.Content, _firstPage);
        }

        [Fact]
        public async Task MoveToNextPage_WhenAlreadyCompleted_ThrowsException()
        {
            // arrange
            A.CallTo(() => _isLastPage(_firstPage)).Returns(true);

            var iterator = new TwitterPageIterator<ITwitterResult<long>, string>(null, _getNextPage, _extractNextCursor, _isLastPage);

            // 1st iteration
            await iterator.MoveToNextPage();

            // act - assert
            await Assert.ThrowsAsync<TwitterIteratorAlreadyCompletedException>(async () => await iterator.MoveToNextPage());
            Assert.Equal("cursor_for_page_2", iterator.NextCursor);
            Assert.True(iterator.Completed);
        }

        [Fact]
        public async Task MoveToNextPageFromPage2_ShouldReturnSecondPage()
        {
            // arrange
            var iterator = new TwitterPageIterator<ITwitterResult<long>, string>("cursor_for_page_2", _getNextPage, _extractNextCursor, _isLastPage);

            // act
            var result = await iterator.MoveToNextPage();

            // assert
            Assert.Equal("cursor_for_page_3", result.NextCursor);
            Assert.Equal("cursor_for_page_3", iterator.NextCursor);

            Assert.True(result.IsLastPage);
            Assert.True(iterator.Completed);

            Assert.Same(result.Content, _secondPage);
        }

        [Fact]
        public async Task MoveToNextPageTwice_ReturnsSecondPage()
        {
            // arrange
            var iterator = new TwitterPageIterator<ITwitterResult<long>, string>(null, _getNextPage, _extractNextCursor, _isLastPage);

            // 1st iteration
            await iterator.MoveToNextPage();

            // act
            var result = await iterator.MoveToNextPage();

            // assert
            Assert.Equal("cursor_for_page_3", result.NextCursor);
            Assert.Equal("cursor_for_page_3", iterator.NextCursor);

            Assert.True(result.IsLastPage);
            Assert.True(iterator.Completed);

            Assert.Same(result.Content, _secondPage);
        }
    }
}