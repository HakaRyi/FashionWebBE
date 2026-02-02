using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class GroupUser
{
    public int GroupId { get; set; }

    public int AccountId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Group Group { get; set; } = null!;
}
