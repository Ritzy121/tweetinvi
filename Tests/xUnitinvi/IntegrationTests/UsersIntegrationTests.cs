﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Xunit;
using Xunit.Abstractions;

namespace xUnitinvi.IntegrationTests
{
    // VERY IMPORTANT NOTE !!!
    // THESE TESTS CANNOT BE RUN IN PARALLEL AS SOME OPERATIONS CAN AFFECT THE STATES IN TWITTER
    // RunIntegrationTests() run each of them one after another

    public class UserIntegrationTests
    {
        private readonly ITestOutputHelper _logger;
        private readonly ITwitterClient _client;
        private readonly ITwitterClient _privateUserClient;

        public UserIntegrationTests(ITestOutputHelper logger)
        {
            _logger = logger;

            _logger.WriteLine(DateTime.Now.ToLongTimeString());

            _client = new TwitterClient(IntegrationTestConfig.TweetinviTest.Credentials);
            _privateUserClient = new TwitterClient(IntegrationTestConfig.ProtectedUser.Credentials);

            TweetinviEvents.QueryBeforeExecute += (sender, args) => { _logger.WriteLine(args.Url); };
        }

        [Fact]
        public async Task RunAllUserTests()
        {
            if (!IntegrationTestConfig.ShouldRunIntegrationTests)
                return;

            _logger.WriteLine($"Starting {nameof(TestFollow)}");
            await TestFollow().ConfigureAwait(false);
            _logger.WriteLine($"{nameof(TestFollow)} succeeded");

            _logger.WriteLine($"Starting {nameof(TestRelationships)}");
            await TestRelationships().ConfigureAwait(false);
            _logger.WriteLine($"{nameof(TestRelationships)} succeeded");

            _logger.WriteLine($"Starting {nameof(TestWithPrivateUser)}");
            await TestWithPrivateUser().ConfigureAwait(false);
            _logger.WriteLine($"{nameof(TestWithPrivateUser)} succeeded");
        }

        [Fact]
        public async Task TestFollow()
        {
            if (!IntegrationTestConfig.ShouldRunIntegrationTests)
                return;

            // act
            var tweetinviTestAuthenticated = await _client.Account.GetAuthenticatedUser();
            var tweetinviTestUser = await _client.Users.GetUser("tweetinvitest");

            var followers = new List<IUser>();
            var followersIterator = tweetinviTestAuthenticated.GetFollowers();

            while (!followersIterator.Completed)
            {
                var pageFollowers = await followersIterator.MoveToNextPage();
                followers.AddRange(pageFollowers);
            }

            var userToFollow = await _client.Users.GetUser("tweetinviapi");

            var friendIdsIterator = _client.Users.GetFriendIds("tweetinvitest");
            var friendIds = await friendIdsIterator.MoveToNextPage();

            if (userToFollow.Id != null && friendIds.Contains(userToFollow.Id.Value))
            {
                await _client.Account.UnFollowUser(userToFollow);
            }

            await _client.Account.FollowUser(userToFollow);

            var friendsAfterAdd = await tweetinviTestAuthenticated.GetFriends().MoveToNextPage();

            await _client.Account.UnFollowUser(userToFollow);

            var friendsAfterRemove = await tweetinviTestAuthenticated.GetFriends().MoveToNextPage();

            // assert
            Assert.Equal(1693649419, tweetinviTestUser.Id);
            Assert.NotNull(tweetinviTestAuthenticated);

            Assert.Contains(friendsAfterAdd, friend => friend.Id == userToFollow.Id);
            Assert.DoesNotContain(friendsAfterRemove, friend => friend.Id == userToFollow.Id);
        }

        [Fact]
        public async Task TestRelationships()
        {
            if (!IntegrationTestConfig.ShouldRunIntegrationTests)
                return;

            // act
            var authenticatedUser = await _client.Account.GetAuthenticatedUser();

            var usernameToFollow = "tweetinviapi";
            var userToFollow = await _client.Users.GetUser(usernameToFollow);

            await _client.Account.FollowUser(userToFollow);

            var relationshipAfterAdd = await authenticatedUser.GetRelationshipWith(userToFollow);
            var relationshipStateAfterAdd = await _client.Account.GetRelationshipsWith(new IUserIdentifier[] {userToFollow});

            await _client.Account.UpdateRelationship(new UpdateRelationshipParameters(userToFollow)
            {
                EnableRetweets = false,
                EnableDeviceNotifications = true
            });

            var retweetMutedUsers = await _client.Account.GetUserIdsWhoseRetweetsAreMuted();
            var relationshipAfterUpdate = await _client.Users.GetRelationshipBetween(authenticatedUser, userToFollow);

            await _client.Account.UnFollowUser(userToFollow);

            var relationshipAfterRemove = await _client.Users.GetRelationshipBetween(authenticatedUser, userToFollow);
            var relationshipStateAfterRemove = await _client.Account.GetRelationshipsWith(new[] {usernameToFollow});

            // assert
            Assert.False(relationshipAfterAdd.NotificationsEnabled);
            Assert.True(relationshipAfterUpdate.NotificationsEnabled);

            Assert.True(relationshipAfterAdd.Following);
            Assert.False(relationshipAfterRemove.Following);

            Assert.True(relationshipStateAfterAdd[userToFollow].Following);
            Assert.False(relationshipStateAfterRemove[usernameToFollow].Following);

            Assert.Contains(retweetMutedUsers, x => x == userToFollow.Id);
        }

        [Fact]
        public async Task TestWithPrivateUser()
        {
            if (!IntegrationTestConfig.ShouldRunIntegrationTests) { return; }

            var publicUser = await _client.Account.GetAuthenticatedUser();
            var privateUser = await _privateUserClient.Account.GetAuthenticatedUser();

            // act
            await _client.Account.FollowUser(privateUser);

            var sentRequestsIterator = _client.Account.GetUsersYouRequestedToFollow();
            var sentRequestUsers = await sentRequestsIterator.MoveToNextPage();

            var receivedRequestsIterator = _privateUserClient.Account.GetUsersRequestingFriendship();
            var receivedRequestUsers = await receivedRequestsIterator.MoveToNextPage();

            // delete ongoing request
//            await publicUserClient.Account.UnFollowUser(privateUser);
//
//            var afterUnfollowSentRequestsIterator = publicUserClient.Account.GetUsersYouRequestedToFollow();
//            var afterUnfollowSentRequestUsers = await afterUnfollowSentRequestsIterator.MoveToNextPage();
//
//            var afterUnfollowReceivedRequestsIterator = privateUserClient.Account.GetUsersRequestingFriendship();
//            var afterUnfollowReceivedRequestUsers = await afterUnfollowReceivedRequestsIterator.MoveToNextPage();

            // assert
            Assert.Contains(sentRequestUsers, user => user.Id == privateUser.Id);
            Assert.Contains(receivedRequestUsers, user => user.Id == publicUser.Id);

//            Assert.DoesNotContain(afterUnfollowSentRequestUsers, user => user.Id == privateUser.Id);
//            Assert.DoesNotContain(afterUnfollowReceivedRequestUsers, user => user.Id == publicUser.Id);
        }
    }
}