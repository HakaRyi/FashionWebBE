using Microsoft.AspNetCore.Mvc;
using Repositories.Entities;
using Services.Implements.SocialImp;
using Services.Request.CommentReq;
using Services.Request.ReactionReq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialController : ControllerBase
    {
        private readonly ISocialService _socialService;
        public SocialController(ISocialService socialService)
        {
            _socialService = socialService;
        }
        // GET: api/<SocialController>
        [HttpGet("get/{postId}")]
        public async Task<IActionResult> Get([FromRoute] int postId)
        {
            var result = await _socialService.GetAllReactionByPostId(postId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "No reactions found for the specified post."
                });
            }
        }
        [HttpGet("getAllCommentByPostId/{postId}")]
        public async Task<IActionResult> GetAllComment([FromRoute] int postId)
        {
            var result = await _socialService.GetAllCommentByPostId(postId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "No comment found for the specified post."
                });
            }
        }
        [HttpGet("getReactionCountByPostId/{postId}")]
        public async Task<IActionResult> GetCount([FromRoute] int postId)
        {
            var result = await _socialService.GetReactionCountByPostId(postId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Can not count, idk why"
                });
            }
        }
        [HttpGet("getCommentCountByPostId/{postId}")]
        public async Task<IActionResult> GetCountComment([FromRoute] int postId)
        {
            var result = await _socialService.GetCommentCountByPostId(postId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Can not count, idk why"
                });
            }
        }
        // GET api/<SocialController>/5
        [HttpGet("{reactionId}")]
        public async Task<IActionResult> GetById(int reactionId)
        {
            var result = await _socialService.GetById(reactionId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "Reaction not found."
                });
            }
        }
        [HttpGet("GetByCommentId/{commentId}")]
        public async Task<IActionResult> GetByCommentId(int commentId)
        {
            var result = await _socialService.GetCommentById(commentId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "Reaction not found."
                });
            }
        }

        // POST api/<SocialController>
        [HttpPost("{postId}")]
        public async Task<IActionResult> Post([FromRoute] int postId)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _socialService.CreateReaction(int.Parse(userId),postId);
            if (result != 0)
            {
                return Ok(new
                {
                    message = "Reaction created successfully.",
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Failed to create reaction."
                });
            }
        }
        [HttpPost("createComment/{postId}")]
        public async Task<IActionResult> CreateComment([FromRoute] int postId, [FromBody]CommentRequest request)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _socialService.CreateComment(request,int.Parse(userId),postId);
            if (result != 0)
            {
                return Ok(new
                {
                    message = "Comment created successfully.",
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Failed to create comment."
                });
            }
        }
        [HttpPut("updateComment/{commentId}")]
        public async Task<IActionResult> PutComment([FromRoute] int commentId, [FromBody] CommentRequest request)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _socialService.UpdateComment(commentId, int.Parse(userId), request);
            if (result != 0)
            {
                return Ok(new
                {
                    message = "Comment updated successfully.",
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Failed to update comment."
                });
            }
        }

        // PUT api/<SocialController>/5
        [HttpPut("{postId}")]
        public async Task<IActionResult> Put( [FromRoute] int postId, [FromBody] UpdateReactionRequest request)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _socialService.UpdateReaction(int.Parse(userId), postId, request);
            if (result != 0)
            {
                return Ok(new
                {
                    message = "Reaction updated successfully.",
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Failed to updated reaction."
                });
            }
        }

        // DELETE api/<SocialController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _socialService.RemoveReaction(id);
            if (result)
            {
                return Ok(new
                {
                    message = "Reaction deleted successfully.",
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Failed to delete reaction."
                });
            }
        }
        [HttpDelete("deteleComment/{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _socialService.RemoveReaction(commentId);
            if (result)
            {
                return Ok(new
                {
                    message = "Comment deleted successfully.",
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    message = "Failed to delete comment."
                });
            }
        }
    }
}
