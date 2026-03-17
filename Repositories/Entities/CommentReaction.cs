using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class CommentReaction
    {
        public int CommentReactionId { get; set; }

        public int CommentId { get; set; }

        public int AccountId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Comment Comment { get; set; } = null!;

        public Account Account { get; set; } = null!;
    }

}
