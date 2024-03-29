﻿using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public enum Grade
{
    A, B, C, D, F
}

public class Enrollment: EntityBase
{
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    [DisplayFormat(NullDisplayText = "No grade")]
    public Grade? Grade { get; set; }

    public virtual Course Course { get; set; }
    public virtual Student Student { get; set; }
}

