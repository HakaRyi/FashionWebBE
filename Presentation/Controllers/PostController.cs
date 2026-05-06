using Application.Interfaces;
using Application.Response.PostResp;
using Application.Services.PostImp;
using Application.Utils;
using Domain.Dto.Social.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/post")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IEventService _eventService;

        public PostController(IPostService postService, IEventService eventService)
        {
            _postService = postService;
            _eventService = eventService;
        }

        [HttpGet("/api/events/{eventId}/posts")]
        public async Task<IActionResult> GetPostsByEvent(int eventId)
        {
            var posts = await _postService.GetPostsByEventIdAsync(eventId);
            return Ok(posts);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
        {
            var userId = User.GetUserId();

            var createdPost = await _postService.CreatePostAsync(userId, request);

            return Created($"/api/post/{createdPost.PostId}", createdPost);
        }

        [HttpPut("{postId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(
            int postId,
            [FromBody] UpdatePostDto request)
        {
            var userId = User.GetUserId();

            await _postService.UpdatePostAsync(postId, userId, request);

            return NoContent();
        }

        [HttpDelete("{postId:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userId = User.GetUserId();

            await _postService.DeletePostAsync(postId, userId);

            return NoContent();
        }

        [HttpPatch("{postId:int}/hide")]
        [Authorize]
        public async Task<IActionResult> HidePost(int postId)
        {
            var userId = User.GetUserId();

            var response = await _postService.HidePostAsync(postId, userId);

            return Ok(response);
        }

        [HttpPatch("{postId:int}/unhide")]
        [Authorize]
        public async Task<IActionResult> UnhidePost(int postId)
        {
            var userId = User.GetUserId();

            var response = await _postService.UnhidePostAsync(postId, userId);

            return Ok(response);
        }

        [HttpGet("{postId:int}")]
        [Authorize]
        public async Task<IActionResult> GetPostDetail(int postId)
        {
            var userId = User.GetUserId();

            var post = await _postService.GetPostDetailAsync(postId, userId);

            if (post == null)
                return NotFound();

            return Ok(post);
        }

        [HttpGet("feed")]
        [Authorize]
        public async Task<IActionResult> GetFeed(
            [FromQuery] DateTime? cursor,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postService.GetFeedAsync(userId, cursor, pageSize);

            return Ok(posts);
        }

        [HttpGet("trending")]
        [Authorize]
        public async Task<IActionResult> GetTrending([FromQuery] int limit = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postService.GetTrendingPostsAsync(userId, limit);

            return Ok(posts);
        }

        [HttpPost("{postId:int}/share")]
        [Authorize]
        public async Task<IActionResult> SharePost(int postId)
        {
            var shareCount = await _postService.SharePostAsync(postId);

            return Ok(new
            {
                message = "Share recorded successfully.",
                postId,
                shareCount
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postService.GetMyPostsAsync(userId, page, pageSize);

            return Ok(posts);
        }

        [HttpGet("user/{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserPosts(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            int? viewerId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                viewerId = User.GetUserId();
            }

            var posts = await _postService.GetUserPostsAsync(userId, viewerId, page, pageSize);

            return Ok(posts);
        }

        [HttpPost("event-participation")]
        [Authorize]
        public async Task<IActionResult> JoinEventWithPost([FromForm] CreatePostDto request)
        {
            var accountId = User.GetUserId();
            try
            {
                var result = await _eventService.JoinEventByPostAsync(accountId, request);

                return Created($"/api/post/{result.PostId}", result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("/api/search/finding")]
        public async Task<ActionResult<GlobalSearchResultDto>> GlobalSearch([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new GlobalSearchResultDto());
            }

            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int parsedId))
            {
                userId = parsedId;
            }

            var result = await _postService.SearchEverythingAsync(q, userId);

            return Ok(result);
        }
    }
}