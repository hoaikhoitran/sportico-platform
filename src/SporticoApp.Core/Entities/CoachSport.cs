using System;
using System.Collections.Generic;

namespace SporticoApp.Core.Entities;

/// <summary>
/// Many-to-many: Coach dạy những môn nào
/// </summary>
public partial class CoachSport
{
    public Guid CoachId { get; set; }

    public int SportId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CoachProfile Coach { get; set; } = null!;

    public virtual Sport Sport { get; set; } = null!;
}
